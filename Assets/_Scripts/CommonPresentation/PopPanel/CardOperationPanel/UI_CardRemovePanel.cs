using System.Collections.Generic;
using _DreamKeep.RuntimeData;

namespace _DreamKeep.CommonPresentation
{
    /// <summary>卡牌移除弹板：陈列局内卡组全部卡，左键选中即开 card_ripple 涟漪、再点取消；选满请求张数时确认按钮缩放弹出，
    /// 选满后再点未选卡按 FIFO 替换最早选中的；右键打开统一卡牌详情特写。确认只交回选中卡并关板，
    /// 真正从卡组移除与删牌演出都由域侧链路完成；关闭键为取消语义交还 null。
    /// 选择流程在 UI_CardSelectPanel，这里只回答「候选是整副牌库」与「确认后交回哪些卡」。
    /// 获取方式并存：休息段经 PopPanelFactory 懒建，商店/事件/先古段直接用场景实例注册。</summary>
    public sealed class UI_CardRemovePanel : UI_CardSelectPanel
    {
        protected override IReadOnlyList<CardModel> CollectCandidates() => Player.Deck.Cards;

        // 确认移除：只取走选中卡交回结果源，写域与演出由域侧链路完成
        protected override void OnConfirm() => FinishAsync(TakeSelectedCards());
    }
}
