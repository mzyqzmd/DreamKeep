using UnityEngine;

namespace _DreamKeep.ConfigData
{
    /// <summary>状态「超巨化」定义：宿主下一张攻击牌造成的伤害乘以倍率，触发后整条移除。</summary>
    [CreateAssetMenu(fileName = "Gigantification", menuName = "DreamKeep/Data/Perk/超巨化")]
    public sealed class SO_Gigantification : SO_Perk
    {
        [SerializeField, Min(1f)] private float _multiplier = 3f;

        public float Multiplier => _multiplier;

        public override string Name => "超巨化";
        public override EPerkSettleTiming SettleTiming => EPerkSettleTiming.None;
    }
}
