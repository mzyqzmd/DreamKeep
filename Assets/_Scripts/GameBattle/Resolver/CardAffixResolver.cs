using System;
using System.Collections.Generic;
using _DreamKeep.RuntimeData;

namespace _DreamKeep.GameBattle
{
    /// <summary>词条转动作工厂：把玩家打出卡的效果型词条按管线顺序解析成动作集合，由调用方决定如何入队。</summary>
    public static class CardAffixResolver
    {
        #region 建队：先解析落点，再按词条类型逐个转动作后整体返回

        /// <summary>建队：forceRandomEnemy_ 为真时把单体牌的落点改成随机（地狱狂徒自动打出专用），
        /// 忽略"前方敌人"取值与瞄准下标；手动出牌一律传 false。</summary>
        public static List<BattleAction> Build(BattleContext battleContext_, CardModel card_, Player player_,
            int aimSlot_, bool forceRandomEnemy_)
        {
            List<Creature> targets = ResolveTarget(battleContext_, card_, player_, aimSlot_, forceRandomEnemy_);
            List<BattleAction> actions = new List<BattleAction>();
            foreach (CardAffix affix in card_.ActiveAffixes)
                ResolveAffix(battleContext_, affix, player_.Creature, targets, card_, actions);
            return actions;
        }

        #endregion

        #region 预览落点：表现层提示专用，与结算共用同一份映射

        /// <summary>预览落点：填充调用方缓冲（先清后填），存活敌人由调用方按槽位排好传入，语义与返回新列表的版本一致。</summary>
        public static void PreviewTargets(CardModel card_, Player player_, int aimSlot_, List<Enemy> aliveEnemies_,
            List<Creature> results_)
        {
            results_.Clear();
            if (card_.IsNoneTarget) return;
            if (card_.IsSelfTarget)
            {
                results_.Add(player_.Creature);
                return;
            }

            if (aliveEnemies_.Count == 0) return;
            // 随机目标预告不出具体是哪一只，点亮全部存活敌人（不消耗随机源）
            if (card_.IsRandomEnemyTarget || card_.IsAllEnemyTarget)
            {
                for (int i = 0; i < aliveEnemies_.Count; i++) results_.Add(aliveEnemies_[i].Creature);
                return;
            }

            if (card_.IsFrontEnemyTarget)
            {
                results_.Add(aliveEnemies_[0].Creature);
                return;
            }

            if (card_.IsAnyEnemyTarget && aimSlot_ >= 0 && aimSlot_ < aliveEnemies_.Count)
                results_.Add(aliveEnemies_[aimSlot_].Creature);
        }

        #endregion

        /// <summary>落点解析：自标卡先判自身，避免空场时打不出防御牌；其余落点针对存活敌人。
        /// 强制随机只改单体牌（前方敌人 / 任意敌人）的落点，随机源与"本来就是随机敌人"的牌共用一次掷骰口径。</summary>
        private static List<Creature> ResolveTarget(BattleContext battleContext_, CardModel card_, Player player_,
            int aimSlot_, bool forceRandomEnemy_)
        {
            if (card_.IsNoneTarget) return new List<Creature>();
            if (card_.IsSelfTarget) return new List<Creature> { player_.Creature };

            List<Creature> alive = battleContext_.GetAliveCreatures();
            if (alive.Count == 0) return new List<Creature>();
            if (card_.IsAllEnemyTarget) return alive;
            if (card_.IsRandomEnemyTarget) return RandomOne(battleContext_, alive);
            // 自动打出的单体牌按状态描述落到随机敌人：前方敌人与瞄准下标都不再生效
            if (forceRandomEnemy_ && (card_.IsFrontEnemyTarget || card_.IsAnyEnemyTarget))
                return RandomOne(battleContext_, alive);
            if (card_.IsFrontEnemyTarget) return new List<Creature> { alive[0] };
            // 瞄准下标来自表现层命令，可能为 -1（未瞄准）或目标刚死而越界：按空落点处理，与药水解析同口径
            if (card_.IsAnyEnemyTarget)
                return aimSlot_ >= 0 && aimSlot_ < alive.Count
                    ? new List<Creature> { alive[aimSlot_] }
                    : new List<Creature>();
            return new List<Creature>();
        }

        // 随机单体落点：随机敌人牌与强制随机共用同一处掷骰，保证两路消耗随机流的次数一致
        private static List<Creature> RandomOne(BattleContext battleContext_, List<Creature> alive_) =>
            new List<Creature> { alive_[battleContext_.Rng.Next(alive_.Count)] };

        // 自身落点：资源型词条（格挡/失血/抽牌/能量）恒定作用于出牌者，与卡牌是否以敌人为目标无关
        private static List<Creature> SelfTargets(Creature source_) => new List<Creature> { source_ };

        // 按词条类型转动作：伤害/状态用卡牌落点，资源型词条恒为出牌者；重复次数由词条自身决定
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
                    // 按牌加成经 CardBonusHook 统一求值：与预览门面共用同一份规则，杜绝结算/预览漂移
                    actions_.Add(new BA_Block(source_, SelfTargets(source_),
                        block.Amount.Value + CardBonusHook.GetBonus(source_, card_, EStatChannel.Block)));
                    break;
                case CA_ApplyPerk applyPerk:
                    actions_.Add(new BA_ApplyPerk(source_, targets_, applyPerk.Perk));
                    break;
                case CA_DrawCard drawCard:
                    for (int i = 0; i < drawCard.Count.Value; i++)
                        actions_.Add(new BA_DrawCard(source_, SelfTargets(source_)));
                    break;
                case CA_GainEnergy gainEnergy:
                    actions_.Add(new BA_GainEnergy(source_, SelfTargets(source_), gainEnergy.Amount.Value));
                    break;
                case CA_Heal heal:
                    actions_.Add(new BA_Heal(source_, targets_, heal.Amount.Value));
                    break;
                case CA_LostHealth lostHealth:
                    actions_.Add(new BA_LoseHealth(source_, SelfTargets(source_), lostHealth.Amount.Value));
                    break;
                case CA_Erase _:
                    actions_.Add(new BA_Erase(source_, targets_, card_));
                    break;
                case CA_Exhaust _:
                    actions_.Add(new BA_Exhaust(source_, targets_, card_));
                    break;
                case CA_SelectUpgrade selectUpgrade:
                    actions_.Add(new BA_SelectUpgrade(source_, targets_, selectUpgrade));
                    break;
                case CA_SelectExhaust selectExhaust:
                    actions_.Add(new BA_SelectExhaust(source_, targets_, selectExhaust));
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