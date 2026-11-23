using UnityEngine;

namespace _DreamKeep.ConfigData
{
    /// <summary>肌肉药水：给自己施加若干层肌肉影子状态，同步等量真实力量。</summary>
    [CreateAssetMenu(fileName = "FlexPotion", menuName = "DreamKeep/Data/Potion/肌肉药水")]
    public sealed class SO_FlexPotion : SO_Potion
    {
        [SerializeField] private SO_Muscle _muscle;
        [SerializeField] private int _layers = 5;

        public SO_Muscle Muscle => _muscle;
        public int Layers => _layers;
        public override EPotionTargetType TargetType => EPotionTargetType.Self;
    }
}
