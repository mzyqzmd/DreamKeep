using System;
using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using UnityEngine;
using _DreamKeep.RuntimeData;

namespace _DreamKeep.CommonPresentation
{
    /// <summary>卡牌移除弹板：陈列局内卡组全部卡，左键选中即开 card_ripple 涟漪、再点取消；选满请求张数时确认按钮缩放弹出，
    /// 选满后再点未选卡按 FIFO 替换最早选中的；右键打开统一卡牌详情特写。确认即逐张从卡组移除并关板，
    /// 被删卡以删牌 VFX 在弹层画布上横向均布同时演出，播完交还被删卡列表；关闭键为取消语义交还 null。
    /// 选择流程在 UI_CardSelectPanel，这里只回答「候选是整副牌库」与「确认后删牌 + 播删牌演出」。
    /// 获取方式并存：休息段经 PopPanelFactory 懒建，商店/事件/先古段直接用场景实例注册。</summary>
    public sealed class UI_CardRemovePanel : UI_CardSelectPanel
    {
        protected override IReadOnlyList<CardModel> CollectCandidates() => Player.Deck.Cards;

        // 确认移除：先取走选中卡再写域（同帧手快再敲确认时选中表已空），随后由基类关板、演出并交付
        protected override void OnConfirm()
        {
            List<CardModel> removed = TakeSelectedCards();
            for (int i = 0; i < removed.Count; i++) Player.Deck.Remove(removed[i]);
            FinishAsync(removed).Forget();
        }

        protected override void PlayVfx(Transform parent_, Vector3 worldPosition_, float scale_, CardModel card_,
            Action onFinished_)
        {
            if (Audio) Audio.Play(Audio.Cues.RemoveCard);
            VfxFactory.PlayRemoveCard(parent_, worldPosition_, scale_, card_, onFinished_);
        }
    }
}
