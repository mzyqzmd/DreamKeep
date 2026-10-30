using UnityEngine;

namespace _DreamKeep.ConfigData
{
    /// <summary>招架盾：回合结束时若格挡达到阈值，对随机敌人造成伤害。</summary>
    [CreateAssetMenu(fileName = "ParryShield", menuName = "DreamKeep/Data/Relic/招架盾")]
    public sealed class SO_ParryShield : SO_Relic
    {
        [SerializeField] private int _blockThreshold = 5;
        [SerializeField] private int _damage = 5;

        public int BlockThreshold => _blockThreshold;
        public int Damage => _damage;
    }
}
