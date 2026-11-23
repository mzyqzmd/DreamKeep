using UnityEngine;

namespace _DreamKeep.ConfigData
{
    /// <summary>幸运补剂：给自己施加若干层缓冲。</summary>
    [CreateAssetMenu(fileName = "LuckyTonic", menuName = "DreamKeep/Data/Potion/幸运补剂")]
    public sealed class SO_LuckyTonic : SO_Potion
    {
        [SerializeField] private SO_Buffer _buffer;
        [SerializeField] private int _layers = 1;

        public SO_Buffer Buffer => _buffer;
        public int Layers => _layers;
        public override EPotionTargetType TargetType => EPotionTargetType.Self;
    }
}
