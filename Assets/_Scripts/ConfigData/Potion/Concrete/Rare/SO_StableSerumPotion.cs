using UnityEngine;

namespace _DreamKeep.ConfigData
{
    /// <summary>稳定血清：给自己施加若干层稳定血清，接下来若干回合末保留手牌。</summary>
    [CreateAssetMenu(fileName = "StableSerumPotion", menuName = "DreamKeep/Data/Potion/稳定血清")]
    public sealed class SO_StableSerumPotion : SO_Potion
    {
        [SerializeField] private SO_StableSerum _stableSerum;
        [SerializeField] private int _layers = 2;

        public SO_StableSerum StableSerum => _stableSerum;
        public int Layers => _layers;
        public override EPotionTargetType TargetType => EPotionTargetType.Self;
    }
}
