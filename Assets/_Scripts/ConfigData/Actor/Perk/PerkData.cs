using System;
using UnityEngine;

namespace _DreamKeep.ConfigData
{
    /// <summary>战斗状态配置模板：资产里被 [SerializeReference] 引用的静态定义，只描述"这类状态是什么"；
    /// 运行时挂在生物身上的是 RuntimeData 侧由 PerkBaker 烘焙出的独立实例。</summary>
    [Serializable]
    public abstract class PerkData
    {
        [SerializeField] private Sprite _icon;

        public Sprite Icon => _icon;

        /// <summary>显示名。</summary>
        public abstract string Name { get; }

        /// <summary>衰减时机：宿主自己的回合边界递减一层，默认回合结束；永久状态覆写为 None。</summary>
        public abstract EPerkDecayTiming DecayTiming { get; }
    }
}
