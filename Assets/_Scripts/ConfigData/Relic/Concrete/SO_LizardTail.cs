using UnityEngine;

namespace _DreamKeep.ConfigData
{
    /// <summary>蜥蜴尾巴：每场战斗第一次受到致命伤害时，把生命改为最大生命的指定比例（至少保留 1 点）。</summary>
    [CreateAssetMenu(fileName = "LizardTail", menuName = "DreamKeep/Data/Relic/蜥蜴尾巴")]
    public sealed class SO_LizardTail : SO_Relic
    {
        [Range(0.05f, 1f)]
        [SerializeField] private float _healPercent = 0.5f;

        public float HealPercent => _healPercent;
    }
}
