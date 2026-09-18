using System.Collections.Generic;
using _DreamKeep.RuntimeData;
using _DreamKeep.Utility;

namespace _DreamKeep.GameBattle
{
    /// <summary>战斗态：本场战斗的场级聚合，只持有玩家、敌人、随机源、回合数与"战斗卡→局内卡"映射；
    /// 玩家自己的五个牌堆与能量在 Player.CombatState 上（经 context.Player.CombatState 取用）。
    /// 变更一律由动作经动作系统写入，本类只提供数据与查询，不认识表现层。</summary>
    public class BattleContext
    {
        public Player Player { get; }
        public List<Enemy> Enemies { get; }
        public Rng Rng { get; }
        /// <summary>当前回合序号：从 1 起，我方与敌方回合共享同一个 n，敌人回合结束阶段才 ++。</summary>
        public int TurnCount { get; set; } = 1;

        /// <summary>本场被「遗忘」的局内卡，供战斗结束时从卡组永久移除。</summary>
        public IReadOnlyCollection<CardModel> ErasedRunCards => _erasedRunCards;

        private readonly Dictionary<CardModel, CardModel> _runCardOf;
        private readonly HashSet<CardModel> _erasedRunCards = new();

        #region 战斗结果

        public bool IsDefeat => Player.Creature.IsDead;
        public bool IsVictory => Enemies.TrueForAll(enemy_ => enemy_.Creature.IsDead);
        public bool IsBattleOver => IsDefeat || IsVictory;

        #endregion

        public BattleContext(Player player_, List<Enemy> enemies_, Dictionary<CardModel, CardModel> runCardOf_,
            Rng rng_)
        {
            Player = player_;
            Enemies = enemies_;
            Rng = rng_;
            _runCardOf = runCardOf_ ?? new Dictionary<CardModel, CardModel>();
        }

        #region 取全部存活敌人，按槽位从左到右

        public List<Enemy> GetAliveEnemies()
        {
            List<Enemy> alive = new();
            foreach (Enemy enemy in Enemies)
                if (!enemy.Creature.IsDead)
                    alive.Add(enemy);
            alive.Sort((left_, right_) => left_.Slot.CompareTo(right_.Slot));
            return alive;
        }

        #endregion

        #region 查战斗副本对应的局内卡，查不到即为战斗内无源临时卡

        public bool TryGetRunCard(CardModel battleCard_, out CardModel runCard_) =>
            _runCardOf.TryGetValue(battleCard_, out runCard_);

        #endregion

        public void RegisterErased(CardModel runCard_) => _erasedRunCards.Add(runCard_);
    }
}