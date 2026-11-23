using UnityEngine;

namespace _DreamKeep.ConfigData
{
    /// <summary>营养牡蛎：拾起时一次性提高最大生命值（当前生命同步回复同量），无持续效果。</summary>
    [CreateAssetMenu(fileName = "NutritiousOyster", menuName = "DreamKeep/Data/Relic/营养牡蛎")]
    public sealed class SO_NutritiousOyster : SO_Relic
    {
        [SerializeField] private int _maxHpGain = 8;

        public int MaxHpGain => _maxHpGain;
    }
}
