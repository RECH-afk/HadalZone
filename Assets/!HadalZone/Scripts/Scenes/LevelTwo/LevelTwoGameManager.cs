using DG.Tweening;
using RKS.HadalZone.Core;
using TMPro;
using UnityEngine;

public class LevelTwoGameManager : RKSBehaviour
{
    [Header("References")]
    [SerializeField] private TextMeshProUGUI textMesh;

    [Header("Fade In")]
    [SerializeField] private float fadeInDuration = 3f;

    [Header("Spacing")]
    [SerializeField] private float startCharacterSpacing = 20f;
    [SerializeField] private float spacingDuration = 2f;
    private bool spacingStarted;

    [Header("Fade Out")]
    [SerializeField] private float fadeOutStartSpacing = 5f;

    private void Start()
    {
        PlayAnimation();
    }

    public void PlayAnimation()
    {
        spacingStarted = false;

        textMesh.alpha = 0f;
        textMesh.characterSpacing = startCharacterSpacing;

        textMesh.DOFade(1f, fadeInDuration)
            .OnUpdate(() =>
            {
                if (!spacingStarted && textMesh.alpha >= 0.5f)
                {
                    spacingStarted = true;

                    StartSpacingAnimation();
                }
            });
    }

    private void StartSpacingAnimation()
    {
        DOTween.To(
            () => textMesh.characterSpacing,
            x => textMesh.characterSpacing = x,
            0f,
            spacingDuration
        )
        .SetEase(Ease.InOutSine)
        .OnUpdate(() =>
        {
            if (textMesh.characterSpacing <= fadeOutStartSpacing &&
                !DOTween.IsTweening("FadeOut"))
            {
                float remainingTime =
                    spacingDuration *
                    (textMesh.characterSpacing / startCharacterSpacing);

                textMesh.DOFade(0f, remainingTime)
                    .SetId("FadeOut");
            }
        });
    }
}
