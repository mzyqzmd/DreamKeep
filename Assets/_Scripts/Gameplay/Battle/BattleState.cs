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
        public ModifiableStat EnergyPerTurn { get; }

        /// <summary>本场被「遗忘」永久移除的卡组原型 Id，供战斗结束时回写卡组。</summary>
        public IReadOnlyCollection<int> ErasedSourceIds => _erasedSourceIds;

        #region 战斗结果

        public bool IsDefeat => Player.IsDead;
        public bool IsVictory => Enemies.TrueForAll(enemy_ => enemy_.IsDead);
        public bool IsBattleOver => IsDefeat || IsVictory;

        #endregion

        private readonly Dictionary<CardModel, int> _sourceOf;
        private readonly HashSet<int> _erasedSourceIds = new();

        public BattleState(Player player_, List<Enemy> enemies_, List<CardModel> deck_,
            Dictionary<CardModel, int> sourceOf_, Rng rng_, int energyPerTurn_)
        {
            Player = player_;
            Enemies = enemies_;
            DrawPile = new CardPile();
            Hand = new CardPile();
            DiscardPile = new CardPile();
            ExhaustPile = new CardPile();
            Rng = rng_;
            EnergyPerTurn = new ModifiableStat(energyPerTurn_);
            _sourceOf = sourceOf_ ?? new();

            foreach (CardModel card in deck_)
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

        #region 查战斗副本对应的卡组原型 Id，查不到即为战斗内无源临时卡

        public bool TryGetSourceId(CardModel battleCard_, out int sourceId_) =>
            _sourceOf.TryGetValue(battleCard_, out sourceId_);

        #endregion

        internal void RegisterErased(int sourceId_) => _erasedSourceIds.Add(sourceId_);
    }
}