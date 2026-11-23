using UnityEngine;

namespace _DreamKeep.ConfigData
{
    /// <summary>百年积木：每场战斗中第一次损失生命值时，抽 N 张牌。</summary>
    [CreateAssetMenu(fileName = "CentennialPuzzle", menuName = "DreamKeep/Data/Relic/百年积木")]
    public sealed class SO_CentennialPuzzle : SO_Relic
    {
        [SerializeField] private int _drawCounts = 3;

        public int DrawCounts => _drawCounts;
    }
}
