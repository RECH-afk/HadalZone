using EasyPeasyFirstPersonController;
using RKS.HadalZone.Player;
using UnityEngine;
using UnityEngine.Events;
using Zenject;

namespace RKS.HadalZone.Core.Dialogue
{
    public class DialogueTrigger : MonoBehaviour, IInteractable
    {
        public enum ActivationMode { OnInteract, OnTriggerEnter }

        [Header("Dialogue")]
        [SerializeField] private DialogueData data;
        [SerializeField] private string prompt = "Talk";

        [Header("Behaviour")]
        [SerializeField] private ActivationMode activation = ActivationMode.OnInteract;
        [SerializeField] private bool once = true;
        [SerializeField] private float cooldown;

        [Header("Events")]
        public UnityEvent onDialogueStarted;
        public UnityEvent onDialogueEnded;

        [Header("Indicator")]
        [SerializeField] private GameObject indicator;

        [Inject] private DialogueManager _mgr;

        private bool _used;
        private float _lastUseTime;

        public string Prompt => activation == ActivationMode.OnInteract ? prompt : "";
        public bool CanInteract => activation == ActivationMode.OnInteract && !_used && data != null && _mgr != null && !_mgr.IsActive;

        private void OnTriggerEnter(Collider other)
        {
            if (!other.CompareTag("Player")) return;

            if (activation == ActivationMode.OnTriggerEnter)
            {
                if (_used) return;
                if (Time.time - _lastUseTime < cooldown) return;
                Trigger();
            }
            else if (indicator != null)
            {
                indicator.SetActive(true);
            }
        }

        private void OnTriggerExit(Collider other)
        {
            if (!other.CompareTag("Player")) return;
            if (indicator != null) indicator.SetActive(false);
        }

        public void OnInteract(PlayerController player) => Trigger();

        public void Trigger()
        {
            if (_used || data == null || _mgr == null) return;
            if (_mgr.IsActive) return;

            if (once) _used = true;
            _lastUseTime = Time.time;

            onDialogueStarted?.Invoke();
            _mgr.OnEnded += OnDialogueComplete;
            _mgr.StartDialogue(data);
        }

        private void OnDialogueComplete()
        {
            _mgr.OnEnded -= OnDialogueComplete;

            onDialogueEnded?.Invoke();

            if (indicator != null)
                indicator.SetActive(false);
        }

        private void OnDestroy()
        {
            if (_mgr != null)
                _mgr.OnEnded -= OnDialogueComplete;
        }
    }
}
