using UnityEngine;

namespace _DreamKeep.ConfigData
{
    /// <summary>迅捷药水：抽若干张牌（逐张结算，手牌上限逐张判定）。</summary>
    [CreateAssetMenu(fileName = "SwiftPotion", menuName = "DreamKeep/Data/Potion/迅捷药水")]
    public sealed class SO_SwiftPotion : SO_Potion
    {
        [SerializeField] private int _count = 3;

        public int Count => _count;
        public override EPotionTargetType TargetType => EPotionTargetType.Self;
    }
}
