using UnityEngine;

namespace RKS.HadalZone.Core.Dialogue
{
    [CreateAssetMenu(menuName = "Hadal Zone/Actor", fileName = "NewActor")]
    public class ActorDefinition : ScriptableObject
    {
        [Tooltip("Ключ локализации отображаемого имени.")]
        public string displayNameKey;

        [Tooltip("Ключ локализации описания (для журнала, записок и т.п.).")]
        public string bioKey;

        [Tooltip("Скорость печати (символов/сек). -1 = наследовать из ноды.")]
        [Range(-1, 100)]
        public int defaultCharsPerSecond = -1;
    }
}
