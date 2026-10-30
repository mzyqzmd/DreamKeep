using UnityEngine;

namespace _DreamKeep.ConfigData
{
    /// <summary>手钻：打破敌人格挡时给该敌人施加易伤。</summary>
    [CreateAssetMenu(fileName = "HandDrill", menuName = "DreamKeep/Data/Relic/手钻")]
    public sealed class SO_HandDrill : SO_Relic
    {
        [SerializeField] private SO_Vulnerable _vulnerable;
        [SerializeField] private int _layers = 2;

        public SO_Vulnerable Vulnerable => _vulnerable;
        public int Layers => _layers;
    }
}
