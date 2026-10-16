using System;
using UnityEngine;

namespace _DreamKeep.ConfigData
{
    /// <summary>升级补丁·改状态层数：给指定状态的施加层数加一个增量。
    /// 单独成类而不并入"改数值"，是因为层数不住在 ModifiableStat 上，且需按状态种类定位到承载它的那条施加效果词条。</summary>
    [Serializable]
    [AddTypeMenu("Upgrade/改状态层数")]
    public sealed class UD_ModifyPerkLayers : UpgradeData
    {
        [SerializeField] private SO_Perk _perk; // 改哪一种状态的层数
        [SerializeField] private int _additive = 1; // 层数增量

        public SO_Perk Perk => _perk;
        public int Additive => _additive;
        
    }
}
