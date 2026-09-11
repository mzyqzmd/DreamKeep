using System.Collections.Generic;

namespace _DreamKeep.Gameplay
{
    /// <summary>消耗能量动作：从玩家扣除能量，用于打出卡牌，能量是否足够由阶段侧先行校验。</summary>
    public class BA_SpendEnergy : BattleAction
    {
        private readonly Player _player;

        /// <summary>可被 Pre 钩子修改的消耗量。</summary>
        public int Amount { get; set; }

        public BA_SpendEnergy(Player player_, int amount_) : base(player_, new List<Actor> { player_ })
        {
            _player = player_;
            Amount = amount_;
        }

        protected override void OnExecute(BattleState battleState_, BattleEventBuffer events_)
        {
            _player.Energy -= Amount;
            events_.Emit(new IBE_EnergyChanged(_player.Energy));
        }
    }
}
