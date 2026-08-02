using EasyPeasyFirstPersonController;
using RKS.HadalZone.Core.Dialogue;
using UnityEngine;
using Zenject;

namespace RKS.HadalZone.Player.Examples
{
    public class InteractablePickup : MonoBehaviour, IInteractable
    {
        [Header("Settings")]
        [SerializeField] private string itemName = "Item";
        [SerializeField] private DialogueData dialogue;
        [SerializeField] private bool destroyOnPickup = true;

        [Header("Effects")]
        [SerializeField] private GameObject effect;

        [Inject] private DialogueManager _mgr;

        public string Prompt => "Take " + itemName;
        public bool CanInteract => true;

        public void OnInteract(PlayerController player)
        {
            if (dialogue != null && _mgr != null)
            {
                _mgr.StartDialogue(dialogue);
                return;
            }

            if (effect != null)
                Instantiate(effect, transform.position, Quaternion.identity);

            if (destroyOnPickup)
                Destroy(gameObject);
        }
    }
}
