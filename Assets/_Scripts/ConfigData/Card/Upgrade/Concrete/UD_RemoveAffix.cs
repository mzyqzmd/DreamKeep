using System;
using UnityEngine;

namespace _DreamKeep.ConfigData
{
    /// <summary>升级补丁·移除词条：按词条种类移除这张牌上的一条词条（烘焙期按配置词条的种类对号，同种类有多条时命中第一条）。</summary>
    [Serializable]
    [AddTypeMenu("Upgrade/移除词条")]
    public sealed class UD_RemoveAffix : UpgradeData
    {
        [SerializeReference, SubclassSelector] private AffixData _affix; // 按种类匹配要移除的词条

        public AffixData Affix => _affix;
    }
}
