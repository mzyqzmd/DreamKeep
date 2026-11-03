using System.Collections.Generic;
using UnityEngine;

namespace _DreamKeep.ConfigData
{
    /// <summary>事件配置资产：一段跑图事件的全部内容——事件名、背景、第 1 页整段叙事与选项表。
    /// 选项携带置灰条件与 [SerializeReference] 效果清单，两页结构由"叙事（第 1 页）/每选项结果文案（第 2 页）"两个字段表达。
    /// 本类只存数据：条件求值与效果执行统一由表现层 EventSession 分派（配置层不认识运行时）。</summary>
    public abstract class SO_Event : ScriptableObject
    {
        [SerializeField] private string _eventName; // 事件名 → 标题文本
        [SerializeField] private SO_Background _background; // 事件背景整图
        [TextArea(3, 10)]
        [SerializeField] private string _narrative; // 第 1 页整段叙事
        [SerializeField] private List<EventOptionData> _options = new();

        public string EventName => _eventName;
        public SO_Background Background => _background;
        public string Narrative => _narrative;
        public IReadOnlyList<EventOptionData> Options => _options;
    }
}
