using DG.Tweening;
using EasyPeasyFirstPersonController;
using RKS.HadalZone.Core;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using Zenject;

namespace RKS.HadalZone.Player
{
    public class InteractionPromptUI : RKSBehaviour
    {
        [Header("UI")]
        [SerializeField] private RectTransform panel;
        [SerializeField] private TMP_Text label;
        [SerializeField] private string format = " {0}";

        [Header("Animation")]
        [SerializeField] private float duration = 0.4f;
        [SerializeField] private float offset = 100f;

        [Inject] private PlayerController _playerController;
        private InteractionManager _interaction;
        private CanvasGroup _panelGroup;

        private Vector2 _shown;
        private Vector2 _hidden;

        protected override void OnInjected()
        {
            if (_playerController != null)
                _interaction = _playerController.GetComponent<InteractionManager>();
        }

        protected override void OnReady()
        {
            EnsurePanelGroup();

            if (panel != null)
            {
                _shown = panel.anchoredPosition;
                _hidden = _shown + Vector2.down * offset;

                panel.anchoredPosition = _hidden;
                panel.localScale = Vector3.one;

                if (_panelGroup != null)
                    _panelGroup.alpha = 0f;

                panel.gameObject.SetActive(false);
            }

            if (_interaction != null)
            {
                _interaction.onInteractableFound.AddListener(OnFound);
                _interaction.onInteractableLost.AddListener(OnLost);
            }
        }

        private void EnsurePanelGroup()
        {
            if (panel == null) return;

            if (_panelGroup == null)
            {
                _panelGroup = panel.GetComponent<CanvasGroup>();

                if (_panelGroup == null)
                    _panelGroup = panel.gameObject.AddComponent<CanvasGroup>();
            }

            _panelGroup.blocksRaycasts = false;
            _panelGroup.interactable = false;
        }

        private void OnFound()
        {
            if (panel == null || _interaction == null || _interaction.Current == null) return;

            EnsurePanelGroup();

            if (label != null)
            {
                string prompt = _interaction.Current.Prompt ?? string.Empty;
                label.text = string.IsNullOrEmpty(format)
                    ? prompt
                    : string.Format(format, prompt);
            }

            KillAnimations();

            panel.gameObject.SetActive(true);
            panel.anchoredPosition = _hidden;
            panel.localScale = Vector3.one * 0.9f;

            if (_panelGroup != null)
                _panelGroup.alpha = 0f;

            float d = Mathf.Max(0.05f, duration);

            float overshootDistance = Mathf.Clamp(Mathf.Abs(offset) * 0.1f, 6f, 18f);
            Vector2 overshootPosition = _shown + Vector2.up * overshootDistance;

            float attack = Mathf.Max(0.06f, d * 0.65f);
            float settle = Mathf.Max(0.05f, d * 0.45f);

            var seq = DOTween.Sequence()
                .SetTarget(panel)
                .SetUpdate(true);

            if (_panelGroup != null)
            {
                seq.Join(_panelGroup.DOFade(1f, attack * 0.8f).SetEase(Ease.OutCubic));
            }

            // Сначала чуть проскакивает финальную позицию
            seq.Join(panel.DOAnchorPos(overshootPosition, attack).SetEase(Ease.OutExpo));
            seq.Join(panel.DOScale(1.03f, attack).SetEase(Ease.OutCubic));

            // Затем мягко возвращается
            seq.Append(panel.DOAnchorPos(_shown, settle).SetEase(Ease.OutCubic));
            seq.Join(panel.DOScale(1f, settle).SetEase(Ease.InOutSine));
        }

        private void OnLost()
        {
            if (panel == null) return;

            if (!panel.gameObject.activeSelf)
                return;

            EnsurePanelGroup();
            KillAnimations();

            float d = Mathf.Max(0.04f, duration * 0.65f);

            var seq = DOTween.Sequence()
                .SetTarget(panel)
                .SetUpdate(true);

            if (_panelGroup != null)
            {
                seq.Join(_panelGroup.DOFade(0f, d * 0.9f).SetEase(Ease.InOutSine));
            }

            seq.Join(panel.DOAnchorPos(_hidden, d).SetEase(Ease.InOutCubic));
            seq.Join(panel.DOScale(0.92f, d).SetEase(Ease.InOutCubic));

            seq.OnComplete(() =>
            {
                if (panel != null)
                    panel.gameObject.SetActive(false);
            });
        }

        private void KillAnimations()
        {
            if (panel != null)
                DOTween.Kill(panel);

            if (_panelGroup != null)
                DOTween.Kill(_panelGroup);
        }

        protected override void OnDestroy()
        {
            if (_interaction != null)
            {
                _interaction.onInteractableFound.RemoveListener(OnFound);
                _interaction.onInteractableLost.RemoveListener(OnLost);
            }

            KillAnimations();
        }
    }
}