using UnityEngine;

namespace _DreamKeep.ConfigData
{
    /// <summary>带骨肉：战斗结束时生命比例不高于阈值则回复生命。</summary>
    [CreateAssetMenu(fileName = "MeatOnTheBone", menuName = "DreamKeep/Data/Relic/带骨肉")]
    public sealed class SO_MeatOnTheBone : SO_Relic
    {
        [Range(0f, 1f)]
        [SerializeField] private float _hpThreshold = 0.3f;
        [SerializeField] private int _heal = 12;

        public float HpThreshold => _hpThreshold;
        public int Heal => _heal;
    }
}
