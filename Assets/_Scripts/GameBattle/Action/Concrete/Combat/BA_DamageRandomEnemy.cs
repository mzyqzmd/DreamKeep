using System.Collections.Generic;
using _DreamKeep.RuntimeData;

namespace _DreamKeep.GameBattle
{
    /// <summary>随机伤害动作：每次结算时才从存活敌人中随机取一个目标，
    /// 故同一次打出的多段伤害可能落在不同敌人身上；伤害结算复用 BA_Damage。
    /// 遗物伤害传 applyModifiers_ = false（不吃力量与易伤），卡牌伤害保持默认。</summary>
    public sealed class BA_DamageRandomEnemy : BattleAction
    {
        public int BaseAmount { get; }

        private readonly bool _applyModifiers;

        public BA_DamageRandomEnemy(Creature source_, int amount_, bool applyModifiers_ = true)
            : base(source_, new List<Creature>())
        {
            BaseAmount = amount_;
            _applyModifiers = applyModifiers_;
        }

        public override List<IBattleEvent> Execute(BattleContext battleContext_)
        {
            Enemy enemy = battleContext_.Rng.Pick(battleContext_.GetAliveEnemies());
            return enemy == null
                ? new List<IBattleEvent>()
                : new BA_Damage(Source, new List<Creature> { enemy.Creature }, BaseAmount, _applyModifiers).Execute(battleContext_);
        }
    }
}