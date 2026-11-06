using System.Collections.Generic;
using UnityEngine;

namespace _DreamKeep.ConfigData
{
    /// <summary>药水奖励池：药水奖励掷中后从这里抽一瓶，多池合并时按池权重分摊概率、池内等权。
    /// 可按稀有度分多份池资产，由遭遇奖励配置的池列表合并成一个候选集使用。</summary>
    [CreateAssetMenu(fileName = "PotionPool", menuName = "DreamKeep/Data/Pool/PotionPool")]
    public sealed class SO_PotionPool : ScriptableObject
    {
        [SerializeField] private List<SO_Potion> _potions = new();

        [Tooltip("本池档位权重：多池合并抽取时按此相对值分摊概率，0 视为本池不出现")] [Min(0f)] [SerializeField]
        private float _weight = 1f;

        public IReadOnlyList<SO_Potion> Potions => _potions;

        public float Weight => _weight;
    }
}
