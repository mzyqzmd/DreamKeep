using UnityEngine;

namespace _DreamKeep.ConfigData
{
    /// <summary>棋子：每打出一张能力牌抽牌。</summary>
    [CreateAssetMenu(fileName = "ChessPiece", menuName = "DreamKeep/Data/Relic/棋子")]
    public sealed class SO_ChessPiece : SO_Relic
    {
        [SerializeField] private int _cards = 1;

        public int Cards => _cards;
    }
}
