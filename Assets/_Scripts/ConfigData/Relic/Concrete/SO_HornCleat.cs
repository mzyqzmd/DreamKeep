using UnityEngine;

namespace _DreamKeep.ConfigData
{
    /// <summary>船夹板：指定回合的玩家回合开始时获得格挡。</summary>
    [CreateAssetMenu(fileName = "HornCleat", menuName = "DreamKeep/Data/Relic/船夹板")]
    public sealed class SO_HornCleat : SO_Relic
    {
        [SerializeField] private int _turn = 2;
        [SerializeField] private int _block = 14;

        public int Turn => _turn;
        public int Block => _block;
    }
}
