using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using _DreamKeep.ConfigData;
using _DreamKeep.GameBattle;
using _DreamKeep.RuntimeData;

namespace _DreamKeep.GameRun
{
    /// <summary>燃烧之血：战斗结束时回复生命。
    /// 结束事件为直发型且播完才 EndCombat，处理器内排空能在战斗态清空之前把回血与演出完整落地。</summary>
    public sealed class RE_BurningBlood : RelicEffect
    {
        private readonly SO_BurningBlood _data;

        public RE_BurningBlood(SO_BurningBlood data_, BattleContext context_, ActionSystem actions_, BattleEventDispatcher dispatcher_,
            RelicTriggerNotifier notifier_)
            : base(data_, context_, actions_, dispatcher_, notifier_)
        {
            _data = data_;
            Dispatcher.Register<BE_BattleEnded>(OnBattleEnded);
        }

        private async UniTask OnBattleEnded(BE_BattleEnded ev_)
        {
            if (!ev_.IsVictory) return; // 战败不触发：只播报胜利后的回血
            await PerformAsync(new BA_Heal(null, new List<Creature> { Context.Player.Creature }, _data.Heal));
        }
    }
}
