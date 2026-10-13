namespace _DreamKeep.Utility
{
    /// <summary>九宫格对齐点（左上角、上中、右上角 …… 右下角九种）：既用于浮层以自身该点贴住所给的世界锚点，
    /// 也用于取某个矩形上该点的世界坐标（见 UiGeometry.Pivot / AnchorPoint）；供说明气泡等需要按点对齐的浮层复用。</summary>
    public enum EUiAnchor
    {
        TopLeft,
        TopCenter,
        TopRight,
        MiddleLeft,
        MiddleCenter,
        MiddleRight,
        BottomLeft,
        BottomCenter,
        BottomRight,
    }
}
