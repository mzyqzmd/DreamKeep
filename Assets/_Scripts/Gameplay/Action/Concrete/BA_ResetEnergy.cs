using System.Collections.Generic;

namespace _DreamKeep.Gameplay
{
    /// <summary>重置能量动作：把玩家能量设为指定值，用于回合开始回满，区别于只做累加的获得能量。</summary>
    public class BA_ResetEnergy : BattleAction
    {
        private readonly Player _player;

        /// <summary>可被 Pre 钩子修改的目标能量值。</summary>
        public int Amount { get; set; }

        public BA_ResetEnergy(Player player_, int amount_) : base(player_, new List<Actor> { player_ })
        {
            _player = player_;
            Amount = amount_;
        }

        protected override void OnExecute(BattleState battleState_, BattleEventBuffer events_)
        {
            _player.Energy = Amount;
            events_.Emit(new IBE_EnergyChanged(_player.Energy));
        }
    }
}
