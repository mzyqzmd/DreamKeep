using UnityEngine;

namespace _DreamKeep.Utility
{
    /// <summary>数值富文本：把修正后的数值渲染成描述文本里的片段——高于基础值标升色、低于标降色、相等保持原色；
    /// 颜色由调用方给出（卡面可预先按不可打灰调压暗）。</summary>
    public static class UiValueText
    {
        public static string Value(int shown_, int base_, Color boostColor_, Color reduceColor_)
        {
            if (shown_ > base_) return $"<color=#{ColorUtility.ToHtmlStringRGB(boostColor_)}>{shown_}</color>";
            if (shown_ < base_) return $"<color=#{ColorUtility.ToHtmlStringRGB(reduceColor_)}>{shown_}</color>";
            return shown_.ToString();
        }
    }
}
