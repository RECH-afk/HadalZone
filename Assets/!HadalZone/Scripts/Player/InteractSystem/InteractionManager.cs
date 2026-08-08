using EasyPeasyFirstPersonController;
using RKS.HadalZone.Core;
using UnityEngine;
using UnityEngine.Events;
using Zenject;

namespace RKS.HadalZone.Player
{
    public class InteractionManager : RKSBehaviour
    {
        [SerializeField] private float maxDistance = 3f;

        [Header("Events")]
        public UnityEvent onInteractableFound;
        public UnityEvent onInteractableLost;

        public IInteractable Current { get; private set; }

        [Inject] private PlayerController _playerController;
        private Transform _playerCamera;

        protected override void OnInjected()
        {
            _playerCamera = _playerController.GetComponentInChildren<Camera>().transform;
        }

        protected override void Update()
        {
            if (_playerCamera == null) return;

            RaycastHit hit;
            bool hitSomething = Physics.Raycast(
                _playerCamera.position, _playerCamera.forward,
                out hit, maxDistance);

            IInteractable interactable = null;
            if (hitSomething)
                interactable = hit.collider.GetComponentInParent<IInteractable>();

            if (interactable != null && interactable.CanInteract)
            {
                if (Current != interactable)
                {
                    Current = interactable;
                    onInteractableFound.Invoke();
                }

                if (Input.GetButtonDown("Submit") || Input.GetKeyDown(KeyCode.E))
                {
                    if (_playerController != null)
                        interactable.OnInteract(_playerController);
                }
            }
            else if (Current != null)
            {
                Current = null;
                onInteractableLost.Invoke();
            }
        }
    }
}
