using UnityEngine;

namespace _DreamKeep.ConfigData
{
    /// <summary>古茶具套装：在休息处休息后，下一场战斗首回合额外获得能量；
    /// 待触发标记存 Player.RelicCounters，战斗内时机由 RE_VenerableTeaSet 消费。</summary>
    [CreateAssetMenu(fileName = "VenerableTeaSet", menuName = "DreamKeep/Data/Relic/古茶具套装")]
    public sealed class SO_VenerableTeaSet : SO_Relic
    {
        [Min(1)] [SerializeField] private int _energy = 2;

        /// <summary>下一场战斗首回合额外获得的能量。</summary>
        public int Energy => _energy;
    }
}
