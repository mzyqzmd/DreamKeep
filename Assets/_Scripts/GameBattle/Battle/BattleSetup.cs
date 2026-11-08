using System;
using System.Collections.Generic;
using _DreamKeep.ConfigData;
using _DreamKeep.ConfigData.Boss;
using _DreamKeep.RuntimeData;
using _DreamKeep.Utility;

namespace _DreamKeep.GameBattle
{
    /// <summary>战斗装配器：把整局的玩家与一场遭遇装配成一场战斗的初始状态，是"配置 → 战斗"的唯一入口。
    /// 内部完成战斗卡的逐张克隆与映射建立、洗牌、战斗态建立与敌人意图烘焙；局内卡组由整局对象维护，这里不再烘焙。</summary>
    public static class BattleSetup
    {
        #region 装配一场战斗：产出战斗态

        public static BattleContext Create(Player player_, SO_Encounter encounter_, int startEnergy_, Rng rng_)
        {
            player_.StartCombat(startEnergy_);

            Dictionary<CardModel, CardModel> runCardOf = BuildBattleCards(player_, rng_);
            List<Enemy> enemies = BuildEnemies(encounter_, rng_);

            return new BattleContext(player_, enemies, runCardOf, rng_);
        }

        #endregion

        /// <summary>局内卡逐张克隆成战斗卡进抽牌堆，并登记"战斗卡 → 局内卡"映射；随后洗牌。</summary>
        private static Dictionary<CardModel, CardModel> BuildBattleCards(Player player_, Rng rng_)
        {
            Dictionary<CardModel, CardModel> runCardOf = new();
            CardPile deck = player_.Deck;
            CardPile drawPile = player_.CombatState.DrawPile;
            // 槽位索引连续，按下标遍历即可，不依赖字典遍历序
            for (int i = 0; i < deck.Count; i++)
            {
                CardModel runCard = deck.Cards[i];
                CardModel battleCard = runCard.ToMutable();
                runCardOf.Add(battleCard, runCard);
                drawPile.Add(battleCard);
            }

            drawPile.Shuffle(rng_);
            return runCardOf;
        }

        /// <summary>敌人按遭遇列表顺序装配：从最靠后槽位向前排（1 只站 3 号、2 只站 2/3 号），并烘焙最前槽位替换意图；
        /// 台词表在构造时一次写入，留空即全程沉默。</summary>
        private static List<Enemy> BuildEnemies(SO_Encounter encounter_, Rng rng_)
        {
            IReadOnlyList<SO_Enemy> configs = encounter_.Enemies;
            if (configs.Count > BattleContext.MaxEnemyCount)
                throw new InvalidOperationException(
                    $"遭遇 {encounter_.Name} 的敌人数量 {configs.Count} 超过纵深阵型上限 {BattleContext.MaxEnemyCount}");

            List<Enemy> enemies = new(configs.Count);
            int rearOffset = BattleSlots.Count - configs.Count;
            for (int i = 0; i < configs.Count; i++)
            {
                SO_Enemy config = configs[i];
                int maxHp = RollMaxHp(config.MaxHp, encounter_.HpVariance, rng_);
                Enemy enemy = new(config.Name, maxHp, i + rearOffset, IntentBaker.Bake(config.EnemyIntents),
                    IntentBaker.Bake(config.FrontSlotIntent), config.SpeechLines);
                LoadPhaseTwo(enemy, config);
                ApplyStartPerks(enemy, config);

                enemies.Add(enemy);
            }

            return enemies;
        }

        /// <summary>双阶段敌人装配期装载二阶段意图：普通敌人没有该配置直接跳过。</summary>
        private static void LoadPhaseTwo(Enemy enemy_, SO_Enemy config_)
        {
            if (config_ is not SO_CeremonialBeast beast) return;
            enemy_.LoadPhaseTwo(IntentBaker.Bake(beast.PhaseTwoIntents),
                IntentBaker.Bake(beast.PhaseTwoFrontSlotIntent));
        }

        /// <summary>按配置施加初始状态：克隆模板配层数后走标准施加链。</summary>
        private static void ApplyStartPerks(Enemy enemy_, SO_Enemy config_)
        {
            for (int i = 0; i < config_.StartPerks.Count; i++)
            {
                PerkEntry entry = config_.StartPerks[i];
                if (entry?.Perk == null) continue;
                BattlePerk perk = PerkBaker.Bake(entry.Perk);
                if (perk == null) continue;
                perk.AddLayers(entry.Layers);
                enemy_.Creature.ApplyPerk(perk);
            }
        }

        /// <summary>按浮动百分比抖动血量上限：0 完全固定，0.1 表示 ±10%；浮动不为 0 时每次装配消耗一次随机流。</summary>
        private static int RollMaxHp(int baseMaxHp_, float variance_, Rng rng_)
        {
            if (variance_ <= 0f) return baseMaxHp_;
            float factor = 1f + rng_.Next(-variance_, variance_);
            return Math.Max(1, (int)Math.Round(baseMaxHp_ * factor));
        }
    }
}
