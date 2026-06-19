using UnityEngine;

public class CameraUISway : MonoBehaviour
{
    [SerializeField] private RectTransform uiSwayObject;
    [SerializeField] private Transform targetCamera;

    [Header("Sway Settings")]
    [SerializeField] private float swayAmount = 50f;
    [SerializeField] private float smoothTime = 5f;
    [SerializeField] private float returnSpeed = 2f;

    private Vector2 targetPosition;
    private Vector2 currentVelocity;
    private Vector3 lastCameraRotation;

    void LateUpdate()
    {
        Vector3 currentCameraRotation = targetCamera.eulerAngles;
        
        float deltaX = Mathf.DeltaAngle(lastCameraRotation.y, currentCameraRotation.y);
        float deltaY = Mathf.DeltaAngle(lastCameraRotation.x, currentCameraRotation.x);

        float moveX = -deltaX * swayAmount;
        float moveY = deltaY * swayAmount;

        targetPosition.x = Mathf.Clamp(moveX, -swayAmount, swayAmount);
        targetPosition.y = Mathf.Clamp(moveY, -swayAmount, swayAmount);

        if (deltaX == 0 && deltaY == 0)
        {
            targetPosition = Vector2.MoveTowards(targetPosition, Vector2.zero, Time.deltaTime * returnSpeed);
        }

        uiSwayObject.anchoredPosition = Vector2.SmoothDamp(uiSwayObject.anchoredPosition, targetPosition, ref currentVelocity, 1f / smoothTime);

        lastCameraRotation = currentCameraRotation;
    }
}
