using UnityEngine;

namespace _DreamKeep.ConfigData
{
    /// <summary>敏捷药水：给自己施加敏捷层数。</summary>
    [CreateAssetMenu(fileName = "DexterityPotion", menuName = "DreamKeep/Data/Potion/敏捷药水")]
    public sealed class SO_DexterityPotion : SO_Potion
    {
        [SerializeField] private SO_Dexterity _dexterity;
        [SerializeField] private int _layers = 2;

        public SO_Dexterity Dexterity => _dexterity;
        public int Layers => _layers;
        public override EPotionTargetType TargetType => EPotionTargetType.Self;
    }
}
