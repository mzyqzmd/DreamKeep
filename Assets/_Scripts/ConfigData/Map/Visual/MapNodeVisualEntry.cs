using System;
using UnityEngine;

namespace _DreamKeep.ConfigData
{
    /// <summary>节点视觉条目：一个类型对号键 + 图标 + 描边。常规条目与 Unknown 揭示变体共用本类型，
    /// 变体表平铺在 SO_MapNodeVisual 顶层（Unity 内联序列化不允许自引用列表，会撞 10 层深度上限）。</summary>
    [Serializable]
    public class MapNodeVisualEntry
    {
        [Tooltip("节点类型（变体条目填揭示出的类型）")] [SerializeField] private EMapPointType _type;
        [Tooltip("图标")] [SerializeField] private Sprite _icon;
        [Tooltip("描边（可走态点亮）")] [SerializeField] private Sprite _outline;

        public EMapPointType Type => _type;
        public Sprite Icon => _icon;
        public Sprite Outline => _outline;
    }
}
