using UnityEngine;

namespace _DreamKeep.ConfigData
{
    /// <summary>吊灯：指定回合的玩家回合开始时获得能量。</summary>
    [CreateAssetMenu(fileName = "Chandelier", menuName = "DreamKeep/Data/Relic/吊灯")]
    public sealed class SO_Chandelier : SO_Relic
    {
        [SerializeField] private int _turn = 3;
        [SerializeField] private int _energy = 1;

        public int Turn => _turn;
        public int Energy => _energy;
    }
}
