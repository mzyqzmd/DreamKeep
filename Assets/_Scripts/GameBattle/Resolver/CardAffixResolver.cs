using System;
using System.Collections.Generic;
using _DreamKeep.RuntimeData;

namespace _DreamKeep.GameBattle
{
    /// <summary>词条转动作工厂：把玩家打出卡的效果型词条按管线顺序解析成动作集合，由调用方决定如何入队。</summary>
    public static class CardAffixResolver
    {
        #region 建队：先解析落点，再按词条类型逐个转动作后整体返回

        public static List<BattleAction> Build(BattleContext battleContext_, CardModel card_, Player player_,
            int aimSlot_)
        {
            List<Creature> targets = ResolveTarget(battleContext_, card_, player_, aimSlot_);
            List<BattleAction> actions = new List<BattleAction>();
            foreach (CardAffix affix in card_.ActiveAffixes)
                ResolveAffix(battleContext_, affix, player_.Creature, targets, card_, actions);
            return actions;
        }

        #endregion

        #region 预览落点：表现层提示专用，与结算共用同一份映射

        /// <summary>预览这张牌会命中的目标：与结算共用同一解析，故"提示的是谁就一定打到谁"；
        /// 随机目标预告不出具体是哪一只，这里点亮全部存活敌人（且不消耗随机源）。</summary>
        public static List<Creature> PreviewTargets(BattleContext battleContext_, CardModel card_, Player player_,
            int aimSlot_) =>
            card_.IsRandomEnemyTarget
                ? ToCreatures(battleContext_.GetAliveEnemies())
                : ResolveTarget(battleContext_, card_, player_, aimSlot_);

        #endregion

        /// <summary>落点解析：自标卡先判自身，避免空场时打不出防御牌；其余落点针对存活敌人。</summary>
        private static List<Creature> ResolveTarget(BattleContext battleContext_, CardModel card_, Player player_,
            int aimSlot_)
        {
            if (card_.IsNoneTarget) return new List<Creature>();
            if (card_.IsSelfTarget) return new List<Creature> { player_.Creature };

            List<Enemy> alive = battleContext_.GetAliveEnemies();
            if (alive.Count == 0) return new List<Creature>();
            if (card_.IsAllEnemyTarget) return ToCreatures(alive);
            if (card_.IsRandomEnemyTarget)
                return new List<Creature> { alive[battleContext_.Rng.Next(alive.Count)].Creature };
            if (card_.IsFrontEnemyTarget) return new List<Creature> { alive[0].Creature };
            // 瞄准下标来自表现层命令，可能为 -1（未瞄准）或目标刚死而越界：按空落点处理，与药水解析同口径
            if (card_.IsAnyEnemyTarget)
                return aimSlot_ >= 0 && aimSlot_ < alive.Count
                    ? new List<Creature> { alive[aimSlot_].Creature }
                    : new List<Creature>();
            return new List<Creature>();
        }

        private static List<Creature> ToCreatures(List<Enemy> enemies_)
        {
            List<Creature> creatures = new List<Creature>();
            for (int i = 0; i < enemies_.Count; i++)
                creatures.Add(enemies_[i].Creature);
            return creatures;
        }

        // 按词条类型转动作：伤害与状态作用于敌方，其余作用于自身；重复次数由词条自身决定
        private static void ResolveAffix(BattleContext battleContext_, CardAffix affix_, Creature source_,
            List<Creature> targets_, CardModel card_, List<BattleAction> actions_)
        {
            switch (affix_)
            {
                case CA_Damage damage:
                    for (int i = 0; i < damage.Times.Value; i++)
                        actions_.Add(card_.IsRandomEnemyTarget
                            ? new BA_DamageRandomEnemy(source_, damage.Amount.Value)
                            : new BA_Damage(source_, targets_, damage.Amount.Value));
                    break;
                case CA_Block block:
                    actions_.Add(new BA_Block(source_, targets_, block.Amount.Value));
                    break;
                case CA_ApplyPerk applyPerk:
                    actions_.Add(new BA_ApplyPerk(source_, targets_, applyPerk.Perk));
                    break;
                case CA_DrawCard drawCard:
                    for (int i = 0; i < drawCard.Count.Value; i++)
                        actions_.Add(new BA_DrawCard(source_, targets_));
                    break;
                case CA_GainEnergy gainEnergy:
                    actions_.Add(new BA_GainEnergy(source_, targets_, gainEnergy.Amount.Value));
                    break;
                case CA_LostHealth lostHealth:
                    actions_.Add(new BA_LoseHealth(source_, targets_, lostHealth.Amount.Value));
                    break;
                case CA_MoveEnemy moveEnemy:
                    foreach (Enemy enemy in battleContext_.Enemies)
                        if (!enemy.Creature.IsDead && targets_.Contains(enemy.Creature))
                            actions_.Add(new BA_MoveEnemy(enemy, moveEnemy.Offset.Value));
                    break;
                case CA_Grant grant:
                    // 每张被授予的卡各自持有一份克隆，避免多张卡共享同一词条实例
                    actions_.Add(new BA_Grant(source_, targets_, grant.Count.Value, grant.GrantedAffix));
                    break;
                case CA_Erase _:
                    actions_.Add(new BA_Erase(source_, targets_, card_));
                    break;
                case CA_Exhaust _:
                    actions_.Add(new BA_Exhaust(source_, targets_, card_));
                    break;
                // 被动词条不产生动作：由出牌与回合流程自行读取它们
                case CA_Retain _:
                case CA_UnableToPlay _:
                    break;
                default:
                    throw new ArgumentOutOfRangeException(nameof(affix_),
                        $"{affix_.GetType().Name} 未登记：漏登记会让该词条静默失效");
            }
        }
    }
}