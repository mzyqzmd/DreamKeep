using UnityEngine;

namespace _DreamKeep.ConfigData
{
    /// <summary>不休陀螺：玩家回合内手牌打空时抽牌。</summary>
    [CreateAssetMenu(fileName = "UnceasingTop", menuName = "DreamKeep/Data/Relic/不休陀螺")]
    public sealed class SO_UnceasingTop : SO_Relic
    {
        [SerializeField] private int _cards = 1;

        public int Cards => _cards;
    }
}
