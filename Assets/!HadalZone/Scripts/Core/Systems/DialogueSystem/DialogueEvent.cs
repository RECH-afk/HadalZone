using UnityEngine;

namespace RKS.HadalZone.Core.Dialogue
{
    [CreateAssetMenu(menuName = "Hadal Zone/Dialogue Event", fileName = "NewDialogueEvent")]
    public class DialogueEvent : ScriptableObject
    {
        private event System.Action _onRaised;

        public void Raise()
        {
            _onRaised?.Invoke();
        }

        public void AddListener(System.Action listener)
        {
            _onRaised += listener;
        }

        public void RemoveListener(System.Action listener)
        {
            _onRaised -= listener;
        }

        private void OnDisable()
        {
            _onRaised = null;
        }
    }
}
