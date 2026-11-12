using System.Collections.Generic;
using _DreamKeep.RuntimeData;

namespace _DreamKeep.GameBattle
{
    /// <summary>高墙结算动作：执行期才按宿主阵营解析全体盟友（含宿主自己），复用格挡动作逐目标结算。</summary>
    public sealed class BA_RampartBlock : BattleAction
    {
        private readonly int _amount;

        public BA_RampartBlock(Creature source_, int amount_) : base(source_, new List<Creature>()) => _amount = amount_;

        public override List<IBattleEvent> Execute(BattleContext battleContext_)
        {
            List<Creature> targets = Source == battleContext_.Player.Creature
                ? new List<Creature> { battleContext_.Player.Creature }
                : battleContext_.GetAliveCreatures();

            return new BA_Block(null, targets, _amount).Execute(battleContext_);
        }
    }
}
