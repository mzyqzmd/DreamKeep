using Cysharp.Threading.Tasks;
using _DreamKeep.ConfigData;
using _DreamKeep.GameBattle;
using _DreamKeep.RuntimeData;

namespace _DreamKeep.GameRun
{
    /// <summary>招架盾：回合结束时格挡达到阈值，就对随机敌人造成伤害（遗物伤害不吃力量与易伤）。</summary>
    public sealed class RE_ParryShield : RelicEffect
    {
        private readonly SO_ParryShield _data;

        public RE_ParryShield(SO_ParryShield data_, BattleContext context_, ActionSystem actions_,
            BattleEventDispatcher dispatcher_, RelicTriggerNotifier notifier_)
            : base(data_, context_, actions_, dispatcher_, notifier_)
        {
            _data = data_;
            Dispatcher.Register<BE_PlayerTurnEnded>(OnPlayerTurnEnded);
        }

        private async UniTask OnPlayerTurnEnded(BE_PlayerTurnEnded ev_)
        {
            if (Context.Player.Creature.Block < _data.BlockThreshold) return;
            await PerformAsync(new BA_DamageRandomEnemy(null, _data.Damage, false));
        }
    }
}
