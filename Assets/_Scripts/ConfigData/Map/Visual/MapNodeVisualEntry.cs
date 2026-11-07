using System;
using UnityEngine;

namespace _DreamKeep.ConfigData
{
    /// <summary>节点视觉条目：一个类型对号键 + 图标 + 描边 + 悬停说明文案。常规条目与 Unknown 揭示变体共用本类型
    /// （说明文案恒读常规条目，变体条目文本留空），变体表平铺在 SO_MapNodeVisual 顶层
    /// （Unity 内联序列化不允许自引用列表，会撞 10 层深度上限）。</summary>
    [Serializable]
    public class MapNodeVisualEntry
    {
        [Tooltip("节点类型（变体条目填揭示出的类型）")] [SerializeField] private EMapPointType _type;
        [Tooltip("图标")] [SerializeField] private Sprite _icon;
        [Tooltip("描边（可走态点亮）")] [SerializeField] private Sprite _outline;
        [Tooltip("悬停说明标题")] [SerializeField] private string _name;
        [Tooltip("悬停说明描述（直写 TMP 富文本标签）")] [SerializeField, TextArea(2, 6)] private string _description;

        public EMapPointType Type => _type;
        public Sprite Icon => _icon;
        public Sprite Outline => _outline;
        public string Name => _name;
        public string Description => _description;
    }
}
