using DG.Tweening;
using UnityEngine;

namespace RKS.HadalZone.Core.Dialogue
{
    public class DialogueCameraController : MonoBehaviour
    {
        [Header("Camera")]
        [SerializeField] private Camera targetCamera;

        [Header("Markers")]
        [Tooltip("Пометки-пустышки в сцене. Расставьте их визуально через Transform.")]
        [SerializeField] private Transform[] markers;

        [Header("Default")]
        [SerializeField] private Transform defaultParent;
        [SerializeField] private Vector3 defaultLocalPosition;
        [SerializeField] private Vector3 defaultLocalRotation;

        private Tween _moveTween;
        private Tween _fovTween;
        private Tween _lookTween;

        private void Start()
        {
            if (targetCamera == null)
                targetCamera = Camera.main;

            if (defaultParent == null && targetCamera != null)
                defaultParent = targetCamera.transform.parent;

            CacheDefault();
        }

        private void CacheDefault()
        {
            var t = targetCamera.transform;
            if (defaultParent != null)
            {
                defaultLocalPosition = t.localPosition;
                defaultLocalRotation = t.localEulerAngles;
            }
            else
            {
                defaultLocalPosition = t.position;
                defaultLocalRotation = t.eulerAngles;
            }
        }

        // ——— Marker methods ———

        public void MoveToMarker(int index, float duration)
        {
            if (index < 0 || index >= markers.Length)
            {
                Debug.LogWarning($"[DialogueCameraController] Marker index {index} out of range.");
                return;
            }
            MoveToTransform(markers[index], duration);
        }

        public void JumpToMarker(int index)
        {
            if (index < 0 || index >= markers.Length) return;
            var m = markers[index];
            JumpToTransform(m);
        }

        // ——— Transform-based ———

        public void MoveToTransform(Transform target, float duration)
        {
            if (target == null) return;
            KillAll();
            Detach();

            var t = targetCamera.transform;
            _moveTween = t.DOMove(target.position, duration).SetEase(Ease.InOutSine);
            _moveTween = t.DORotate(target.eulerAngles, duration).SetEase(Ease.InOutSine);
        }

        public void JumpToTransform(Transform target)
        {
            if (target == null) return;
            KillAll();
            Detach();
            targetCamera.transform.SetPositionAndRotation(target.position, target.rotation);
        }

        // ——— LookAt ———

        public void LookAt(Transform target, float duration)
        {
            if (target == null) return;
            KillLook();
            _lookTween = targetCamera.transform.DOLookAt(target.position, duration)
                .SetEase(Ease.InOutSine);
        }

        // ——— FOV ———

        public void SetFOV(float fov, float duration)
        {
            if (targetCamera == null) return;
            KillFOV();
            _fovTween = targetCamera.DOFieldOfView(fov, duration)
                .SetEase(Ease.InOutSine);
        }

        // ——— Default ———

        public void ReturnToDefault(float duration)
        {
            KillAll();
            var t = targetCamera.transform;

            Sequence seq = DOTween.Sequence();
            if (defaultParent != null)
            {
                seq.Join(t.DOLocalMove(defaultLocalPosition, duration).SetEase(Ease.InOutSine));
                seq.Join(t.DOLocalRotate(defaultLocalRotation, duration).SetEase(Ease.InOutSine));
                seq.OnComplete(() => t.SetParent(defaultParent));
            }
            else
            {
                seq.Join(t.DOMove(defaultLocalPosition, duration).SetEase(Ease.InOutSine));
                seq.Join(t.DORotate(defaultLocalRotation, duration).SetEase(Ease.InOutSine));
            }
            seq.Play();
        }

        // ——— Internal ———

        private void Detach()
        {
            targetCamera.transform.SetParent(null);
        }

        private void KillAll()
        {
            KillMove(); KillFOV(); KillLook();
        }

        private void KillMove()
        {
            if (_moveTween != null) { _moveTween.Kill(); _moveTween = null; }
        }

        private void KillFOV()
        {
            if (_fovTween != null) { _fovTween.Kill(); _fovTween = null; }
        }

        private void KillLook()
        {
            if (_lookTween != null) { _lookTween.Kill(); _lookTween = null; }
        }

        private void OnDestroy()
        {
            KillAll();
        }

        private void OnDrawGizmosSelected()
        {
            if (markers == null) return;
            Gizmos.color = Color.cyan;
            for (int i = 0; i < markers.Length; i++)
            {
                if (markers[i] != null)
                {
                    Gizmos.DrawWireCube(markers[i].position, Vector3.one * 0.2f);
                    Gizmos.DrawRay(markers[i].position, markers[i].forward * 0.5f);
                }
            }
        }
    }
}
