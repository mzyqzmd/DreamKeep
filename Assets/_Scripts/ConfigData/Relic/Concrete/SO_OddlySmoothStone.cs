using UnityEngine;

namespace _DreamKeep.ConfigData
{
    /// <summary>意外光滑的石头：战斗开始时给自己施加敏捷层数。</summary>
    [CreateAssetMenu(fileName = "OddlySmoothStone", menuName = "DreamKeep/Data/Relic/意外光滑的石头")]
    public sealed class SO_OddlySmoothStone : SO_Relic
    {
        [SerializeField] private SO_Dexterity _dexterity;
        [SerializeField] private int _layers = 1;

        public SO_Dexterity Dexterity => _dexterity;
        public int Layers => _layers;
    }
}
