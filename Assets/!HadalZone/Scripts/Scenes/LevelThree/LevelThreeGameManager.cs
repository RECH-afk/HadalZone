using DG.Tweening;
using RKS.HadalZone.Core;
using TMPro;
using UnityEngine;

public class LevelThreeGameManager : RKSBehaviour
{
    [Header("References")]
    [SerializeField] private TextMeshProUGUI textMesh;
    [SerializeField] private GameObject lightningLight;

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
        lightningLight.SetActive(false);
        PlayAnimation();
    }

    public void PlayAnimation()
    {
        textMesh.alpha = 0f;
        textMesh.characterSpacing = startCharacterSpacing;

        Sequence sequence = DOTween.Sequence();

        sequence.AppendInterval(showDelay);

        sequence.AppendCallback(() =>
        {
            textMesh.alpha = 1f;
        });

        sequence.AppendInterval(shakeDelay);

        sequence.AppendCallback(() =>
        {
            LightningFlash();
        });

        sequence.Append(
            textMesh.rectTransform.DOShakeAnchorPos(
                shakeDuration,
                shakeStrength,
                20,
                90,
                false,
                true
            )
        );
        sequence.Append(
            DOTween.To(
                () => textMesh.characterSpacing,
                x => textMesh.characterSpacing = x,
                0f,
                spacingDuration
            )
            .SetEase(Ease.InOutSine)
        );

        sequence.AppendCallback(() =>
        {
            LightningFlash();

            textMesh.rectTransform.DOShakeAnchorPos(
                shakeDuration,
                shakeStrength,
                20,
                90,
                false,
                true
            );

            DOVirtual.DelayedCall(shakeDuration * 0.5f, () =>
            {
                textMesh.alpha = 0f;
            });
        });
    }

    private void LightningFlash()
    {
        Sequence flash = DOTween.Sequence();

        Audio.Play("Thunder");

        flash.AppendCallback(() =>
        {
            lightningLight.SetActive(true);
        });

        flash.AppendInterval(0.08f);

        flash.AppendCallback(() =>
        {
            lightningLight.SetActive(false);
        });

        flash.AppendInterval(0.09f);

        flash.AppendCallback(() =>
        {
            lightningLight.SetActive(true);
        });

        flash.AppendInterval(0.1f);

        flash.AppendCallback(() =>
        {
            lightningLight.SetActive(false);
        });
    }
}