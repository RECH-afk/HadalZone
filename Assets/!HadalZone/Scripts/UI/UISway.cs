using RKS.HadalZone.Core;
using UnityEngine;

namespace RKS.HadalZone.UI
{
    public class UISway : RKSBehaviour
    {
        [Header("Sway Settings")]
        [SerializeField] private float swayAmount = 20f;
        [SerializeField] private float swaySpeed = 6f;
        [SerializeField] private float maxOffset = 30f;

        private RectTransform rect;

        protected override void OnReady()
        {
            rect = GetComponent<RectTransform>();
        }

        protected override void Update()
        {
            Vector2 mousePos = Input.mousePosition;
            Vector2 screenCenter = new Vector2(Screen.width, Screen.height) * 0.5f;

            Vector2 offset = (screenCenter - mousePos) / screenCenter;
            offset *= swayAmount;
            offset = Vector2.ClampMagnitude(offset, maxOffset);

            rect.localPosition = Vector2.Lerp(
                rect.localPosition,
                offset,
                Time.deltaTime * swaySpeed
            );
        }
    }
}
