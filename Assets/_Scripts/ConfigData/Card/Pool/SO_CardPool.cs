using System.Collections.Generic;
using UnityEngine;

namespace _DreamKeep.ConfigData
{
    /// <summary>卡牌奖励池：一份奖励从这里抽候选卡，多池合并时按池权重分摊概率、池内等权。
    /// 可按稀有度分多份池资产，由遭遇奖励配置的池列表合并成一个候选集使用。</summary>
    [CreateAssetMenu(fileName = "CardPool", menuName = "DreamKeep/Data/Pool/CardPool")]
    public sealed class SO_CardPool : ScriptableObject
    {
        [SerializeField] private List<SO_Card> _cards = new();

        [Tooltip("本池档位权重：多池合并抽取时按此相对值分摊概率，0 视为本池不出现")] [Min(0f)] [SerializeField]
        private float _weight = 1f;

        public IReadOnlyList<SO_Card> Cards => _cards;

        public float Weight => _weight;
    }
}
