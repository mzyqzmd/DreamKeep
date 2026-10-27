using System.Collections.Generic;
using UnityEngine;

namespace _DreamKeep.ConfigData
{
    /// <summary>背景配置：一组按绘制顺序排列的层图（从远到近）。层数与分层由数据决定，
    /// 同一套美术资源可以自由组合成多份背景。</summary>
    [CreateAssetMenu(fileName = "Background", menuName = "DreamKeep/Data/Run/Background")]
    public sealed class SO_Background : ScriptableObject
    {
        [Tooltip("顺序即绘制顺序：第 0 张最远")]
        [SerializeField] private List<Sprite> _layers = new();

        public IReadOnlyList<Sprite> Layers => _layers;
    }
}
