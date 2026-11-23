using UnityEngine;

namespace _DreamKeep.ConfigData
{
    /// <summary>金刚杵：战斗开始时给自己施加力量层数。</summary>
    [CreateAssetMenu(fileName = "Vajra", menuName = "DreamKeep/Data/Relic/金刚杵")]
    public sealed class SO_Vajra : SO_Relic
    {
        [SerializeField] private SO_Strength _strength;
        [SerializeField] private int _layers = 1;

        public SO_Strength Strength => _strength;
        public int Layers => _layers;
    }
}
