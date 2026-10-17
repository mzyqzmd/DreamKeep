using System.Collections.Generic;
using _DreamKeep.RuntimeData;

namespace _DreamKeep.GameBattle
{
    /// <summary>随机伤害动作：每次结算时才从存活敌人中随机取一个目标，
    /// 故同一次打出的多段伤害可能落在不同敌人身上；伤害结算复用 BA_Damage。</summary>
    public sealed class BA_DamageRandomEnemy : BattleAction
    {
        public int BaseAmount { get; }

        public BA_DamageRandomEnemy(Creature source_, int amount_)
            : base(source_, new List<Creature>()) => BaseAmount = amount_;

        public override List<IBattleEvent> Execute(BattleContext battleContext_)
        {
            Enemy enemy = battleContext_.Rng.Pick(battleContext_.GetAliveEnemies());
            return enemy == null
                ? new List<IBattleEvent>()
                : new BA_Damage(Source, new List<Creature> { enemy.Creature }, BaseAmount).Execute(battleContext_);
        }
    }
}