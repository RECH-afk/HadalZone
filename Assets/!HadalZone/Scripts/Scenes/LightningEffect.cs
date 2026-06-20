using DG.Tweening;
using UnityEngine;
using RKS.HadalZone.Core;

namespace RKS.HadalZone.Environment
{
    public class LightningEffect : RKSBehaviour
    {
        private Light lightningLight;

        protected override void OnReady()
        {
            lightningLight = GetComponent<Light>();
        }

        public Sequence Play()
        {
            Sequence seq = DOTween.Sequence();

            Audio.Play("Thunder");

            seq.AppendCallback(() => lightningLight.enabled = true);
            seq.AppendInterval(0.08f);

            seq.AppendCallback(() => lightningLight.enabled = false);
            seq.AppendInterval(0.09f);

            seq.AppendCallback(() => lightningLight.enabled = true);
            seq.AppendInterval(0.1f);

            seq.AppendCallback(() => lightningLight.enabled = false);

            return seq;
        }
    }
}