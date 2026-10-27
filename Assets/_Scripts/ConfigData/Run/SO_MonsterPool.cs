using System;
using System.Collections.Generic;
using UnityEngine;

namespace _DreamKeep.ConfigData
{
    /// <summary>怪物池：按遭遇种类与当前层数给出候选遭遇列表，抽取由调用方用整局随机流完成——
    /// 配置资产不认识随机源。弱强分界是池自己的知识（弱怪只在前 3 层出现），调用方只说"什么遭遇、第几层"。</summary>
    [CreateAssetMenu(fileName = "MonsterPool", menuName = "DreamKeep/Data/Run/Monster Pool")]
    public sealed class SO_MonsterPool : ScriptableObject
    {
        /// <summary>弱怪池的最后层数：该层及以前取弱怪池，之后取强怪池。</summary>
        public const int WeakFloorLimit = 3;

        [SerializeField] private List<SO_Encounter> _weak = new();
        [SerializeField] private List<SO_Encounter> _strong = new();
        [SerializeField] private List<SO_Encounter> _elite = new();
        [SerializeField] private List<SO_Encounter> _boss = new();

        #region 取候选遭遇：按种类与层数选出对应档位的列表

        public IReadOnlyList<SO_Encounter> Select(EEncounterKind kind_, int floor_) => kind_ switch
        {
            EEncounterKind.Normal => floor_ <= WeakFloorLimit ? _weak : _strong,
            EEncounterKind.Elite => _elite,
            EEncounterKind.Boss => _boss,
            _ => throw new ArgumentOutOfRangeException(nameof(kind_), kind_, "未登记的遭遇种类"),
        };

        #endregion
    }
}
