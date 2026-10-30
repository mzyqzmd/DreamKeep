using UnityEngine;

namespace _DreamKeep.ConfigData
{
    /// <summary>舵盘：指定回合的玩家回合开始时获得格挡。</summary>
    [CreateAssetMenu(fileName = "CaptainsWheel", menuName = "DreamKeep/Data/Relic/舵盘")]
    public sealed class SO_CaptainsWheel : SO_Relic
    {
        [SerializeField] private int _turn = 3;
        [SerializeField] private int _block = 14;

        public int Turn => _turn;
        public int Block => _block;
    }
}
