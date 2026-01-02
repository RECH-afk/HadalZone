using UnityEngine;
using DG.Tweening;
using RKS.HadalZone.Core;

namespace RKS.HadalZone.UI
{
    sealed class MenuController : RKSBehaviour
    {
        [SerializeField] private RectTransform logo;
        [SerializeField] private RectTransform buttonsContainer;
        [SerializeField] private RectTransform playContainer;
        [SerializeField] private RectTransform settingsContainer;
        [SerializeField] private RectTransform creditsContainer;

        [Header("Animation")]
        [SerializeField] private float moveDistance;
        [SerializeField] private float duration;
        [SerializeField] private Ease ease = Ease.OutExpo;

        [Header("Logo Animation")]
        [SerializeField] private float logoMoveUp;
        [SerializeField] private float logoScale;

        private Vector2 buttonsStartPos;
        private Vector2 logoStartPos;
        private Vector3 logoStartScale;

        private RectTransform currentScreen;

        private bool isTransitioning;

        protected override void OnReady()
        {
            buttonsStartPos = buttonsContainer.anchoredPosition;

            logoStartPos = logo.anchoredPosition;
            logoStartScale = logo.localScale;

            InitScreen(playContainer);
            InitScreen(settingsContainer);
            InitScreen(creditsContainer);

            Audio.Play("UnderwaterAmbience");
        }

        protected override void Update()
        {
            if (Input.GetKeyDown(KeyCode.Escape))
            {
                HandleEscape();
            }
        }

        void InitScreen(RectTransform screen)
        {
            screen.anchoredPosition -= Vector2.up * moveDistance;
        }

        public void ShowPlay() => ShowScreen(playContainer);
        public void ShowSettings() => ShowScreen(settingsContainer);
        public void ShowCredits() => ShowScreen(creditsContainer);
        public void Quit()
        {
            Application.Quit();
        }
        void HandleEscape()
        {
            if (isTransitioning)
                return;

            if (currentScreen != null)
            {
                HideCurrentScreen();
                return;
            }

           // Quit();
        }


        public void BackToMenu() => HideCurrentScreen();

        void ShowScreen(RectTransform screen)
        {
            if (isTransitioning)
                return;

            isTransitioning = true;

            if (currentScreen != null)
                HideScreen(currentScreen);

            currentScreen = screen;

            buttonsContainer
                .DOAnchorPos(buttonsStartPos - Vector2.up * moveDistance, duration)
                .SetEase(ease);

            screen
                .DOAnchorPos(screen.anchoredPosition + Vector2.up * moveDistance, duration)
                .SetEase(ease);

            AnimateLogoUp();

            DOVirtual.DelayedCall(duration, () => isTransitioning = false);
        }


        void HideCurrentScreen()
        {
            if (isTransitioning || currentScreen == null)
                return;

            isTransitioning = true;

            buttonsContainer
                .DOAnchorPos(buttonsStartPos, duration)
                .SetEase(ease);

            HideScreen(currentScreen);

            currentScreen = null;

            AnimateLogoDown();

            DOVirtual.DelayedCall(duration, () => isTransitioning = false);
        }


        void HideScreen(RectTransform screen)
        {
            screen
                .DOAnchorPos(screen.anchoredPosition - Vector2.up * moveDistance, duration)
                .SetEase(ease);
        }

        void AnimateLogoUp()
        {
            logo
                .DOAnchorPos(logoStartPos + Vector2.up * logoMoveUp, duration)
                .SetEase(ease);

            logo
                .DOScale(logoStartScale * logoScale, duration)
                .SetEase(ease);
        }

        void AnimateLogoDown()
        {
            logo
                .DOAnchorPos(logoStartPos, duration)
                .SetEase(ease);

            logo
                .DOScale(logoStartScale, duration)
                .SetEase(ease);
        }
    }

}
