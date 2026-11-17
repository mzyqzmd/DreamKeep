using System.Collections.Generic;
using _DreamKeep.RuntimeData;

namespace _DreamKeep.GameBattle
{
    /// <summary>状态介入战斗的抽牌入口：牌进入手牌后询问一次宿主状态，地狱狂徒据此把「打击」牌立即对随机敌人打出；
    /// 自动打出不扣能量，动作链与正常出牌同序，嵌套动作直接执行并合事件。</summary>
    internal static class DrawHook
    {
        #region 抽牌入手后触发：地狱狂徒自动打出「打击」牌

        internal static void Check(BattleContext context_, Creature host_, CardModel drawn_, List<IBattleEvent> events_)
        {
            if (host_ == null || host_.IsDead || drawn_ == null) return;

            BP_Hellraiser hellraiser = BattlePerkQuery.Find<BP_Hellraiser>(host_);
            if (hellraiser == null || !BP_Hellraiser.ShouldAutoPlay(drawn_.Name)) return;

            List<Creature> self = new List<Creature> { host_ };
            PlayerCombatState state = context_.Player.CombatState;

            // 与正常出牌同序：入打出堆 → 词条结算 → 弃置 → 链尾广播；出牌计数照常累加，能量不扣
            events_.AddRange(new BA_ToPlayPile(host_, self, drawn_).Execute(context_));
            // 落点强制随机：随机源在解析器内统一掷，无存活敌人时自然落到空表
            foreach (BattleAction action in CardAffixResolver.Build(context_, drawn_, context_.Player, -1, true))
                events_.AddRange(action.Execute(context_));
            events_.AddRange(new BA_DiscardSingle(host_, self, drawn_, state.PlayPile).Execute(context_));
            events_.AddRange(new BA_TickCardPlayed(host_).Execute(context_));
        }

        #endregion
    }
}
