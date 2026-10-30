using UnityEngine;

namespace _DreamKeep.ConfigData
{
    /// <summary>异鱼之油：给自己同时施加力量与敏捷。</summary>
    [CreateAssetMenu(fileName = "FyshOil", menuName = "DreamKeep/Data/Potion/异鱼之油")]
    public sealed class SO_FyshOil : SO_Potion
    {
        [SerializeField] private SO_Strength _strength;
        [SerializeField] private int _strengthLayers = 1;
        [SerializeField] private SO_Dexterity _dexterity;
        [SerializeField] private int _dexterityLayers = 1;

        public SO_Strength Strength => _strength;
        public int StrengthLayers => _strengthLayers;
        public SO_Dexterity Dexterity => _dexterity;
        public int DexterityLayers => _dexterityLayers;
        public override EPotionTargetType TargetType => EPotionTargetType.Self;
    }
}
