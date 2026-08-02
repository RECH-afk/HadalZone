using DG.Tweening;
using EasyPeasyFirstPersonController;
using UnityEngine;

namespace RKS.HadalZone.Player.Examples
{
    public class InteractableDoor : MonoBehaviour, IInteractable
    {
        public enum DoorState { Closed, Open, Locked, Jammed }

        [Header("Settings")]
        [SerializeField] private string openPrompt = "Open";
        [SerializeField] private string closePrompt = "Close";
        [SerializeField] private Vector3 openRotation = new Vector3(0f, -90f, 0f);
        [SerializeField] private float duration = 0.5f;
        [SerializeField] private DoorState initialState;

        [Header("References")]
        [SerializeField] private Transform hinge;
        [SerializeField] private AudioSource audioSource;
        [SerializeField] private AudioClip openSound;
        [SerializeField] private AudioClip closeSound;
        [SerializeField] private AudioClip lockedSound;

        [Header("Auto-Close")]
        [SerializeField] private float autoCloseDelay;

        private DoorState _state;
        private bool _isAnimating;
        private Tween _autoCloseTween;

        public string Prompt
        {
            get
            {
                return _state switch
                {
                    DoorState.Locked => "Locked",
                    DoorState.Jammed => "Stuck",
                    DoorState.Open => closePrompt,
                    _ => openPrompt
                };
            }
        }

        public bool CanInteract => _state != DoorState.Jammed && !_isAnimating;

        private void Awake()
        {
            if (hinge == null) hinge = transform;
            _state = initialState;
        }

        public void OnInteract(PlayerController player)
        {
            if (_isAnimating) return;

            switch (_state)
            {
                case DoorState.Locked:
                    PlaySound(lockedSound);
                    break;

                case DoorState.Open:
                    Close();
                    break;

                case DoorState.Closed:
                    Open();
                    break;
            }
        }

        private void Open()
        {
            _state = DoorState.Open;
            _isAnimating = true;
            PlaySound(openSound);

            hinge.DOKill();
            hinge.DOLocalRotate(openRotation, duration)
                .SetEase(Ease.OutQuad)
                .OnComplete(() => _isAnimating = false);

            if (autoCloseDelay > 0f)
            {
                _autoCloseTween?.Kill();
                _autoCloseTween = DOVirtual.DelayedCall(autoCloseDelay, Close);
            }
        }

        private void Close()
        {
            _state = DoorState.Closed;
            _isAnimating = true;
            PlaySound(closeSound);

            _autoCloseTween?.Kill();

            hinge.DOKill();
            hinge.DOLocalRotate(Vector3.zero, duration)
                .SetEase(Ease.InQuad)
                .OnComplete(() => _isAnimating = false);
        }

        public void SetLocked(bool locked)
        {
            if (_state == DoorState.Locked && !locked)
                _state = DoorState.Closed;
            else if (locked && _state != DoorState.Jammed)
                _state = DoorState.Locked;
        }

        public void Unlock() => SetLocked(false);

        private void PlaySound(AudioClip clip)
        {
            if (audioSource != null && clip != null)
                audioSource.PlayOneShot(clip);
        }

        private void OnDestroy()
        {
            _autoCloseTween?.Kill();
            hinge.DOKill();
        }
    }
}
