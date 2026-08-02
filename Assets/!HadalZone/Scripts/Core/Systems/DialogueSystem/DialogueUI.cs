using DG.Tweening;
using RKS.HadalZone.Core.Managers;
using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using Zenject;

namespace RKS.HadalZone.Core.Dialogue
{
    public class DialogueUI : RKSBehaviour
    {
        [Header("Panels")]
        [SerializeField] private CanvasGroup dialogueGroup;
        [SerializeField] private GameObject choiceRoot;

        [Header("Speaker")]
        [SerializeField] private CanvasGroup speakerGroup;
        [SerializeField] private TMP_Text speakerLabel;

        [Header("Text")]
        [SerializeField] private TMP_Text content;

        [Header("Continue Indicator")]
        [SerializeField] private CanvasGroup continueIndicator;

        [Header("Choice")]
        [SerializeField] private RectTransform choicePrefab;
        [SerializeField] private HorizontalLayoutGroup choiceLayout;

        [Header("Animation")]
        [SerializeField] private float fadeTime = 0.2f;
        [SerializeField] private float slideDist = 50f;

        [Inject] private DialogueManager _mgr;

        private Coroutine _typing;
        private bool _isTyping;
        private bool _choiceClickedThisFrame;
        private Tween _continueBlink;

        private Vector2 _dialogueShown;
        private Vector2 _dialogueHidden;
        private Vector2 _speakerShown;
        private Vector2 _speakerHidden;

        protected override void OnReady()
        {
            if (_mgr == null)
            {
                Debug.LogError("[DialogueUI] DialogueManager не найден.");
                return;
            }

            _mgr.OnStarted += OnStarted;
            _mgr.OnEnded += OnEnded;
            _mgr.OnNodeChanged += OnNode;

            SetupPanel(dialogueGroup, out _dialogueShown, out _dialogueHidden);
            SetupPanel(speakerGroup, out _speakerShown, out _speakerHidden);

            if (content != null)
            {
                content.textWrappingMode = TextWrappingModes.Normal;
                content.text = "";
            }

            if (choiceRoot != null) choiceRoot.SetActive(false);

            SetHidden(continueIndicator);
        }

        private void SetupPanel(CanvasGroup group, out Vector2 shown, out Vector2 hidden)
        {
            shown = Vector2.zero;
            hidden = Vector2.zero;

            if (group == null) return;

            var rt = group.GetComponent<RectTransform>();
            shown = rt.anchoredPosition;
            hidden = shown + Vector2.down * slideDist;
            rt.anchoredPosition = hidden;
            group.alpha = 0f;
            group.gameObject.SetActive(false);
        }

        private void OnStarted(DialogueData data)
        {
            ShowPanel(dialogueGroup, _dialogueHidden, _dialogueShown);
        }

        private void OnEnded()
        {
            HidePanel(dialogueGroup, _dialogueHidden, () =>
            {
                dialogueGroup.gameObject.SetActive(false);
                ClearChoices();
            });

            HidePanel(speakerGroup, _speakerHidden, () =>
            {
                speakerGroup.gameObject.SetActive(false);
                if (speakerLabel != null) speakerLabel.text = "";
            });

            SetHidden(continueIndicator);
            KillBlink();
        }

        private void OnNode(Node node)
        {
            if (_typing != null) StopCoroutine(_typing);
            KillBlink();

            ShowSpeaker(node);

            var text = Localization != null && LocalizationManager.isReady
                ? Localization.GetLocalizedValue(node.textKey)
                : node.textKey;

            _choiceClickedThisFrame = false;
            SetHidden(continueIndicator);

            content.text = text;
            content.maxVisibleCharacters = 0;
            content.pageToDisplay = 1;
            content.firstVisibleCharacter = 0;

            int cps = ResolveCps(node);
            _typing = StartCoroutine(TypeText(text, cps));
            BuildChoices(node);

            if (!string.IsNullOrEmpty(node.voiceLine) && Audio != null)
                Audio.PlayOneShot(node.voiceLine);
        }

        private static int ResolveCps(Node node)
        {
            if (node.actor != null && node.actor.defaultCharsPerSecond >= 0)
                return node.actor.defaultCharsPerSecond;
            return node.charsPerSecond;
        }

        private void ShowSpeaker(Node node)
        {
            if (speakerGroup == null) return;

            bool hasActor = node.actor != null;

            if (hasActor)
            {
                var name = Localization != null && LocalizationManager.isReady
                    ? Localization.GetLocalizedValue(node.actor.displayNameKey)
                    : node.actor.displayNameKey;

                speakerGroup.gameObject.SetActive(true);
                speakerGroup.DOKill();
                var rt = speakerGroup.GetComponent<RectTransform>();
                rt.anchoredPosition = _speakerHidden;
                speakerGroup.alpha = 0f;
                speakerGroup.DOFade(1f, fadeTime);
                rt.DOAnchorPos(_speakerShown, fadeTime).SetEase(Ease.OutBack);

                if (speakerLabel != null)
                    speakerLabel.text = name;
            }
            else
            {
                HidePanel(speakerGroup, _speakerHidden, () =>
                {
                    speakerGroup.gameObject.SetActive(false);
                    if (speakerLabel != null) speakerLabel.text = "";
                });
            }
        }

        private IEnumerator TypeText(string text, int cps)
        {
            _isTyping = true;
            int total = text.Length;

            if (cps <= 0 || total == 0)
            {
                content.maxVisibleCharacters = total;
                _isTyping = false;
                OnTypingComplete();
                yield break;
            }

            float interval = 1f / cps;

            // При 1 символ/сек пауза между символами ≈ 1000 мс.
            // Если total мал, текст не должен появляться мгновенно.
            int visible = 0;
            float timer = 0f;

            while (visible < total)
            {
                timer += Time.deltaTime;

                while (timer >= interval && visible < total)
                {
                    visible++;
                    content.maxVisibleCharacters = visible;
                    timer -= interval;
                }

                yield return null;
            }

            _isTyping = false;
            OnTypingComplete();
        }

        private void CompleteTyping()
        {
            if (_typing != null) StopCoroutine(_typing);

            content.maxVisibleCharacters = int.MaxValue;
            _isTyping = false;
            OnTypingComplete();
        }

        private void OnTypingComplete()
        {
            if (_mgr == null || !_mgr.IsActive) return;

            var node = _mgr.GetCurrentNode();
            if (node == null) return;

            _mgr.NotifyDisplayComplete();

            bool hasChoices = node.choices != null && node.choices.Length > 0;
            if (!hasChoices)
                TryShowContinue();
        }

        private void TryShowContinue()
        {
            if (continueIndicator == null) return;

            continueIndicator.gameObject.SetActive(true);
            continueIndicator.alpha = 1f;

            _continueBlink = continueIndicator.DOFade(0.2f, 0.6f)
                .SetLoops(-1, LoopType.Yoyo)
                .SetEase(Ease.InOutSine);
        }

        private void BuildChoices(Node node)
        {
            ClearChoices();

            if (node.choices == null || node.choices.Length == 0)
            {
                if (choiceRoot != null) choiceRoot.SetActive(false);
                return;
            }

            if (choiceRoot != null) choiceRoot.SetActive(true);

            var fitter = choiceRoot.GetComponent<ContentSizeFitter>();
            if (fitter == null)
                fitter = choiceRoot.AddComponent<ContentSizeFitter>();
            fitter.horizontalFit = ContentSizeFitter.FitMode.PreferredSize;
            fitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

            if (choiceLayout != null)
                choiceLayout.childForceExpandWidth = false;

            for (int i = 0; i < node.choices.Length; i++)
            {
                var btn = Instantiate(choicePrefab, choiceLayout.transform);
                var btnText = btn.GetComponentInChildren<TMP_Text>();

                var label = Localization != null && LocalizationManager.isReady
                    ? Localization.GetLocalizedValue(node.choices[i].textKey)
                    : node.choices[i].textKey;
                btnText.text = label;

                btnText.ForceMeshUpdate();
                float w = btnText.preferredWidth + 30f;

                var le = btn.GetComponent<LayoutElement>();
                if (le == null) le = btn.gameObject.AddComponent<LayoutElement>();
                le.preferredWidth = w;

                int idx = i;
                btn.GetComponent<UnityEngine.UI.Button>().onClick.AddListener(() =>
                {
                    _choiceClickedThisFrame = true;
                    _mgr.SelectChoice(idx);
                });

                btn.localScale = Vector3.zero;
                btn.DOScale(1f, 0.2f).SetDelay(i * 0.08f).SetEase(Ease.OutBack);
            }
        }

        private void ClearChoices()
        {
            if (choiceLayout == null) return;
            for (int i = choiceLayout.transform.childCount - 1; i >= 0; i--)
                Destroy(choiceLayout.transform.GetChild(i).gameObject);
        }

        protected override void Update()
        {
            if (_mgr == null || !_mgr.IsActive) return;

            if (_choiceClickedThisFrame)
            {
                _choiceClickedThisFrame = false;
                return;
            }

            if (_isTyping)
            {
                if (Input.GetButtonDown("Submit") || Input.GetMouseButtonDown(0))
                    CompleteTyping();
            }
            else
            {
                bool advance = Input.GetButtonDown("Submit");
                if (!advance && Input.GetMouseButtonDown(0) && !IsPointerOverUI())
                    advance = true;

                if (advance)
                    _mgr.NextNode();
            }
        }

        private static bool IsPointerOverUI()
        {
            return EventSystem.current != null && EventSystem.current.IsPointerOverGameObject();
        }

        private void ShowPanel(CanvasGroup group, Vector2 from, Vector2 to)
        {
            if (group == null) return;
            group.gameObject.SetActive(true);
            group.DOKill();
            var rt = group.GetComponent<RectTransform>();
            rt.anchoredPosition = from;
            group.DOFade(1f, fadeTime);
            rt.DOAnchorPos(to, fadeTime).SetEase(Ease.OutBack);
        }

        private void HidePanel(CanvasGroup group, Vector2 to, TweenCallback onComplete)
        {
            if (group == null) return;
            group.DOKill();
            var rt = group.GetComponent<RectTransform>();
            group.DOFade(0f, fadeTime);
            rt.DOAnchorPos(to, fadeTime)
                .SetEase(Ease.InBack)
                .OnComplete(onComplete);
        }

        private static void SetHidden(CanvasGroup g)
        {
            if (g == null) return;
            g.DOKill();
            g.alpha = 0f;
            g.gameObject.SetActive(false);
        }

        private void KillBlink()
        {
            if (_continueBlink != null)
            {
                _continueBlink.Kill();
                _continueBlink = null;
            }
        }

        protected override void OnDisposed()
        {
            if (_mgr != null)
            {
                _mgr.OnStarted -= OnStarted;
                _mgr.OnEnded -= OnEnded;
                _mgr.OnNodeChanged -= OnNode;
            }
        }
    }
}
