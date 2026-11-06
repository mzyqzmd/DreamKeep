using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using _DreamKeep.ConfigData;
using _DreamKeep.GameBattle;
using _DreamKeep.RuntimeData;

namespace _DreamKeep.GameRun
{
    /// <summary>奥利哈钢：回合结束时若没有格挡，获得格挡。
    /// 回合结束事件播出时格挡尚未被清（清除只发生在下回合开始），此刻读到的是本回合残留值；
    /// 获得的格挡在敌人回合生效、下回合开始随清格挡一并清除，与原作语义一致。</summary>
    public sealed class RE_Orichalcum : RelicEffect_T<SO_Orichalcum>
    {
        public RE_Orichalcum(SO_Orichalcum data_, BattleContext context_, ActionSystem actions_, BattleEventDispatcher dispatcher_,
            RelicTriggerNotifier notifier_)
            : base(data_, context_, actions_, dispatcher_, notifier_)
        {
            Dispatcher.Register<BE_PlayerTurnEnded>(OnPlayerTurnEnded);
        }

        private async UniTask OnPlayerTurnEnded(BE_PlayerTurnEnded ev_)
        {
            if (Context.Player.Creature.Block != 0) return;
            await PerformAsync(new BA_Block(null, PlayerTarget, Data.Block));
        }
    }
}
