using UnityEngine;

namespace _DreamKeep.ConfigData
{
    /// <summary>明耀酊剂：获得能量并给自己施加若干层光辉（下 N 个回合始额外获得 1 能量）。</summary>
    [CreateAssetMenu(fileName = "RadiantTincture", menuName = "DreamKeep/Data/Potion/明耀酊剂")]
    public sealed class SO_RadiantTincture : SO_Potion
    {
        [SerializeField] private int _energy = 1;
        [SerializeField] private SO_Radiance _radiance;
        [SerializeField] private int _layers = 3;

        public int Energy => _energy;
        public SO_Radiance Radiance => _radiance;
        public int Layers => _layers;
        public override EPotionTargetType TargetType => EPotionTargetType.Self;
    }
}
