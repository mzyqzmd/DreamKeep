using System.Collections.Generic;
using _DreamKeep.RuntimeData;

namespace _DreamKeep.GameBattle
{
    /// <summary>敌人意图转动作工厂：把运行时意图展开成动作集合，复合意图递归展开两条子意图。
    /// 敌人的伤害打玩家、格挡给自己、状态按配置决定落点。</summary>
    public static class EnemyIntentResolver
    {
        #region 建队：按意图类型逐个转动作后整体返回

        public static List<BattleAction> Build(BattleContext battleContext_, Enemy enemy_, EnemyIntent intent_)
        {
            List<BattleAction> actions = new List<BattleAction>();
            Resolve(intent_, enemy_, battleContext_, actions);
            return actions;
        }

        #endregion

        private static void Resolve(EnemyIntent intent_, Enemy enemy_, BattleContext battleContext_,
            List<BattleAction> actions_)
        {
            switch (intent_)
            {
                case EI_Damage damage:
                {
                    List<Creature> players = new List<Creature> { battleContext_.Player.Creature };
                    for (int i = 0; i < damage.Times; i++)
                        actions_.Add(new BA_Damage(enemy_.Creature, players, damage.Amount));
                    break;
                }
                case EI_Block block:
                    actions_.Add(new BA_Block(enemy_.Creature, new List<Creature> { enemy_.Creature }, block.Amount));
                    break;
                case EI_ApplyPerk applyPerk:
                {
                    Creature target = applyPerk.IsSelf ? enemy_.Creature : battleContext_.Player.Creature;
                    actions_.Add(new BA_ApplyPerk(enemy_.Creature, new List<Creature> { target }, applyPerk.Perk));
                    break;
                }
                case EI_Mixed mixed:
                    if (mixed.First != null) Resolve(mixed.First, enemy_, battleContext_, actions_);
                    if (mixed.Second != null) Resolve(mixed.Second, enemy_, battleContext_, actions_);
                    break;
            }
        }
    }
}