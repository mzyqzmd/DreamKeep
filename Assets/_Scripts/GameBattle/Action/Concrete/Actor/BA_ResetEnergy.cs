using System.Collections.Generic;
using _DreamKeep.RuntimeData;

namespace _DreamKeep.GameBattle
{
    /// <summary>重置能量动作：把当前能量设为指定值，用于回合开始回满，区别于只做累加的获得能量。</summary>
    public class BA_ResetEnergy : BattleAction
    {
        /// <summary>可被 Pre 钩子修改的目标能量值。</summary>
        public int Amount { get; set; }

        public BA_ResetEnergy(Creature source_, List<Creature> targets_, int amount_) : base(source_, targets_) => Amount = amount_;

        protected override List<IBattleEvent> OnExecute(BattleContext battleContext_)
        {
            List<IBattleEvent> events = new();
            PlayerCombatState state = battleContext_.Player.CombatState;
            state.CurrentEnergy = Amount;
            events.Add(new BE_EnergyChanged(state.CurrentEnergy, state.MaxEnergy));
            return events;
        }
    }
}
