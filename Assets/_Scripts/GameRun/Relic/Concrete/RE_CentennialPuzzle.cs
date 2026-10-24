using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using _DreamKeep.ConfigData;
using _DreamKeep.GameBattle;
using _DreamKeep.RuntimeData;

namespace _DreamKeep.GameRun
{
    /// <summary>百年积木：每场战斗中第一次损失生命值时，抽 N 张牌。
    /// 伤害事件的 Amount 是修正后总伤害、Absorbed 是格挡吸收，两者之差大于零才是真的失血。</summary>
    public sealed class RE_CentennialPuzzle : RelicEffect
    {
        private readonly SO_CentennialPuzzle _data;
        private bool _lostHp; // 每场一次

        public RE_CentennialPuzzle(SO_CentennialPuzzle data_, BattleContext context_, ActionSystem actions_, BattleEventDispatcher dispatcher_,
            RelicTriggerNotifier notifier_)
            : base(data_, context_, actions_, dispatcher_, notifier_)
        {
            _data = data_;
            Dispatcher.Register<BE_DamageDealt>(OnDamageDealt);
        }

        private async UniTask OnDamageDealt(BE_DamageDealt ev_)
        {
            if (_lostHp) return;
            if (ev_.Target != Context.Player.Creature) return;
            if (ev_.Amount - ev_.Absorbed <= 0) return;
            _lostHp = true;
            List<Creature> self = new() { Context.Player.Creature };
            BattleAction[] draws = new BattleAction[_data.DrawCounts];
            for (int i = 0; i < draws.Length; i++)
                draws[i] = new BA_DrawCard(null, self);
            await PerformAsync(draws);
        }
    }
}
