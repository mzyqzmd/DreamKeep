using UnityEngine;

namespace _DreamKeep.ConfigData
{
    /// <summary>休息选项配置：名称、图标与说明是所有选项共有的静态配置。
    /// _isStaticDescription 为真时说明整句照抄资产文本，为假时说明由段入口按玩家状态与数值插值。</summary>
    public abstract class SO_RestOption : ScriptableObject
    {
        [Tooltip("选项名")] [SerializeField] private string _optionName;
        [Tooltip("选项图标")] [SerializeField] private Sprite _icon;

        [Tooltip("说明：静态选项整句直用，动态选项由段入口插值")] [SerializeField, TextArea(3, 10)]
        private string _description;

        [Tooltip("说明是否为静态文本：真＝照抄资产，假＝段入口按数值插值")] [SerializeField]
        private bool _isStaticDescription;

        public string OptionName => _optionName;

        public Sprite Icon => _icon;

        public string Description => _description;

        public bool IsStaticDescription => _isStaticDescription;
    }
}