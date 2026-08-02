using System;
using UnityEngine;

namespace RKS.HadalZone.Core.Dialogue
{
    public class DialogueManager : RKSBehaviour
    {
        public bool IsActive { get; private set; }
        public bool IsPaused { get; private set; }
        public bool IsCurrentNodeLast =>
            _data != null && _index >= _data.nodes.Length - 1;

        public event Action<DialogueData> OnStarted;
        public event Action OnEnded;
        public event Action<Node> OnNodeChanged;
        public event Action<bool> OnDialoguePause;

        private DialogueData _data;
        private int _index;
        private Action _onComplete;

        public void StartDialogue(DialogueData data, Action onComplete = null)
        {
            if (IsActive || data == null || data.nodes.Length == 0) return;

            _data = data;
            _index = 0;
            _onComplete = onComplete;
            IsActive = true;
            SetPause(true);
            FireEvents(_data.onStarted);
            OnStarted?.Invoke(data);
            ShowNode();
        }

        public void SelectChoice(int index)
        {
            if (!IsActive) return;

            var node = _data.nodes[_index];
            if (node.choices == null || index < 0 || index >= node.choices.Length) return;

            FireEvents(node.choices[index].onSelect);

            int next = node.choices[index].nextNode;
            LeaveNode();

            if (next < 0)
                EndDialogue();
            else
                GoTo(next);
        }

        public void NextNode()
        {
            if (!IsActive) return;

            var node = _data.nodes[_index];
            if (node.choices != null && node.choices.Length > 0) return;

            LeaveNode();

            if (IsCurrentNodeLast)
                EndDialogue();
            else
                GoTo(_index + 1);
        }

        public void EndDialogue()
        {
            if (!IsActive) return;

            IsActive = false;
            SetPause(false);
            FireEvents(_data.onEnded);

            _data = null;
            _index = 0;

            OnEnded?.Invoke();

            _onComplete?.Invoke();
            _onComplete = null;
        }

        public void NotifyDisplayComplete()
        {
            var node = GetCurrentNode();
            if (node != null)
                FireEvents(node.onDisplayComplete);
        }

        public Node GetCurrentNode()
        {
            if (_data == null || _index < 0 || _index >= _data.nodes.Length)
                return null;
            return _data.nodes[_index];
        }

        private void ShowNode()
        {
            if (_index < 0 || _index >= _data.nodes.Length)
            {
                EndDialogue();
                return;
            }

            var node = _data.nodes[_index];
            FireEvents(node.onEnter);
            OnNodeChanged?.Invoke(node);
        }

        private void LeaveNode()
        {
            var node = GetCurrentNode();
            if (node != null)
                FireEvents(node.onExit);
        }

        private void GoTo(int index)
        {
            _index = index;
            ShowNode();
        }

        private static void FireEvents(DialogueEvent[] events)
        {
            if (events == null) return;
            for (int i = 0; i < events.Length; i++)
                events[i]?.Raise();
        }

        private void SetPause(bool paused)
        {
            IsPaused = paused;
            if (_data != null && _data.pausePlayerOnDialogue)
                OnDialoguePause?.Invoke(paused);
        }
    }
}
