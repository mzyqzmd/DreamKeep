using System.Collections.Generic;

namespace _DreamKeep.Gameplay
{
    /// <summary>消耗能量动作：从目标扣除能量，用于打出卡牌，能量是否足够由阶段侧先行校验。</summary>
    public class Action_SpendEnergy : GameAction
    {
        /// <summary>可被 Pre 钩子修改的消耗量。</summary>
        public int Amount { get; set; }

        public Action_SpendEnergy(Actor source_, List<Actor> targets_, int amount_) : base(source_, targets_) => Amount = amount_;

        protected override void OnExecute(BattleState battleState_, DomainEventBuffer events_)
        {
            foreach (Actor target in Targets)
            {
                target.Energy -= Amount;
                events_.Emit(new EnergyChangedEvent(target.Energy));
            }
        }
    }
}
