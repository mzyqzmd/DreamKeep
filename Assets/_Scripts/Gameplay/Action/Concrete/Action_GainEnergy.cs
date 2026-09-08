using System.Collections.Generic;

namespace _DreamKeep.Gameplay
{
    /// <summary>获得能量动作：为目标累加能量，敌人无能量消耗故恒不使用。</summary>
    public class Action_GainEnergy : GameAction
    {
        public int Amount { get; set; }

        public Action_GainEnergy(Actor source_, List<Actor> targets_, int amount_) : base(source_, targets_) => Amount = amount_;

        protected override void OnExecute(BattleState battleState_, DomainEventBuffer events_)
        {
            foreach (Actor target in Targets)
            {
                target.Energy += Amount;
                events_.Emit(new EnergyChangedEvent(target.Energy));
            }
        }
    }
}
