using System.Collections.Generic;
using _DreamKeep.RuntimeData;

namespace _DreamKeep.GameBattle
{
    /// <summary>消耗能量动作：从玩家扣除当前能量，用于打出卡牌，能量是否足够由阶段侧先行校验。</summary>
    public sealed class BA_SpendEnergy : BattleAction
    {
        /// <summary>消耗量。</summary>
        public int Amount { get; }

        public BA_SpendEnergy(Creature source_, List<Creature> targets_, int amount_) : base(source_, targets_) => Amount = amount_;

        public override List<IBattleEvent> Execute(BattleContext battleContext_)
        {
            List<IBattleEvent> events = new();
            PlayerCombatState state = battleContext_.Player.CombatState;
            state.CurrentEnergy -= Amount;
            events.Add(new BE_EnergyChanged(state.CurrentEnergy, state.MaxEnergy));
            return events;
        }
    }
}
