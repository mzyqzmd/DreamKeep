using System.Collections.Generic;

namespace _DreamKeep.Gameplay
{
    /// <summary>重置能量动作：把目标能量设为指定值，用于回合开始回满，区别于只做累加的获得能量。</summary>
    public class Action_ResetEnergy : GameAction
    {
        /// <summary>可被 Pre 钩子修改的目标能量值。</summary>
        public int Amount { get; set; }

        public Action_ResetEnergy(Actor source_, List<Actor> targets_, int amount_) : base(source_, targets_) => Amount = amount_;

        protected override void OnExecute(BattleState battleState_, DomainEventBuffer events_)
        {
            foreach (Actor target in Targets)
            {
                target.Energy = Amount;
                events_.Emit(new EnergyChangedEvent(target.Energy));
            }
        }
    }
}
