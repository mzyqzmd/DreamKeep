using UnityEngine;

namespace _DreamKeep.ConfigData
{
    /// <summary>遗物配置基类：一份资产描述"这件遗物是什么"（名称、图标、稀有度、描述），全局唯一真源，运行期只读；
    /// 每件遗物一个子类，效果数值在子类上逐件配置，由 GameRun 侧的 RelicBaker 按资产类型分派出对应效果。
    /// 描述为静态富文本、与数值同住配置资产：遗物效果不随局面变化，故不按 BP_*/EI_*/CA_* 的常例放到运行时类。</summary>
    public abstract class SO_Relic : ScriptableObject
    {
        [SerializeField] private string _relicName;
        [SerializeField] private Sprite _icon;
        [SerializeField] private ERelicRarity _rarity;
        [TextArea(2, 6)]
        [SerializeField] private string _description;

        public string RelicName => _relicName;
        public Sprite Icon => _icon;
        public ERelicRarity Rarity => _rarity;

        /// <summary>遗物栏悬停气泡的正文，TMP 富文本。</summary>
        public string Description => _description;
    }
}