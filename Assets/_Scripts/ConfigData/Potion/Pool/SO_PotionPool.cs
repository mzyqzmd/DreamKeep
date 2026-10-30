using System.Collections.Generic;
using UnityEngine;

namespace _DreamKeep.ConfigData
{
    /// <summary>药水奖励池：药水奖励掷中后从这里等权抽一瓶。
    /// 可按稀有度分多份池资产，由遭遇奖励配置的池列表合并成一个候选集使用。</summary>
    [CreateAssetMenu(fileName = "PotionPool", menuName = "DreamKeep/Data/Pool/PotionPool")]
    public sealed class SO_PotionPool : ScriptableObject
    {
        [SerializeField] private List<SO_Potion> _potions = new();

        public IReadOnlyList<SO_Potion> Potions => _potions;
    }
}
