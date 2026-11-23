using UnityEngine;

namespace _DreamKeep.ConfigData
{
    /// <summary>精致折扇：每回合每打出若干张攻击牌，获得格挡。</summary>
    [CreateAssetMenu(fileName = "OrnamentalFan", menuName = "DreamKeep/Data/Relic/精致折扇")]
    public sealed class SO_OrnamentalFan : SO_Relic
    {
        [SerializeField] private int _count = 3;
        [SerializeField] private int _block = 4;

        public int Count => _count;
        public int Block => _block;
    }
}
