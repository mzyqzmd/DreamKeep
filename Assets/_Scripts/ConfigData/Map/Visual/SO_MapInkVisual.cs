using UnityEngine;

namespace _DreamKeep.ConfigData
{
    /// <summary>地图笔迹视觉配置：连续折线的颜色与线宽、采样间距、橡皮半径，游戏中全部固定、只能在此调。</summary>
    [CreateAssetMenu(fileName = "MapInkVisual", menuName = "DreamKeep/Data/Map/笔迹视觉")]
    public class SO_MapInkVisual : ScriptableObject
    {
        [Tooltip("笔迹颜色")] [SerializeField]
        private Color _inkColor = new(0.15f, 0.1f, 0.1f, 0.85f);

        [Tooltip("线宽（像素）＝折线三角带宽度")] [SerializeField]
        private float _lineWidthPx = 16f;

        [Tooltip("采样间距（像素）：相邻采样点最小距离，越小越贴合轨迹")] [SerializeField]
        private float _sampleSpacingPx = 6f;

        [Tooltip("橡皮半径（像素）：半径内的采样点被删除，剩余点断成多条线")] [SerializeField]
        private float _eraseRadiusPx = 24f;

        public Color InkColor => _inkColor;
        public float LineWidthPx => _lineWidthPx;
        public float SampleSpacingPx => _sampleSpacingPx;
        public float EraseRadiusPx => _eraseRadiusPx;
    }
}
