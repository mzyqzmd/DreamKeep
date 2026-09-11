using System.Collections.Generic;

namespace _DreamKeep.Gameplay
{
    /// <summary>获得能量动作：为玩家累加能量，能量为玩家专属故本动作恒作用于玩家。</summary>
    public class BA_GainEnergy : BattleAction
    {
        private readonly Player _player;

        /// <summary>可被 Pre 钩子修改的获得量。</summary>
        public int Amount { get; set; }

        public BA_GainEnergy(Player player_, int amount_) : base(player_, new List<Actor> { player_ })
        {
            _player = player_;
            Amount = amount_;
        }

        protected override void OnExecute(BattleState battleState_, BattleEventBuffer events_)
        {
            _player.Energy += Amount;
            events_.Emit(new IBE_EnergyChanged(_player.Energy));
        }
    }
}
