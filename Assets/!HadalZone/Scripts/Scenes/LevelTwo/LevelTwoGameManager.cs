using DG.Tweening;
using RKS.HadalZone.Core;
using RKS.HadalZone.UI;
using UnityEngine;

namespace RKS.HadalZone.Core.Managers
{
    public class LevelTwoGameManager : RKSBehaviour
    {
        [Header("References")]
        [SerializeField] private NarrativeText narrativeText;

        [Header("Fade In")]
        [SerializeField] private float fadeInDuration = 3f;

        [Header("Spacing")]
        [SerializeField] private float startCharacterSpacing = 20f;
        [SerializeField] private float spacingDuration = 2f;

        [Header("Fade Out")]
        [SerializeField] private float fadeOutStartSpacing = 5f;

        protected override void OnReady()
        {
            PlayAnimation();
        }

        private void PlayAnimation()
        {
            narrativeText.Hide();
            narrativeText.SetSpacing(startCharacterSpacing);

            float spacingStartTime = fadeInDuration * 0.5f;

            float fadeOutDelay =
                spacingDuration *
                ((startCharacterSpacing - fadeOutStartSpacing)
                    / startCharacterSpacing);

            float remainingFadeTime =
                spacingDuration - fadeOutDelay;

            Sequence sequence = DOTween.Sequence();

            sequence.Append(
                narrativeText.FadeIn(fadeInDuration)
            );

            sequence.Insert(
                spacingStartTime,
                narrativeText
                    .ResetSpacing(spacingDuration)
                    .SetEase(Ease.InOutSine)
            );

            sequence.Insert(
                spacingStartTime + fadeOutDelay,
                narrativeText.FadeOut(remainingFadeTime)
            );
        }
    }
}