using EasyPeasyFirstPersonController;
using UnityEngine;
using UnityEngine.Events;

namespace RKS.HadalZone.Player
{
    public class InteractionManager : MonoBehaviour
    {
        [Header("References")]
        [SerializeField] private Transform playerCamera;
        [SerializeField] private float maxDistance = 3f;

        [Header("Events")]
        public UnityEvent onInteractableFound;
        public UnityEvent onInteractableLost;

        public IInteractable Current { get; private set; }

        private PlayerController _player;

        private void Start()
        {
            _player = FindObjectOfType<PlayerController>();
            if (playerCamera == null && _player != null)
                playerCamera = _player.playerCamera;
        }

        private void Update()
        {
            if (playerCamera == null) return;

            RaycastHit hit;
            bool hitSomething = Physics.Raycast(
                playerCamera.position, playerCamera.forward,
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
                    if (_player != null)
                        interactable.OnInteract(_player);
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
