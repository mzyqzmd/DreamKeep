using UnityEngine;

namespace _DreamKeep.ConfigData
{
    /// <summary>速度药水：给自己施加若干层速度影子状态，同步等量真实敏捷。</summary>
    [CreateAssetMenu(fileName = "SpeedPotion", menuName = "DreamKeep/Data/Potion/速度药水")]
    public sealed class SO_SpeedPotion : SO_Potion
    {
        [SerializeField] private SO_SpeedPerk _speed;
        [SerializeField] private int _layers = 5;

        public SO_SpeedPerk Speed => _speed;
        public int Layers => _layers;
        public override EPotionTargetType TargetType => EPotionTargetType.Self;
    }
}
