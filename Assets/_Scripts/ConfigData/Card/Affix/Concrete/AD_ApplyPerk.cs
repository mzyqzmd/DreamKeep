using System;
using UnityEngine;

namespace _DreamKeep.ConfigData
{
    /// <summary>施加效果词条：为指定目标施加一个状态（由 Perk 引用状态定义资产），层数由 Layers 决定。</summary>
    [Serializable]
    [AddTypeMenu("Affix/施加效果")]
    public sealed class AD_ApplyPerk : AffixData
    {
        [SerializeField] private SO_Perk _perk;
        [SerializeField] private int _layers;

        [Tooltip("卡面是否直接输出状态自身的描述：能力牌用，避免出现「对目标施加 N 层状态」这类错误文案")]
        [SerializeField]
        private bool _showPerkDescription;

        public SO_Perk Perk => _perk;
        public int Layers => _layers;
        public bool ShowPerkDescription => _showPerkDescription;
        public override string Name => "施加效果";
    }
}
