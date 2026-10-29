using UnityEngine;

namespace _DreamKeep.ConfigData
{
    /// <summary>药水配置基类：一份资产描述"这瓶药水是什么"（名称、图标、稀有度、描述、落点），全局唯一真源，运行期只读；
    /// 效果数值配在具体子类上，由 GameBattle 侧的 PotionResolver 按资产类型分派出既有动作。
    /// 描述是静态富文本、与数值同住资产：药水数值不随局面变化，与 SO_Relic 同款取舍。</summary>
    public abstract class SO_Potion : ScriptableObject
    {
        [SerializeField] private string _potionName;
        [SerializeField] private Sprite _icon;
        [SerializeField] private EPotionRarity _rarity;

        [Tooltip("落地特效的液体色：着色先去色再乘此色")] [SerializeField]
        private Color _liquidColor = Color.white;

        [TextArea(2, 6)]
        [SerializeField] private string _description;

        public string PotionName => _potionName;
        public Sprite Icon => _icon;
        public EPotionRarity Rarity => _rarity;

        /// <summary>落地水花与地面的液体色。</summary>
        public Color LiquidColor => _liquidColor;

        /// <summary>悬停气泡的正文，TMP 富文本。</summary>
        public string Description => _description;

        /// <summary>落点：决定使用流程要不要选目标，也决定效果作用于谁。</summary>
        public abstract EPotionTargetType TargetType { get; }

        /// <summary>能否在战斗外饮用：只有效果作用于自身、且不依赖战斗态的药水（如治疗）才有意义，
        /// 其余药水（伤害、状态层、抽牌、能量）一律只能在战斗内使用。丢弃不受此限，任何时刻都能丢。</summary>
        public virtual bool UsableOutsideBattle => false;
    }
}
