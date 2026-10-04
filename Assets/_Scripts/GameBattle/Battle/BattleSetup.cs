using System;
using System.Collections.Generic;
using _DreamKeep.ConfigData;
using _DreamKeep.RuntimeData;
using _DreamKeep.Utility;

namespace _DreamKeep.GameBattle
{
    /// <summary>战斗装配器：把玩家与敌人的配置一次装配成一场战斗的初始状态，是"配置 → 战斗"的唯一入口。
    /// 内部完成局内卡组的烘焙、战斗卡的逐张克隆与映射建立、洗牌、战斗态建立与敌人意图烘焙。</summary>
    public static class BattleSetup
    {
        #region 装配一场战斗：产出玩家、敌人与战斗态

        public static BattleContext Create(SO_Player playerConfig_, IReadOnlyList<SO_Enemy> enemyConfigs_, Rng rng_)
        {
            Player player = new(playerConfig_.MaxHp);

            // 配置 → 局内卡（canonical 原型克隆成可变副本，跨战斗保留）
            for (int i = 0; i < playerConfig_.StartDeck.Count; i++)
                player.Deck.Add(CardModelBaker.Bake(playerConfig_.StartDeck[i]).ToMutable());

            player.StartCombat(playerConfig_.StartEnergy);

            Dictionary<CardModel, CardModel> runCardOf = BuildBattleCards(player, rng_);
            List<Enemy> enemies = BuildEnemies(enemyConfigs_);

            return new BattleContext(player, enemies, runCardOf, rng_);
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

        /// <summary>敌人按配置顺序装配：从最靠后槽位向前排（1 只站 3 号、2 只站 2/3 号），并烘焙最前槽位替换意图。</summary>
        private static List<Enemy> BuildEnemies(IReadOnlyList<SO_Enemy> enemyConfigs_)
        {
            if (enemyConfigs_.Count > BattleContext.MaxEnemyCount)
                throw new InvalidOperationException(
                    $"敌人数量 {enemyConfigs_.Count} 超过纵深阵型上限 {BattleContext.MaxEnemyCount}");

            List<Enemy> enemies = new();
            int rearOffset = 4 - enemyConfigs_.Count;
            for (int i = 0; i < enemyConfigs_.Count; i++)
            {
                SO_Enemy config = enemyConfigs_[i];
                enemies.Add(new Enemy(config.Name, config.MaxHp, i + rearOffset, IntentBaker.Bake(config.EnemyIntents),
                    IntentBaker.Bake(config.FrontSlotIntent)));
            }

            return enemies;
        }
    }
}