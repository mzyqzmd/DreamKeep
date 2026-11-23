using UnityEngine;

namespace _DreamKeep.ConfigData
{
    /// <summary>马萨雷斯的赠礼：给自己施加若干层仪式。</summary>
    [CreateAssetMenu(fileName = "MazalethsGift", menuName = "DreamKeep/Data/Potion/马萨雷斯的赠礼")]
    public sealed class SO_MazalethsGift : SO_Potion
    {
        [SerializeField] private SO_Ritual _ritual;
        [SerializeField] private int _layers = 1;

        public SO_Ritual Ritual => _ritual;
        public int Layers => _layers;
        public override EPotionTargetType TargetType => EPotionTargetType.Self;
    }
}
