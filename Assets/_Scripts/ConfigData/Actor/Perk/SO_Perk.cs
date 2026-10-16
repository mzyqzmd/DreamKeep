using UnityEngine;

namespace _DreamKeep.ConfigData
{
    /// <summary>战斗状态定义：一份资产描述"这类状态是什么"（图标、显示名、衰减时机），全局唯一真源，运行期只读；
    /// 挂在生物身上的是 RuntimeData 侧由 PerkBaker 烘焙出的独立实例，层数与数值修正都只改实例。</summary>
    public abstract class SO_Perk : ScriptableObject
    {
        [SerializeField] private Sprite _icon;

        public Sprite Icon => _icon;

        /// <summary>显示名。</summary>
        public abstract string Name { get; }

        /// <summary>衰减时机：宿主自己的回合边界递减一层，默认回合结束；永久状态覆写为 None。</summary>
        public abstract EPerkDecayTiming DecayTiming { get; }
    }
}
