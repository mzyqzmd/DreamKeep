using UnityEngine;

namespace _DreamKeep.ConfigData
{
    /// <summary>格挡药水：给自己获得固定格挡（照常吃敏捷修正）。</summary>
    [CreateAssetMenu(fileName = "BlockPotion", menuName = "DreamKeep/Data/Potion/格挡药水")]
    public sealed class SO_BlockPotion : SO_Potion
    {
        [SerializeField] private int _amount = 12;

        public int Amount => _amount;
        public override EPotionTargetType TargetType => EPotionTargetType.Self;
    }
}
