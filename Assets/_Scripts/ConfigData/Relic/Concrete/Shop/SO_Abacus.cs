using UnityEngine;

namespace _DreamKeep.ConfigData
{
    /// <summary>算盘：每次弃牌堆洗回抽牌堆时获得格挡。</summary>
    [CreateAssetMenu(fileName = "Abacus", menuName = "DreamKeep/Data/Relic/算盘")]
    public sealed class SO_Abacus : SO_Relic
    {
        [SerializeField] private int _block = 6;

        public int Block => _block;
    }
}
