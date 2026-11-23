using UnityEngine;

namespace _DreamKeep.ConfigData
{
    /// <summary>红面具：战斗开始时给全体敌人施加虚弱层数。</summary>
    [CreateAssetMenu(fileName = "RedMask", menuName = "DreamKeep/Data/Relic/红面具")]
    public sealed class SO_RedMask : SO_Relic
    {
        [SerializeField] private SO_Weak _weak;
        [SerializeField] private int _layers = 2;

        public SO_Weak Weak => _weak;
        public int Layers => _layers;
    }
}
