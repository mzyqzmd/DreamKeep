using UnityEngine;

namespace _DreamKeep.ConfigData
{
    /// <summary>铁心药水：给自己施加若干层覆甲。</summary>
    [CreateAssetMenu(fileName = "HeartOfIron", menuName = "DreamKeep/Data/Potion/铁心药水")]
    public sealed class SO_HeartOfIron : SO_Potion
    {
        [SerializeField] private SO_Plating _plating;
        [SerializeField] private int _layers = 7;

        public SO_Plating Plating => _plating;
        public int Layers => _layers;
        public override EPotionTargetType TargetType => EPotionTargetType.Self;
    }
}
