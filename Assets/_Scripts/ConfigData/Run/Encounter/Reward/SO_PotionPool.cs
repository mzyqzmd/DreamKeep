using System.Collections.Generic;
using UnityEngine;

namespace _DreamKeep.ConfigData
{
    /// <summary>药水奖励池：药水奖励掷中后从这里等权抽一瓶。</summary>
    [CreateAssetMenu(fileName = "PotionPool", menuName = "DreamKeep/Data/Run/Encounter/PotionPool")]
    public sealed class SO_PotionPool : ScriptableObject
    {
        [SerializeField] private List<SO_Potion> _potions = new();

        public IReadOnlyList<SO_Potion> Potions => _potions;
    }
}
