using DG.Tweening;
using TMPro;
using RKS.HadalZone.Core;
using UnityEngine;

namespace RKS.HadalZone.UI
{
    [RequireComponent(typeof(TextMeshProUGUI))]
    public class NarrativeText : RKSBehaviour
    {
        private TextMeshProUGUI text;

        public float Alpha => text.alpha;
        public float CharacterSpacing => text.characterSpacing;

        protected override void OnReady()
        {
            text = GetComponent<TextMeshProUGUI>();
        }

        public void Show()
        {
            text.alpha = 1f;
        }

        public void Hide()
        {
            text.alpha = 0f;
        }

        public Tween FadeIn(float duration)
        {
            return Fade(1f, duration);
        }

        public Tween FadeOut(float duration)
        {
            return Fade(0f, duration);
        }

        public Tween Fade(float targetAlpha, float duration)
        {
            return text.DOFade(targetAlpha, duration);
        }

        public void SetSpacing(float value)
        {
            text.characterSpacing = value;
        }

        public Tween AnimateSpacing(float target, float duration)
        {
            return DOTween.To(
                () => text.characterSpacing,
                x => text.characterSpacing = x,
                target,
                duration);
        }

        public Tween ResetSpacing(float duration)
        {
            return AnimateSpacing(0f, duration);
        }

        public Tween Shake(float duration, float strength)
        {
            return text.rectTransform.DOShakeAnchorPos(
                duration,
                strength);
        }
    }
}