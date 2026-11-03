using System;
using UnityEngine;

namespace _DreamKeep.ConfigData
{
    /// <summary>自选移除卡牌：弹卡牌移除面板选满指定张数（面板自管删除与演出）；关闭键取消 → 会话回滚整个选项。</summary>
    [Serializable]
    [AddTypeMenu("Event/自选移除卡牌")]
    public sealed class EED_RemoveCards : EventEffectData
    {
        [SerializeField] private int _count; // 要移除的张数（选项条件 DeckAtLeast 兜住牌组下限）

        public int Count => _count;
    }
}
