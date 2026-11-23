using UnityEngine;

namespace _DreamKeep.ConfigData
{
    /// <summary>力量药水：给自己施加若干层力量；层数随条目配，状态定义资产不携带层数。</summary>
    [CreateAssetMenu(fileName = "StrengthPotion", menuName = "DreamKeep/Data/Potion/力量药水")]
    public sealed class SO_StrengthPotion : SO_Potion
    {
        [SerializeField] private SO_Strength _perk;
        [SerializeField] private int _layers = 2;

        public SO_Strength Perk => _perk;
        public int Layers => _layers;
        public override EPotionTargetType TargetType => EPotionTargetType.Self;
    }
}
