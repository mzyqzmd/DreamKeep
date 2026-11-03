using System;
using System.Collections.Generic;
using UnityEngine;

namespace _DreamKeep.ConfigData
{
    /// <summary>事件的单条选项：按钮名、副标题（机制数值）、置灰条件、悬停展示卡与第 2 页结果文案；
    /// 选中后按 _effects 顺序执行。取消回滚只覆盖生命与金币——会回弹面板的效果（删牌/升级）必须排在序列末尾，
    /// 其前不得有已落地不回滚的效果（入卡/遗物/药水），否则取消会把它们留下。</summary>
    [Serializable]
    public class EventOptionData
    {
        [SerializeField] private string _name; // 选项名
        [TextArea] [SerializeField] private string _subtitle; // 副标题：机制数值（如「金币 −80 ｜ 药水 ×2」）
        [SerializeField] private EEventCondition _condition; // 置灰条件：不满足时选项可见不可点
        [SerializeField] private int _conditionValue; // 条件阈值（与效果数值分开配置，保持直白）
        [SerializeField] private SO_Card _displayCard; // 悬停展示卡（可空：无卡牌相关则不展示）
        [TextArea(3, 10)] [SerializeField] private string _resultText; // 第 2 页按本选项显示的结果文案
        [SerializeReference, SubclassSelector] private List<EventEffectData> _effects = new();

        public string Name => _name;
        public string Subtitle => _subtitle;
        public EEventCondition Condition => _condition;
        public int ConditionValue => _conditionValue;
        public SO_Card DisplayCard => _displayCard;
        public string ResultText => _resultText;
        public IReadOnlyList<EventEffectData> Effects => _effects;
    }
}