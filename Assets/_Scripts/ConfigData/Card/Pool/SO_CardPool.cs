using System.Collections.Generic;
using UnityEngine;

namespace _DreamKeep.ConfigData
{
    /// <summary>卡牌奖励池：一份奖励从这里等权抽候选卡。
    /// 可按稀有度分多份池资产，由遭遇奖励配置的池列表合并成一个候选集使用。</summary>
    [CreateAssetMenu(fileName = "CardPool", menuName = "DreamKeep/Data/Pool/CardPool")]
    public sealed class SO_CardPool : ScriptableObject
    {
        [SerializeField] private List<SO_Card> _cards = new();

        public IReadOnlyList<SO_Card> Cards => _cards;
    }
}
