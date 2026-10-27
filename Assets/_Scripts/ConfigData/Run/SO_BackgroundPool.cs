using System;
using System.Collections.Generic;
using UnityEngine;

namespace _DreamKeep.ConfigData
{
    /// <summary>背景池：按遭遇种类给出候选背景，Boss 战与其余战斗各一支，抽取交给调用方。</summary>
    [CreateAssetMenu(fileName = "BackgroundPool", menuName = "DreamKeep/Data/Run/Background Pool")]
    public sealed class SO_BackgroundPool : ScriptableObject
    {
        [SerializeField] private List<SO_Background> _normal = new();
        [SerializeField] private List<SO_Background> _boss = new();

        #region 取候选背景：Boss 战取 Boss 支，其余取普通支

        public IReadOnlyList<SO_Background> Select(EEncounterKind kind_) => kind_ switch
        {
            EEncounterKind.Normal => _normal,
            EEncounterKind.Elite => _normal,
            EEncounterKind.Boss => _boss,
            _ => throw new ArgumentOutOfRangeException(nameof(kind_), kind_, "未登记的遭遇种类"),
        };

        #endregion
    }
}
