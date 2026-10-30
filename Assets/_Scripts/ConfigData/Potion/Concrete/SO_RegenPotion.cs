using UnityEngine;

namespace _DreamKeep.ConfigData
{
    /// <summary>再生药水：给自己施加再生层数。</summary>
    [CreateAssetMenu(fileName = "RegenPotion", menuName = "DreamKeep/Data/Potion/再生药水")]
    public sealed class SO_RegenPotion : SO_Potion
    {
        [SerializeField] private SO_Regen _regen;
        [SerializeField] private int _layers = 5;

        public SO_Regen Regen => _regen;
        public int Layers => _layers;
        public override EPotionTargetType TargetType => EPotionTargetType.Self;
    }
}
