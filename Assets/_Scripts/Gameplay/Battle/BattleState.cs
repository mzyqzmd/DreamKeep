using _DreamKeep.RuntimeData;
using _DreamKeep.Utility;
using System.Collections.Generic;

namespace _DreamKeep.Gameplay
{
    /// <summary>战斗态：唯一可变的战斗数据集合，变更一律由动作经动作系统写入，本类只提供数据与查询，不认识表现层。</summary>
    public class BattleState
    {
        public Player Player { get; }
        public List<Enemy> Enemies { get; }
        public CardPile DrawPile { get; }
        public CardPile Hand { get; }
        public CardPile DiscardPile { get; }
        public CardPile ExhaustPile { get; }
        public Rng Rng { get; }
        public int TurnCount { get; set; }
        public int EnergyPerTurn { get; }

        public bool IsDefeat => Player.IsDead;
        public bool IsVictory => Enemies.TrueForAll(enemy_ => enemy_.IsDead);
        public bool IsBattleOver => IsDefeat || IsVictory;

        public BattleState(Player player_, List<Enemy> enemies_, List<InBattleCard> deck_, Rng rng_, int energyPerTurn_)
        {
            Player = player_;
            Enemies = enemies_;
            DrawPile = new CardPile();
            Hand = new CardPile();
            DiscardPile = new CardPile();
            ExhaustPile = new CardPile();
            Rng = rng_;
            EnergyPerTurn = energyPerTurn_;

            foreach (InBattleCard card in deck_)
                DrawPile.Add(card);
            DrawPile.Shuffle(Rng);
        }

        #region 取全部存活敌人，按槽位从左到右
        public List<Enemy> GetAliveEnemies()
        {
            List<Enemy> alive = new();
            foreach (Enemy enemy in Enemies)
                if (!enemy.IsDead)
                    alive.Add(enemy);
            alive.Sort((left_, right_) => left_.Slot.CompareTo(right_.Slot));
            return alive;
        }
        #endregion
    }
}
