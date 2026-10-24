using UnityEngine;

namespace _DreamKeep.ConfigData
{
    /// <summary>摆动球：每当 N 个回合抽一张牌。</summary>
    [CreateAssetMenu(fileName = "Pendulum", menuName = "DreamKeep/Data/Relic/摆动球")]
    public sealed class SO_Pendulum : SO_Relic
    {
        [SerializeField] private int _turnInterval = 3;
        [SerializeField] private int _drawCount = 1;

        public int TurnInterval => _turnInterval;
        public int DrawCount => _drawCount;
    }
}