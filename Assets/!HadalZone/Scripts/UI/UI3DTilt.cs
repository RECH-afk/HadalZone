using UnityEngine;
using UnityEngine.EventSystems;
using RKS.HadalZone.Core;

namespace RKS.HadalZone.UI
{
    sealed class UI3DTilt : RKSBehaviour, IPointerEnterHandler, IPointerExitHandler, IPointerMoveHandler
    {
        [Header("Tilt Settings")]
        public float maxTilt = 15f;
        public float smooth = 10f;

        RectTransform rect;
        Quaternion startRotation;
        Vector3 startPosition;

        bool hovering;
        Vector2 localCursor;

        protected override void OnReady()
        {
            rect = GetComponent<RectTransform>();
            startRotation = rect.localRotation;
            startPosition = rect.localPosition;
        }

        protected override void Update()
        {
            Quaternion targetRotation = startRotation;

            if (hovering)
            {
                float x = ApplyEdgeResponse(localCursor.x);
                float y = ApplyEdgeResponse(localCursor.y);

                float tiltX = -y * maxTilt;
                float tiltY = x * maxTilt;

                targetRotation = Quaternion.Euler(tiltX, tiltY, 0f);
            }

            rect.localRotation = Quaternion.Lerp(
                rect.localRotation,
                targetRotation,
                Time.deltaTime * smooth
            );
        }

        float ApplyEdgeResponse(float value)
        {
            float sign = Mathf.Sign(value);
            float abs = Mathf.Abs(value);

            float curved = Mathf.SmoothStep(0f, 1f, abs);

            return curved * sign;
        }

        #region Pointer Events
        public void OnPointerEnter(PointerEventData eventData)
        {
            hovering = true;
        }

        public void OnPointerExit(PointerEventData eventData)
        {
            hovering = false;
        }

        public void OnPointerMove(PointerEventData eventData)
        {
            RectTransformUtility.ScreenPointToLocalPointInRectangle(
                rect,
                eventData.position,
                eventData.pressEventCamera,
                out Vector2 localPoint
            );

            // нормализация в диапазон -1 .. 1
            localCursor = new Vector2(
                (localPoint.x / (rect.rect.width * 0.5f)),
                (localPoint.y / (rect.rect.height * 0.5f))
            );

            localCursor = Vector2.ClampMagnitude(localCursor, 1f);
        }

        #endregion

    }
}