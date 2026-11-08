using System.Collections.Generic;
using UnityEngine;

namespace _DreamKeep.ConfigData
{
    /// <summary>敌人台词目录：每个敌人配置一份条目，按阶段/意图序号/前槽替换归组；
    /// 意图数据本身不持文本，表现层按「执行了第几个意图」查这里。</summary>
    [CreateAssetMenu(fileName = "EnemySpeechLines", menuName = "DreamKeep/Data/Speech/EnemySpeechLines")]
    public sealed class SO_EnemySpeechLines : ScriptableObject
    {
        [Header("阶段一")] [Tooltip("下标 = 意图序号")] [SerializeField]
        private List<SpeechLineGroup> _intentGroups = new();

        [Tooltip("站最前槽位时的替换意图")] [SerializeField]
        private SpeechLineGroup _frontSlotGroup = new();

        [Header("阶段二（双阶段敌人）")] [Tooltip("下标 = 阶段二意图序号")] [SerializeField]
        private List<SpeechLineGroup> _phaseTwoIntentGroups = new();

        [Tooltip("阶段二站最前槽位时的替换意图")] [SerializeField]
        private SpeechLineGroup _phaseTwoFrontSlotGroup = new();

        /// <summary>按执行事实取分组：前槽替换优先，否则按阶段取对应意图序号表。</summary>
        public SpeechLineGroup PickGroup(int intentIndex_, bool isFrontSlot_, bool isPhaseTwo_)
        {
            if (isFrontSlot_) return isPhaseTwo_ ? _phaseTwoFrontSlotGroup : _frontSlotGroup;
            List<SpeechLineGroup> groups = isPhaseTwo_ ? _phaseTwoIntentGroups : _intentGroups;
            return intentIndex_ >= 0 && intentIndex_ < groups.Count ? groups[intentIndex_] : null;
        }
    }
}