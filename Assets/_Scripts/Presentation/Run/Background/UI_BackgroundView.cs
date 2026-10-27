using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using _DreamKeep.ConfigData;

namespace _DreamKeep.Presentation
{
    /// <summary>战斗背景视图：按配置逐层替换背景图，层数由场景接线决定——
    /// 配置层数少于接线层数时隐藏多余层，多于接线层数时报警并忽略多出的层。
    /// 只写 sprite 与 Image.enabled，不动 GameObject 与层级，不脏画布布局。</summary>
    public class UI_BackgroundView : MonoBehaviour
    {
        [Tooltip("顺序即绘制顺序：第 0 层最远")]
        [SerializeField] private List<Image> _layers = new();

        #region 应用背景：逐层写入 sprite，未接线与多出的层隐藏

        public void Apply(SO_Background background_)
        {
            IReadOnlyList<Sprite> sprites = background_.Layers;
            if (sprites.Count > _layers.Count)
                Debug.LogWarning($"[{nameof(UI_BackgroundView)}] 背景 {background_.name} 有 {sprites.Count} 层，" +
                                 $"超过场景接线的 {_layers.Count} 层，多出的层已忽略（可在 BackgroundRoot 下补节点）");

            for (int i = 0; i < _layers.Count; i++)
            {
                Image layer = _layers[i];
                if (!layer) continue;
                Sprite sprite = i < sprites.Count ? sprites[i] : null;
                layer.sprite = sprite;
                layer.enabled = sprite != null;
            }
        }

        #endregion
    }
}
