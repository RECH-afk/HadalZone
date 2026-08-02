using UnityEngine;
using UnityEngine.Events;

namespace RKS.HadalZone.Core.Dialogue
{
    public class DialogueEventListener : MonoBehaviour
    {
        [SerializeField] private DialogueEvent dialogueEvent;
        [SerializeField] private UnityEvent response;

        private void OnEnable()
        {
            if (dialogueEvent != null)
                dialogueEvent.AddListener(InvokeResponse);
        }

        private void OnDisable()
        {
            if (dialogueEvent != null)
                dialogueEvent.RemoveListener(InvokeResponse);
        }

        private void InvokeResponse()
        {
            response.Invoke();
        }
    }
}
