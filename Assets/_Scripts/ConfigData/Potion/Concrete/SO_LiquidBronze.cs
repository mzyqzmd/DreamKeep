using UnityEngine;

namespace _DreamKeep.ConfigData
{
    /// <summary>流动铜液：给自己施加若干层荆棘。</summary>
    [CreateAssetMenu(fileName = "LiquidBronze", menuName = "DreamKeep/Data/Potion/流动铜液")]
    public sealed class SO_LiquidBronze : SO_Potion
    {
        [SerializeField] private SO_Thorns _thorns;
        [SerializeField] private int _layers = 3;

        public SO_Thorns Thorns => _thorns;
        public int Layers => _layers;
        public override EPotionTargetType TargetType => EPotionTargetType.Self;
    }
}
