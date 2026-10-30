using UnityEngine;

namespace _DreamKeep.ConfigData
{
    /// <summary>烛台：指定回合的玩家回合开始时获得能量。</summary>
    [CreateAssetMenu(fileName = "Candelabra", menuName = "DreamKeep/Data/Relic/烛台")]
    public sealed class SO_Candelabra : SO_Relic
    {
        [SerializeField] private int _turn = 2;
        [SerializeField] private int _energy = 1;

        public int Turn => _turn;
        public int Energy => _energy;
    }
}
