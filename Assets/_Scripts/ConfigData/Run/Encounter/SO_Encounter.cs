using System.Collections.Generic;
using UnityEngine;

namespace _DreamKeep.ConfigData
{
    /// <summary>遭遇配置：一场战斗的怪物组合。敌人列表顺序即站位（从前到后），
    /// 血量浮动在装配时按域随机流掷一次，让同一遭遇重复出现时血量略有差异。</summary>
    [CreateAssetMenu(fileName = "Encounter", menuName = "DreamKeep/Data/Run/Encounter/Encounter")]
    public sealed class SO_Encounter : ScriptableObject
    {
        [Header("基础配置")]
        [SerializeField] private string _name;

        [Tooltip("顺序即站位：第 0 个站最靠前")]
        [SerializeField] private List<SO_Enemy> _enemies = new();

        [Tooltip("血量浮动百分比，0 为固定；0.1 表示 ±10%")]
        [SerializeField] private float _hpVariance;

        [Header("奖励")]
        [Tooltip("战斗结束后的奖励配置；Boss 等终局遭遇留空即不结算")]
        [SerializeField] private SO_EncounterReward _reward;

        public string Name => _name;
        public IReadOnlyList<SO_Enemy> Enemies => _enemies;
        public float HpVariance => _hpVariance;
        public SO_EncounterReward Reward => _reward;
    }
}
