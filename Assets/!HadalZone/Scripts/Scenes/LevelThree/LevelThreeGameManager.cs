using DG.Tweening;
using RKS.HadalZone.Core;
using RKS.HadalZone.UI;
using RKS.HadalZone.Environment;
using UnityEngine;

namespace RKS.HadalZone.Core.Managers
{
    public class LevelThreeGameManager : RKSBehaviour
    {
        [Header("References")]
        [SerializeField] private NarrativeText narrativeText;
        [SerializeField] private LightningEffect lightningEffect;

        [Header("Timing")]
        [SerializeField] private float showDelay = 3f;
        [SerializeField] private float shakeDelay = 2f;

        [Header("Shake")]
        [SerializeField] private float shakeDuration = 0.5f;
        [SerializeField] private float shakeStrength = 20f;

        [Header("Spacing")]
        [SerializeField] private float startCharacterSpacing = 20f;
        [SerializeField] private float spacingDuration = 2f;

        protected override void OnReady()
        {
            narrativeText.Hide();
            narrativeText.SetSpacing(startCharacterSpacing);

            PlayAnimation();
        }

        private void PlayAnimation()
        {
            Sequence sequence = DOTween.Sequence();

            sequence.AppendInterval(showDelay);

            sequence.AppendCallback(narrativeText.Show);

            sequence.AppendInterval(shakeDelay);

            sequence.AppendCallback(() =>
            {
                lightningEffect.Play();
            });

            sequence.Append(
                narrativeText.Shake(
                    shakeDuration,
                    shakeStrength
                )
            );

            sequence.Append(
                narrativeText.AnimateSpacing(
                    0f,
                    spacingDuration
                )
            );

            sequence.AppendCallback(() =>
            {
                lightningEffect.Play();

                narrativeText.Shake(
                    shakeDuration,
                    shakeStrength
                );

                DOVirtual.DelayedCall(
                    shakeDuration * 0.5f,
                    narrativeText.Hide
                );
            });
        }
    }
}