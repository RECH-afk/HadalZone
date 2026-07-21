using DG.Tweening;
using RKS.HadalZone.Core;
using System;
using TMPro;
using UnityEngine;
using Random = UnityEngine.Random;
using UnityEngine.UI;

namespace RKS.HadalZone.UI
{
    public class FirstOpenSceneController : RKSBehaviour
    {
        [Header("Windows")]
        [SerializeField] private RectTransform firstWindow;
        [SerializeField] private RectTransform secondWindow;
        [SerializeField] private RectTransform thirdWindow;

        [Header("UI Elements")]
        [SerializeField] private RectTransform toggleAgreeRectTransform;
        [SerializeField] private RectTransform buttonAgreeRectTransform;
        [SerializeField] private Toggle toggleAgree;
        [SerializeField] private Button buttonAgree;
        [SerializeField] private Image progressBar;
        [SerializeField] private TMP_Text progressText;

        [Header("Positions")]
        [SerializeField] private float middleTogglePosX = 0f;
        [SerializeField] private float rightTogglePosX = 300f;
        [SerializeField] private float downButtonPosY = -200f;
        [SerializeField] private float topButtonPosY = -50f;

        [Header("Other")]
        [SerializeField] private float delayBeforeSecondWindow = 5f;
        [SerializeField] private ShaderVariantCollection shaderVariants;

        private Vector2 firstPos;
        private Vector2 secondPos;
        private Sequence playSequence;

        protected override void OnReady()
        {
            InitializeSave();
            CachePositions();
            PrepareUI();
            PlaySequence();

            Audio.Play("UnderwaterAmbience");
        }

        private void CachePositions()
        {
            firstPos = firstWindow.anchoredPosition;
            secondPos = secondWindow.anchoredPosition;
        }

        private void PrepareUI()
        {
            firstWindow.gameObject.SetActive(true);
            firstWindow.anchoredPosition = firstPos + Vector2.up * 450f;
            firstWindow.localRotation = Quaternion.identity;
            firstWindow.localScale = Vector3.one;

            secondWindow.gameObject.SetActive(false);
            secondWindow.anchoredPosition = secondPos + Vector2.up * 300f;
            secondWindow.localScale = Vector3.one;

            toggleAgreeRectTransform.anchoredPosition =
                new Vector2(middleTogglePosX, toggleAgreeRectTransform.anchoredPosition.y);

            buttonAgreeRectTransform.anchoredPosition =
                new Vector2(buttonAgreeRectTransform.anchoredPosition.x, downButtonPosY);

            buttonAgree.interactable = false;
            toggleAgree.onValueChanged.AddListener(OnToggleValueChanged);
        }
        private void UpdateProgress(float value)
        {
            progressBar.fillAmount = value;
            progressText.text = $"<wave amp=3>{Mathf.RoundToInt(value * 100)}%</wave>";
        }

        private void StartProgressBarLoading()
        {
            UpdateProgress(0f);

            shaderVariants.WarmUp();

            Sequence loading = DOTween.Sequence();

            float current = 0f;

            while (current < 0.98f)
            {
                float step = Random.Range(0.02f, 0.10f);
                current = Mathf.Min(current + step, 0.98f);

                float duration = Random.Range(0.15f, 0.5f);

                loading.Append(
                    DOTween.To(
                        () => progressBar.fillAmount,
                        x =>
                        {
                            UpdateProgress(x);
                        },
                        current,
                        duration)
                    .SetEase(Ease.OutQuad));

                if (Random.value > 0.5f)
                    loading.AppendInterval(Random.Range(0.1f, 0.5f));
            }

            loading.Append(
                DOTween.To(
                        () => progressBar.fillAmount,
                        x =>
                        {
                            UpdateProgress(x);
                        },
                        1f,
                        Random.Range(0.8f, 1.4f))
                    .SetEase(Ease.InOutQuad));

            loading.OnComplete(() =>
            {
                GC.Collect();
                Resources.UnloadUnusedAssets();

                UpdateProgress(1f);

                Save.CurrentData.isShadersCompiled = true;
                Save.Write();

                Transition?.LoadScene("IsMenuScene");
            });
        }

        private void PlaySequence()
        {
            playSequence?.Kill();

            playSequence = DOTween.Sequence();

            playSequence.Append(
                firstWindow.DOAnchorPos(firstPos, 0.3f)
                    .SetEase(Ease.OutBack));

            playSequence.Join(
                firstWindow.DOScale(1f, 0.3f)
                    .From(0.85f)
                    .SetEase(Ease.OutBack));

            playSequence.Append(
                firstWindow.DOShakeRotation(
                    0.25f,
                    new Vector3(0f, 0f, 12f),
                    25,
                    90,
                    true));

            playSequence.Append(
                firstWindow.DOPunchScale(
                    Vector3.one * 0.08f,
                    0.2f,
                    10,
                    1));

            playSequence.AppendInterval(delayBeforeSecondWindow);

            playSequence.Append(
                firstWindow.DOShakePosition(
                    0.15f,
                    18f,
                    20));

            playSequence.Append(
                firstWindow.DOAnchorPos(
                    firstPos + Vector2.down * 250f,
                    0.25f)
                    .SetEase(Ease.InBack));

            playSequence.Join(
                firstWindow.DOScale(0.9f, 0.25f));

            playSequence.AppendCallback(() =>
            {
                firstWindow.gameObject.SetActive(false);

                secondWindow.gameObject.SetActive(true);
                secondWindow.anchoredPosition = secondPos + Vector2.up * 300f;
                secondWindow.localScale = Vector3.one * 0.85f;
                secondWindow.localRotation = Quaternion.identity;
            });

            playSequence.Append(
                secondWindow.DOAnchorPos(secondPos, 0.35f)
                    .SetEase(Ease.OutBack));

            playSequence.Join(
                secondWindow.DOScale(1f, 0.35f)
                    .SetEase(Ease.OutBack));

            playSequence.Append(
                secondWindow.DOPunchScale(
                    Vector3.one * 0.12f,
                    0.25f,
                    12,
                    0.8f));

            playSequence.AppendCallback(() =>
            {
                playSequence.Pause();
            });

            playSequence.Append(
                secondWindow.DOShakePosition(
                    0.15f,
                    18f,
                    20));

            playSequence.Append(
                secondWindow.DOAnchorPos(
                    secondPos + Vector2.down * 250f,
                    0.25f)
                    .SetEase(Ease.InBack));

            playSequence.Join(
                secondWindow.DOScale(0.9f, 0.25f));

            playSequence.AppendCallback(() =>
            {
                secondWindow.gameObject.SetActive(false);

                thirdWindow.gameObject.SetActive(true);
                thirdWindow.anchoredPosition = secondPos + Vector2.up * 300f;
                thirdWindow.localScale = Vector3.one * 0.85f;
                thirdWindow.localRotation = Quaternion.identity;
            });

            playSequence.Append(
                thirdWindow.DOAnchorPos(secondPos, 0.35f)
                    .SetEase(Ease.OutBack));

            playSequence.Join(
                thirdWindow.DOScale(1f, 0.35f)
                    .SetEase(Ease.OutBack));

            playSequence.Append(
                thirdWindow.DOPunchScale(
                    Vector3.one * 0.12f,
                    0.25f,
                    12,
                    0.8f));

            playSequence.AppendCallback(() =>
            {
                playSequence.Pause();
                StartProgressBarLoading();
            });
        }

        private void InitializeSave()
        {
            Save.Load();

            if (Save.CurrentData == null)
            {
                Debug.LogWarning("[FirstOpenSceneController] Save null → create new.");
                Save.Write();
            }
        }

        private void OnToggleValueChanged(bool isOn)
        {
            var data = Save.CurrentData;

            if (isOn)
            {
                buttonAgree.interactable = true;

                toggleAgreeRectTransform.DOAnchorPosX(rightTogglePosX, 0.25f);
                buttonAgreeRectTransform.DOAnchorPosY(topButtonPosY, 0.25f);

                data.isPlayerAgreedPlay = true;
            }
            else
            {
                buttonAgree.interactable = false;

                toggleAgreeRectTransform.DOAnchorPosX(middleTogglePosX, 0.25f);
                buttonAgreeRectTransform.DOAnchorPosY(downButtonPosY, 0.25f);

                data.isPlayerAgreedPlay = false;
            }

            Save.Write();
        }

        public void AgreeButton()
        {
            buttonAgree.interactable = false;

            toggleAgreeRectTransform.DOAnchorPosX(middleTogglePosX, 0.25f);
            buttonAgreeRectTransform.DOAnchorPosY(downButtonPosY, 0.25f);

            Save.CurrentData.isPlayerAgreedPlay = true;
            Save.Write();

            playSequence.Play();
        }   
    }
}
