using DG.Tweening;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace RKS.HadalZone.Player
{
    public class InteractionPromptUI : MonoBehaviour
    {
        [Header("UI")]
        [SerializeField] private RectTransform panel;
        [SerializeField] private TMP_Text label;
        [SerializeField] private string format = " {0}";

        [Header("Animation")]
        [SerializeField] private float duration = 0.4f;
        [SerializeField] private float offset = 100f;

        [Header("Detection")]
        [SerializeField] private InteractionManager interaction;

        private Vector2 _shown;
        private Vector2 _hidden;

        private void Awake()
        {
            if (interaction == null)
                interaction = FindObjectOfType<InteractionManager>();

            if (panel != null)
            {
                _shown = panel.anchoredPosition;
                _hidden = _shown + Vector2.down * offset;
                panel.anchoredPosition = _hidden;
                panel.gameObject.SetActive(false);
            }

            if (interaction != null)
            {
                interaction.onInteractableFound.AddListener(OnFound);
                interaction.onInteractableLost.AddListener(OnLost);
            }
        }

        private void OnFound()
        {
            if (panel == null || interaction?.Current == null) return;

            panel.gameObject.SetActive(true);
            label.text = string.Format(format, interaction.Current.Prompt);

            panel.DOKill();
            panel.anchoredPosition = _hidden;
            panel.DOAnchorPos(_shown, duration).SetEase(Ease.OutBack);
        }

        private void OnLost()
        {
            if (panel == null) return;

            panel.DOKill();
            panel.DOAnchorPos(_hidden, duration * 0.7f)
                .SetEase(Ease.InBack)
                .OnComplete(() => panel.gameObject.SetActive(false));
        }

        private void OnDestroy()
        {
            if (interaction != null)
            {
                interaction.onInteractableFound.RemoveListener(OnFound);
                interaction.onInteractableLost.RemoveListener(OnLost);
            }
        }
    }
}
