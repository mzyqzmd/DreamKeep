using UnityEngine;

namespace _DreamKeep.ConfigData
{
    /// <summary>灯笼：指定回合的玩家回合开始时获得能量。</summary>
    [CreateAssetMenu(fileName = "Lantern", menuName = "DreamKeep/Data/Relic/灯笼")]
    public sealed class SO_Lantern : SO_Relic
    {
        [SerializeField] private int _turn = 1;
        [SerializeField] private int _energy = 1;

        public int Turn => _turn;
        public int Energy => _energy;
    }
}
