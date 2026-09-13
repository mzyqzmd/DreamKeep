using System.Collections.Generic;
using _DreamKeep.RuntimeData;

namespace _DreamKeep.Gameplay
{
    /// <summary>消耗能量动作：从玩家扣除当前能量，用于打出卡牌，能量是否足够由阶段侧先行校验。</summary>
    public class BA_SpendEnergy : BattleAction
    {
        /// <summary>可被 Pre 钩子修改的消耗量。</summary>
        public int Amount { get; set; }

        public BA_SpendEnergy(Creature source_, List<Creature> targets_, int amount_) : base(source_, targets_) => Amount = amount_;

        protected override List<IBattleEvent> OnExecute(BattleContext battleContext_)
        {
            List<IBattleEvent> events = new();
            PlayerCombatState state = battleContext_.Player.CombatState;
            state.CurrentEnergy -= Amount;
            events.Add(new BE_EnergyChanged(state.CurrentEnergy, state.MaxEnergy));
            return events;
        }
    }
}
