using DG.Tweening;
using RKS.HadalZone.Core;
using UnityEngine;
using UnityEngine.UI;

namespace RKS.HadalZone.UI
{
    public class FirstOpenSceneController : RKSBehaviour
    {
        [Header("Windows")]
        [SerializeField] private RectTransform firstWindow;
        [SerializeField] private RectTransform secondWindow;

        [Header("UI Elements")]
        [SerializeField] private RectTransform toggleAgreeRectTransform;
        [SerializeField] private RectTransform buttonAgreeRectTransform;
        [SerializeField] private Toggle toggleAgree;
        [SerializeField] private Button buttonAgree;

        [Header("Positions")]
        [SerializeField] private float middleTogglePosX = 0f;
        [SerializeField] private float rightTogglePosX = 300f;
        [SerializeField] private float downButtonPosY = -200f;
        [SerializeField] private float topButtonPosY = -50f;

        [Header("Timing")]
        [SerializeField] private float delayBeforeSecondWindow = 5f;

        private Vector2 firstPos;
        private Vector2 secondPos;

        protected override void OnReady()
        {
            InitializeSave();
            CachePositions();
            PrepareUI();
            PlaySequence();
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

        private void PlaySequence()
        {
            Sequence seq = DOTween.Sequence();

            seq.Append(firstWindow.DOAnchorPos(firstPos, 0.25f).SetEase(Ease.OutCubic));

            seq.Append(firstWindow.DOShakeRotation(
                0.25f,
                new Vector3(0, 0, 14f),
                30,
                90,
                true
            ));

            seq.Append(firstWindow.DOPunchScale(Vector3.one * 0.06f, 0.15f, 10, 1));
            seq.AppendInterval(delayBeforeSecondWindow);
            seq.Append(firstWindow.DOShakePosition(0.15f, 20f, 25));
            seq.Append(firstWindow.DOAnchorPos(firstPos + Vector2.down * 200f, 0.2f)
                .SetEase(Ease.InCubic));

            seq.AppendCallback(() =>
            {
                firstWindow.gameObject.SetActive(false);
                secondWindow.gameObject.SetActive(true);
            });

            seq.Append(secondWindow.DOAnchorPos(secondPos, 0.25f).SetEase(Ease.OutCubic));

            seq.Append(secondWindow.DOPunchScale(
                Vector3.one * 0.12f,
                0.25f,
                12,
                0.8f
            ));
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

        public void GoToMenuScene()
        {
            var data = Save.CurrentData;
            data.isPlayerAgreedPlay = true;
            Save.Write(data);

            Transition?.LoadScene("IsMenuScene");
        }
    }
}
