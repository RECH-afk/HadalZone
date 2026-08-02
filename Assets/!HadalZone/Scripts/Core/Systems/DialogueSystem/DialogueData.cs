using UnityEngine;

namespace RKS.HadalZone.Core.Dialogue
{
    [CreateAssetMenu(menuName = "Hadal Zone/Dialogue", fileName = "NewDialogue")]
    public class DialogueData : ScriptableObject
    {
        [Header("Dialogue")]
        public Node[] nodes;

        [Header("Behaviour")]
        [Tooltip("Блокировать управление игроком во время диалога.")]
        public bool pausePlayerOnDialogue = true;

        [Header("Events")]
        public DialogueEvent[] onStarted;
        public DialogueEvent[] onEnded;

        private void OnValidate()
        {
            if (nodes == null) return;

            for (int i = 0; i < nodes.Length; i++)
            {
                var n = nodes[i];
                if (n.choices == null) continue;

                for (int j = 0; j < n.choices.Length; j++)
                {
                    var c = n.choices[j];
                    if (c.nextNode < -1 || c.nextNode >= nodes.Length)
                        Debug.LogWarning(
                            $"[{name}] Choice #{j} в Node[{i}] ссылается на " +
                            $"несуществующий Node[{c.nextNode}].", this);
                }
            }
        }
    }

    [System.Serializable]
    public class Node
    {
        [Tooltip("Ключ локализации текста реплики.")]
        public string textKey;

        [Tooltip("Персонаж. Оставьте пустым для narrator.")]
        public ActorDefinition actor;

        [Tooltip("Символов в секунду. Actor.defaultCharsPerSecond может переопределить.")]
        [Range(1, 100)]
        public int charsPerSecond = 33;

        [Tooltip("Варианты ответа. Пусто = continue по клику/кнопке.")]
        public Choice[] choices;

        [Tooltip("Имя звука из SoundLibrary для голосовой линии.")]
        public string voiceLine;

        [Header("Node events")]
        public DialogueEvent[] onEnter;
        public DialogueEvent[] onExit;
        public DialogueEvent[] onDisplayComplete;
    }

    [System.Serializable]
    public class Choice
    {
        [Tooltip("Ключ локализации текста кнопки.")]
        public string textKey;

        [Tooltip("Индекс следующего узла. -1 завершает диалог.")]
        public int nextNode;

        [Header("Choice events")]
        public DialogueEvent[] onSelect;
    }
}
