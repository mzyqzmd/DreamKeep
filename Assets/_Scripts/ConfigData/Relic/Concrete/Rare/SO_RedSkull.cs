using UnityEngine;

namespace _DreamKeep.ConfigData
{
    /// <summary>红头骨：生命比例跌破阈值时获得力量（每场一次，刻意的一次性近似）。</summary>
    [CreateAssetMenu(fileName = "RedSkull", menuName = "DreamKeep/Data/Relic/红头骨")]
    public sealed class SO_RedSkull : SO_Relic
    {
        [Range(0f, 1f)]
        [SerializeField] private float _hpThreshold = 0.3f;
        [SerializeField] private SO_Strength _strength;
        [SerializeField] private int _layers = 3;

        public float HpThreshold => _hpThreshold;
        public SO_Strength Strength => _strength;
        public int Layers => _layers;
    }
}
