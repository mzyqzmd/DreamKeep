using System.Collections.Generic;
using _DreamKeep.RuntimeData;

namespace _DreamKeep.GameBattle
{
    /// <summary>消费状态动作：把宿主身上指定的一次性状态摘除并广播移除事件，供出牌链尾按命中情况调用。</summary>
    public sealed class BA_ConsumePerk : BattleAction
    {
        private readonly BattlePerk _perk;

        public BA_ConsumePerk(Creature source_, List<Creature> targets_, BattlePerk perk_) : base(source_, targets_) =>
            _perk = perk_;

        public override List<IBattleEvent> Execute(BattleContext battleContext_)
        {
            if (_perk == null || _perk.Layers <= 0) return new List<IBattleEvent>();

            BattlePerk removed = Source.RemovePerk(_perk);
            return removed == null ? new List<IBattleEvent>() : new List<IBattleEvent> { new BE_PerkRemoved(Source, removed) };
        }
    }
}
