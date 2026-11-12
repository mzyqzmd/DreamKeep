using System.Collections.Generic;
using _DreamKeep.RuntimeData;

namespace _DreamKeep.GameBattle
{
    /// <summary>获得能量动作：为玩家累加当前能量，能量为玩家专属故读写恒指向玩家战斗态。</summary>
    public sealed class BA_GainEnergy : BattleAction
    {
        /// <summary>获得量。</summary>
        public int Amount { get; }

        public BA_GainEnergy(Creature source_, List<Creature> targets_, int amount_) : base(source_, targets_) => Amount = amount_;

        public override List<IBattleEvent> Execute(BattleContext battleContext_)
        {
            List<IBattleEvent> events = new();
            PlayerCombatState state = battleContext_.Player.CombatState;
            state.CurrentEnergy += Amount;
            events.Add(new BE_EnergyChanged(state.CurrentEnergy, state.MaxEnergy));
            return events;
        }
    }
}
