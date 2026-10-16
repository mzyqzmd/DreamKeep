using System;
using UnityEngine;

namespace _DreamKeep.ConfigData
{
    /// <summary>升级补丁·追加词条：升级后为这张牌追加上一条词条（词条内容照配置，可带自己的参数）。</summary>
    [Serializable]
    [AddTypeMenu("Upgrade/追加词条")]
    public sealed class UD_AddAffix : UpgradeData
    {
        [SerializeReference,SubclassSelector] private AffixData _affix;

        public AffixData Affix => _affix;
    }
}
