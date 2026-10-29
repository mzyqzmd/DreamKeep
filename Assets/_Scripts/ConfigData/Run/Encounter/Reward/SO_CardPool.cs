using System.Collections.Generic;
using UnityEngine;

namespace _DreamKeep.ConfigData
{
    /// <summary>卡牌奖励池：一份奖励从这里等权抽候选卡。
    /// 不分稀有度档位——想调档就再建一份池资产、换奖励配置里的引用即可。</summary>
    [CreateAssetMenu(fileName = "CardPool", menuName = "DreamKeep/Data/Run/Encounter/CardPool")]
    public sealed class SO_CardPool : ScriptableObject
    {
        [SerializeField] private List<SO_Card> _cards = new();

        public IReadOnlyList<SO_Card> Cards => _cards;
    }
}
