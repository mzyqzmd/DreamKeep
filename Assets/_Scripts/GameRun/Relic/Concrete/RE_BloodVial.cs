using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using _DreamKeep.ConfigData;
using _DreamKeep.GameBattle;
using _DreamKeep.RuntimeData;

namespace _DreamKeep.GameRun
{
    /// <summary>小血瓶：战斗开始时回复生命。</summary>
    public sealed class RE_BloodVial : RelicEffect
    {
        private readonly SO_BloodVial _data;

        public RE_BloodVial(SO_BloodVial data_, BattleContext context_, ActionSystem actions_, BattleEventDispatcher dispatcher_,
            RelicTriggerNotifier notifier_)
            : base(data_, context_, actions_, dispatcher_, notifier_)
        {
            _data = data_;
            Dispatcher.Register<BE_BattleStarted>(OnBattleStarted);
        }

        private async UniTask OnBattleStarted(BE_BattleStarted ev_) =>
            await PerformAsync(new BA_Heal(null, new List<Creature> { Context.Player.Creature }, _data.Heal));
    }
}
