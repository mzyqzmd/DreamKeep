using System;
using UnityEngine;

namespace _DreamKeep.CommonPresentation
{
    /// <summary>一条奖励的展示描述：图标、一段文案与"被点击时做什么"，是面板与条目之间的最小契约。
    /// 它由 UI_RewardPanel 按 RunReward 翻译出来——具体是什么奖励、点击后给多少，全写在 OnClaimed 里，
    /// 条目自身不认识卡牌 / 遗物 / 药水 / 金币，也不反向查询玩法数据。
    /// 文案是玩家的唯一信息来源：名称、数量、效果都写在这一段里（需要强调可自带富文本，面板不再另设标题位）。
    /// OnClaimed 收到的是条目视图本身：兑现后调 Complete() 让它从列表消失，放弃则调 Cancel() 解锁可再点。</summary>
    public readonly struct RewardEntry
    {
        public Sprite Icon { get; }

        public string Description { get; }

        public Action<UI_RewardItem> OnClaimed { get; }

        public RewardEntry(Sprite icon_, string description_, Action<UI_RewardItem> onClaimed_)
        {
            Icon = icon_;
            Description = description_;
            OnClaimed = onClaimed_;
        }
    }
}