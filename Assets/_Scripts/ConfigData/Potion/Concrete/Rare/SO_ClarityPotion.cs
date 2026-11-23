using UnityEngine;

namespace _DreamKeep.ConfigData
{
    /// <summary>明晰提取物：抽若干张牌，并给自己施加若干层明晰。</summary>
    [CreateAssetMenu(fileName = "ClarityPotion", menuName = "DreamKeep/Data/Potion/明晰提取物")]
    public sealed class SO_ClarityPotion : SO_Potion
    {
        [SerializeField] private int _cards = 1;
        [SerializeField] private SO_ClarityPerk _clarity;
        [SerializeField] private int _layers = 3;

        public int Cards => _cards;
        public SO_ClarityPerk Clarity => _clarity;
        public int Layers => _layers;
        public override EPotionTargetType TargetType => EPotionTargetType.Self;
    }
}
