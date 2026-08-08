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
        [SerializeField] private CanvasGroup dialogueGroup;
        [SerializeField] private CanvasGroup speakerGroup;
        [SerializeField] private TMP_Text speakerLabel;
        [SerializeField] private TMP_Text content;
        [SerializeField] private TMP_Text continueIndicator;
        [SerializeField] private RectTransform choicePrefab;
        [SerializeField] private GameObject choiceRoot;
        [HideInInspector] private HorizontalLayoutGroup choiceLayout;

        [Header("Animation")]
        [SerializeField] private float fadeTime = 0.2f;
        [SerializeField] private float slideDist = 50f;
        [SerializeField, Min(0f)] private float choiceStagger = 0.05f;

        [Header("Input")]
        [SerializeField, Min(0f)] private float startInputLock = 0.12f;
        [SerializeField, Min(0f)] private float advanceInputLock = 0.12f;
        [SerializeField, Min(0f)] private float nodeChangeInputLock = 0.04f;

        [Inject] private DialogueManager _mgr;

        private Coroutine _typing;
        private bool _isTyping;
        private bool _choicesVisible;
        private bool _choiceClickedThisFrame;

        private Node _currentNode;
        private string _lastSpeakerKey;
        private int _totalChars;

        private float _nextAllowedInputTime;
        private bool _waitForSubmitRelease;

        private Vector2 _dialogueShown;
        private Vector2 _dialogueHidden;
        private Vector2 _speakerShown;
        private Vector2 _speakerHidden;

        private float AnimTime => fadeTime <= 0f ? 0.01f : fadeTime;

        protected override void OnReady()
        {
            if (_mgr == null)
            {
                Debug.LogError("[DialogueUI] DialogueManager not found.");
                return;
            }

            _mgr.OnStarted += OnStarted;
            _mgr.OnEnded += OnEnded;
            _mgr.OnNodeChanged += OnNode;

            SetupPanel(dialogueGroup, out _dialogueShown, out _dialogueHidden);
            SetupPanel(speakerGroup, out _speakerShown, out _speakerHidden);

            if (choiceRoot != null)
            {
                choiceLayout = choiceRoot.GetComponent<HorizontalLayoutGroup>();

                if (choiceLayout == null)
                {
                    var anyLayout = choiceRoot.GetComponent<LayoutGroup>();
                    if (anyLayout == null)
                    {
                        choiceLayout = choiceRoot.AddComponent<HorizontalLayoutGroup>();
                        choiceLayout.childAlignment = TextAnchor.MiddleCenter;
                        choiceLayout.spacing = 12f;
                    }
                }

                if (choiceLayout != null)
                {
                    choiceLayout.childForceExpandWidth = false;
                    choiceLayout.childForceExpandHeight = false;
                }

                var fitter = choiceRoot.GetComponent<ContentSizeFitter>();
                if (fitter == null)
                    fitter = choiceRoot.AddComponent<ContentSizeFitter>();

                fitter.horizontalFit = ContentSizeFitter.FitMode.PreferredSize;
                fitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

                choiceRoot.SetActive(false);
            }

            if (content != null)
            {
                content.textWrappingMode = TextWrappingModes.Normal;
                content.text = "";
                content.maxVisibleCharacters = 0;
            }

            HideContinue();
        }

        private void SetupPanel(CanvasGroup group, out Vector2 shown, out Vector2 hidden)
        {
            shown = Vector2.zero;
            hidden = Vector2.zero;

            if (group == null) return;

            var rt = group.GetComponent<RectTransform>();
            if (rt == null) return;

            shown = rt.anchoredPosition;
            hidden = shown + Vector2.down * slideDist;

            rt.anchoredPosition = hidden;
            rt.localScale = Vector3.one;
            group.alpha = 0f;
            group.gameObject.SetActive(false);
        }

        private void OnStarted(DialogueData data)
        {
            _currentNode = null;
            _choicesVisible = false;
            _lastSpeakerKey = null;

            LockInput(Mathf.Max(startInputLock, AnimTime * 0.75f));
            _waitForSubmitRelease = true;

            ShowPanel(dialogueGroup, _dialogueHidden, _dialogueShown);
        }

        private void OnEnded()
        {
            StopTyping();

            _currentNode = null;
            _choicesVisible = false;
            _lastSpeakerKey = null;

            LockInput(Mathf.Max(advanceInputLock, AnimTime * 0.75f));
            _waitForSubmitRelease = true;

            HidePanel(dialogueGroup, _dialogueHidden, ClearChoices);
            HidePanel(speakerGroup, _speakerHidden, () =>
            {
                if (speakerLabel != null) speakerLabel.text = "";
            });

            HideContinue();
        }

        private void OnNode(Node node)
        {
            if (node == null) return;

            StopTyping();
            HideContinue();
            ClearChoices();

            _currentNode = node;
            _choicesVisible = false;
            LockInput(nodeChangeInputLock);

            ShowSpeaker(node);

            string text = Localization != null && LocalizationManager.isReady
                ? Localization.GetLocalizedValue(node.textKey)
                : node.textKey;

            if (string.IsNullOrEmpty(text))
                text = string.Empty;

            if (content != null)
            {
                content.text = text;
                content.maxVisibleCharacters = int.MaxValue;
                content.pageToDisplay = 1;
                content.firstVisibleCharacter = 0;
                content.ForceMeshUpdate(true, true);

                _totalChars = content.textInfo != null ? content.textInfo.characterCount : text.Length;
                content.maxVisibleCharacters = 0;

                int cps = ResolveCps(node);
                _typing = StartCoroutine(TypeText(cps));
            }
            else
            {
                _totalChars = 0;
                OnTypingComplete();
            }

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

            if (node.actor == null)
            {
                _lastSpeakerKey = null;

                if (speakerGroup.gameObject.activeSelf)
                {
                    HidePanel(speakerGroup, _speakerHidden, () =>
                    {
                        if (speakerLabel != null) speakerLabel.text = "";
                    });
                }

                return;
            }

            string key = string.IsNullOrEmpty(node.actor.displayNameKey)
                ? string.Empty
                : node.actor.displayNameKey;

            string speakerName = Localization != null && LocalizationManager.isReady
                ? Localization.GetLocalizedValue(key)
                : key;

            if (speakerLabel != null)
                speakerLabel.text = speakerName;

            if (speakerGroup.gameObject.activeSelf && _lastSpeakerKey == key && speakerGroup.alpha > 0.9f)
                return;

            _lastSpeakerKey = key;
            ShowPanel(speakerGroup, _speakerHidden, _speakerShown);
        }

        private IEnumerator TypeText(int cps)
        {
            _isTyping = true;

            if (content == null)
            {
                _isTyping = false;
                OnTypingComplete();
                yield break;
            }

            int total = _totalChars;

            if (cps <= 0 || total <= 0)
            {
                content.maxVisibleCharacters = total;
                _isTyping = false;
                OnTypingComplete();
                yield break;
            }

            float interval = 1f / Mathf.Max(1, cps);
            int visible = 0;
            float timer = 0f;

            while (visible < total)
            {
                timer += Time.unscaledDeltaTime;

                while (timer >= interval && visible < total)
                {
                    visible++;
                    timer -= interval;
                }

                content.maxVisibleCharacters = visible;
                yield return null;
            }

            content.maxVisibleCharacters = total;
            _isTyping = false;
            OnTypingComplete();
        }

        private void StopTyping()
        {
            if (_typing != null)
            {
                StopCoroutine(_typing);
                _typing = null;
            }

            _isTyping = false;
        }

        private void CompleteTyping()
        {
            if (_typing == null && !_isTyping) return;

            StopTyping();

            if (content != null)
                content.maxVisibleCharacters = _totalChars;

            OnTypingComplete();
        }

        private void OnTypingComplete()
        {
            if (_mgr == null || !_mgr.IsActive) return;

            var node = _mgr.GetCurrentNode();
            if (node == null) return;

            _currentNode = node;
            _mgr.NotifyDisplayComplete();

            if (HasChoices(node))
                BuildChoices(node);
            else
                TryShowContinue();
        }

        private void TryShowContinue()
        {
            if (continueIndicator == null) return;

            DOTween.Kill(continueIndicator);

            continueIndicator.gameObject.SetActive(true);
            continueIndicator.alpha = 0f;

            var rt = continueIndicator.rectTransform;

            rt.localScale = Vector3.one * 0.8f;
            rt.localRotation = Quaternion.identity;

            Vector2 startPos = rt.anchoredPosition;

            // Тряска на всё время, пока continue видим
            var shake = rt.DOShakeAnchorPos(0.35f, 3f, 12, 90f, false, false)
                .SetTarget(continueIndicator)
                .SetUpdate(true)
                .SetLoops(-1, LoopType.Restart);

            shake.OnKill(() =>
            {
                if (rt != null)
                    rt.anchoredPosition = startPos;
            });

            var seq = DOTween.Sequence()
                .SetTarget(continueIndicator)
                .SetUpdate(true);

            // Появление
            seq.Join(continueIndicator.DOFade(1f, 0.14f).SetEase(Ease.OutSine));

            // Лёгкое увеличение
            seq.Join(rt.DOScale(0.92f, 0.18f).SetEase(Ease.OutCubic));

            // Вращение на 360 градусов
            seq.Join(
                rt.DOLocalRotate(Vector3.forward * 360f, 0.45f, RotateMode.FastBeyond360)
                  .SetEase(Ease.OutCubic)
            );

            // Мягкая фиксация размера
            seq.Append(rt.DOScale(0.88f, 0.16f).SetEase(Ease.InOutSine));

            seq.OnComplete(() =>
            {
                rt.localRotation = Quaternion.identity;

                // Текущая пульсация
                rt.DOScale(0.93f, 0.55f)
                    .SetTarget(continueIndicator)
                    .SetUpdate(true)
                    .SetLoops(-1, LoopType.Yoyo)
                    .SetEase(Ease.InOutSine);

                continueIndicator.DOFade(0.55f, 0.55f)
                    .SetTarget(continueIndicator)
                    .SetUpdate(true)
                    .SetLoops(-1, LoopType.Yoyo)
                    .SetEase(Ease.InOutSine);
            });

            seq.OnKill(() =>
            {
                if (rt != null)
                    rt.anchoredPosition = startPos;
            });
        }

        private void HideContinue()
        {
            if (continueIndicator == null) return;

            DOTween.Kill(continueIndicator);
            continueIndicator.alpha = 0f;
            continueIndicator.gameObject.SetActive(false);
        }

        private void BuildChoices(Node node)
        {
            ClearChoices();

            if (node.choices == null || node.choices.Length == 0 || choiceRoot == null || choicePrefab == null)
            {
                if (choiceRoot != null)
                    choiceRoot.SetActive(false);
                return;
            }

            _choicesVisible = true;
            choiceRoot.SetActive(true);

            Transform parent = choiceLayout != null ? choiceLayout.transform : choiceRoot.transform;

            for (int i = 0; i < node.choices.Length; i++)
            {
                var btn = Instantiate(choicePrefab, parent);

                var button = btn.GetComponent<Button>();
                if (button == null)
                    button = btn.gameObject.AddComponent<Button>();

                var label = btn.GetComponentInChildren<TMP_Text>(true);
                var choice = node.choices[i];

                string choiceText = Localization != null && LocalizationManager.isReady
                    ? Localization.GetLocalizedValue(choice.textKey)
                    : choice.textKey;

                if (string.IsNullOrEmpty(choiceText))
                    choiceText = string.Empty;

                if (label != null)
                {
                    label.text = choiceText;
                    label.ForceMeshUpdate();

                    var le = btn.GetComponent<LayoutElement>();
                    if (le == null)
                        le = btn.gameObject.AddComponent<LayoutElement>();

                    le.preferredWidth = label.preferredWidth + 30f;
                }

                var cg = btn.GetComponent<CanvasGroup>();
                if (cg == null)
                    cg = btn.gameObject.AddComponent<CanvasGroup>();

                cg.alpha = 0f;
                cg.blocksRaycasts = false;
                button.interactable = false;

                // Более спокойный старт
                btn.localScale = Vector3.one * 0.94f;

                int idx = i;
                Button capturedButton = button;

                capturedButton.onClick.AddListener(() =>
                {
                    capturedButton.interactable = false;
                    _choiceClickedThisFrame = true;
                    LockInput(advanceInputLock);
                    _waitForSubmitRelease = true;

                    if (_mgr != null)
                        _mgr.SelectChoice(idx);
                });

                float delay = i * Mathf.Max(0.02f, choiceStagger);

                float fadeDur = Mathf.Max(0.06f, AnimTime * 0.4f);
                float attackDur = Mathf.Max(0.12f, AnimTime * 0.6f);
                float settleDur = Mathf.Max(0.1f, AnimTime * 0.5f);

                var seq = DOTween.Sequence()
                    .SetTarget(btn.gameObject)
                    .SetUpdate(true);

                seq.Insert(delay, cg.DOFade(1f, fadeDur).SetEase(Ease.OutSine));

                // Лёгкий, аккуратный pop
                seq.Insert(delay, btn.DOScale(1.025f, attackDur).SetEase(Ease.OutCubic));

                // Плавный возврат
                seq.Insert(delay + attackDur, btn.DOScale(1f, settleDur).SetEase(Ease.InOutSine));

                seq.OnComplete(() =>
                {
                    cg.blocksRaycasts = true;
                    capturedButton.interactable = true;
                });
            }

            var rootRt = choiceRoot.GetComponent<RectTransform>();
            if (rootRt != null)
                LayoutRebuilder.ForceRebuildLayoutImmediate(rootRt);
        }

        private void ClearChoices()
        {
            _choicesVisible = false;

            if (choiceRoot == null) return;

            for (int i = choiceRoot.transform.childCount - 1; i >= 0; i--)
            {
                var child = choiceRoot.transform.GetChild(i);

                DOTween.Kill(child.gameObject);
                DOTween.Kill(child);

                var cg = child.GetComponent<CanvasGroup>();
                if (cg != null)
                    DOTween.Kill(cg);

                Destroy(child.gameObject);
            }

            choiceRoot.SetActive(false);
        }

        protected override void Update()
        {
            if (_mgr == null || !_mgr.IsActive) return;

            if (_choiceClickedThisFrame)
            {
                _choiceClickedThisFrame = false;
                return;
            }

            if (!CanReceiveInput()) return;

            if (_isTyping)
            {
                if (Input.GetButtonDown("Submit") || Input.GetMouseButtonDown(0))
                {
                    LockInput(0.02f);
                    CompleteTyping();
                }

                return;
            }

            if (_choicesVisible || HasChoices(_currentNode))
                return;

            bool advance = Input.GetButtonDown("Submit");
            if (!advance && Input.GetMouseButtonDown(0) && !IsPointerOverUI())
                advance = true;

            if (advance)
            {
                LockInput(advanceInputLock);
                _waitForSubmitRelease = true;
                _mgr.NextNode();
            }
        }

        private bool CanReceiveInput()
        {
            if (Time.unscaledTime < _nextAllowedInputTime)
                return false;

            if (_waitForSubmitRelease)
            {
                if (!Input.GetButton("Submit"))
                    _waitForSubmitRelease = false;
                else
                    return false;
            }

            return true;
        }

        private void LockInput(float duration)
        {
            if (duration <= 0f) return;
            _nextAllowedInputTime = Mathf.Max(_nextAllowedInputTime, Time.unscaledTime + duration);
        }

        private static bool HasChoices(Node node)
        {
            return node != null && node.choices != null && node.choices.Length > 0;
        }

        private static bool IsPointerOverUI()
        {
            return EventSystem.current != null && EventSystem.current.IsPointerOverGameObject();
        }

        private void ShowPanel(CanvasGroup group, Vector2 from, Vector2 to)
        {
            if (group == null) return;

            var rt = group.GetComponent<RectTransform>();
            if (rt == null) return;

            DOTween.Kill(group);

            group.gameObject.SetActive(true);
            group.blocksRaycasts = false;
            group.alpha = 0f;

            rt.anchoredPosition = from;
            rt.localScale = Vector3.one * 0.96f;

            Vector2 delta = to - from;
            Vector2 dir = delta.sqrMagnitude > 0.001f ? delta.normalized : Vector2.up;

            // Небольшой overshoot, но уже очень мягкий
            Vector2 overshootPosition = to + dir * 5f;

            float fadeDur = Mathf.Max(0.07f, AnimTime * 0.5f);
            float attackDur = Mathf.Max(0.14f, AnimTime * 0.85f);
            float settleDur = Mathf.Max(0.12f, AnimTime * 0.6f);

            float scaleAttackDur = Mathf.Max(0.1f, AnimTime * 0.55f);
            float scaleSettleDur = Mathf.Max(0.1f, AnimTime * 0.55f);

            var seq = DOTween.Sequence()
                .SetTarget(group)
                .SetUpdate(true);

            seq.Join(group.DOFade(1f, fadeDur).SetEase(Ease.OutSine));

            seq.Join(rt.DOAnchorPos(overshootPosition, attackDur).SetEase(Ease.OutCubic));

            seq.Join(rt.DOScale(1.02f, scaleAttackDur).SetEase(Ease.OutSine));

            seq.Append(rt.DOAnchorPos(to, settleDur).SetEase(Ease.InOutSine));
            seq.Join(rt.DOScale(1f, scaleSettleDur).SetEase(Ease.InOutSine));

            seq.OnComplete(() => group.blocksRaycasts = true);
        }

        private void HidePanel(CanvasGroup group, Vector2 to, TweenCallback onComplete = null)
        {
            if (group == null)
            {
                onComplete?.Invoke();
                return;
            }

            DOTween.Kill(group);

            if (!group.gameObject.activeInHierarchy && group.alpha <= 0.01f)
            {
                onComplete?.Invoke();
                return;
            }

            var rt = group.GetComponent<RectTransform>();

            group.blocksRaycasts = false;

            float fadeDur = Mathf.Max(0.06f, AnimTime * 0.45f);
            float moveDur = Mathf.Max(0.12f, AnimTime * 0.75f);

            var seq = DOTween.Sequence()
                .SetTarget(group)
                .SetUpdate(true);

            seq.Join(group.DOFade(0f, fadeDur).SetEase(Ease.InOutSine));

            if (rt != null)
            {
                seq.Join(rt.DOAnchorPos(to, moveDur).SetEase(Ease.InOutCubic));
                seq.Join(rt.DOScale(0.97f, moveDur).SetEase(Ease.InOutCubic));
            }

            seq.OnComplete(() =>
            {
                group.gameObject.SetActive(false);
                onComplete?.Invoke();
            });
        }

        protected override void OnDisposed()
        {
            if (_mgr != null)
            {
                _mgr.OnStarted -= OnStarted;
                _mgr.OnEnded -= OnEnded;
                _mgr.OnNodeChanged -= OnNode;
            }

            StopTyping();
            HideContinue();
            ClearChoices();

            if (dialogueGroup != null) DOTween.Kill(dialogueGroup);
            if (speakerGroup != null) DOTween.Kill(speakerGroup);
        }
    }
}