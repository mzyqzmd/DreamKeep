using UnityEngine;

namespace _DreamKeep.ConfigData
{
    /// <summary>弹珠袋：战斗开始时给全体敌人施加易伤层数。</summary>
    [CreateAssetMenu(fileName = "BagOfMarbles", menuName = "DreamKeep/Data/Relic/弹珠袋")]
    public sealed class SO_BagOfMarbles : SO_Relic
    {
        [SerializeField] private SO_Vulnerable _vulnerable;
        [SerializeField] private int _layers = 1;

        public SO_Vulnerable Vulnerable => _vulnerable;
        public int Layers => _layers;
    }
}
