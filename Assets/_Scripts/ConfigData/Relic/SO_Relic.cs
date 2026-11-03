using UnityEngine;

namespace _DreamKeep.ConfigData
{
    /// <summary>遗物配置基类：一份资产描述"这件遗物是什么"（名称、图标、稀有度、描述），全局唯一真源，运行期只读；
    /// 每件遗物一个子类，效果数值在子类上逐件配置，由 GameRun 侧的 RelicBaker 按资产类型分派出对应效果。
    /// 描述为静态富文本、与数值同住配置资产：遗物效果不随局面变化，故不按 BP_*/EI_*/CA_* 的常例放到运行时类。
    /// 「拾起时」一次性结算的遗物在 RelicBaker 里登记为 null（无战斗内效果），结算写在领取它的段里。</summary>
    public abstract class SO_Relic : ScriptableObject
    {
        [SerializeField] private string _relicName;
        [SerializeField] private Sprite _icon;
        [SerializeField] private ERelicRarity _rarity;
        [TextArea(2, 6)]
        [SerializeField] private string _description;

        [Tooltip("拾起代价：领取这件遗物时失去的生命值，0 表示无代价；三选一按它置灰，扣血后保底留 1 点")]
        [SerializeField]
        private int _pickupHpCost;

        public string RelicName => _relicName;
        public Sprite Icon => _icon;
        public ERelicRarity Rarity => _rarity;

        /// <summary>遗物栏悬停气泡的正文，TMP 富文本。</summary>
        public string Description => _description;

        /// <summary>拾起时失去的生命值；0 表示无代价。置灰判定与扣血执行共用这一处配置。</summary>
        public int PickupHpCost => _pickupHpCost;
    }
}