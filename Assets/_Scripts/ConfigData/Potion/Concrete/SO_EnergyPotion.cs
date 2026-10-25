using UnityEngine;

namespace _DreamKeep.ConfigData
{
    /// <summary>能量药水：立刻获得若干点能量。</summary>
    [CreateAssetMenu(fileName = "EnergyPotion", menuName = "DreamKeep/Data/Potion/能量药水")]
    public sealed class SO_EnergyPotion : SO_Potion
    {
        [SerializeField] private int _amount = 2;

        public int Amount => _amount;
        public override EPotionTargetType TargetType => EPotionTargetType.Self;
    }
}
