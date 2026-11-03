using System;
using UnityEngine;

namespace _DreamKeep.ConfigData
{
    /// <summary>自选升级卡牌：一次开锻造面板，升级张数取 min(本项张数, 牌库可升级卡数)；
    /// 一张可升的都没有时面板照样弹出，直接确认即可完成（选项永远可选，选了什么都不升）；
    /// 关闭键取消 → 会话回滚整个选项。</summary>
    [Serializable]
    [AddTypeMenu("Event/自选升级卡牌")]
    public sealed class EED_UpgradeCards : EventEffectData
    {
        [SerializeField] private int _count; // 最多升级的张数

        public int Count => _count;
    }
}
