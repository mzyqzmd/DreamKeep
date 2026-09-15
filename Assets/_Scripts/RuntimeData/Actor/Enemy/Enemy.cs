using System.Collections.Generic;

namespace _DreamKeep.RuntimeData
{
    /// <summary>敌人：占据一个固定阵型槽位（槽位索引即从左到右的站位次序），持有一串运行时意图并按顺序循环取用。</summary>
    public class Enemy
    {
        private readonly List<EnemyIntent> _intents;
        private int _intentIndex;

        public Creature Creature { get; }
        public int Slot { get; }
        public string Name { get; }

        public IReadOnlyList<EnemyIntent> Intents => _intents;

        /// <summary>本次将要执行的意图；没有配置意图时为 null。</summary>
        public EnemyIntent CurrentIntent => _intents.Count == 0 ? null : _intents[_intentIndex];

        public Enemy(string name_, int maxHp_, int slot_, List<EnemyIntent> intents_)
        {
            Name = name_;
            Creature = new Creature(maxHp_);
            Slot = slot_;
            _intents = intents_ ?? new List<EnemyIntent>();
        }

        #region 推进到下一条意图，按顺序循环
        public void AdvanceIntent()
        {
            if (_intents.Count == 0) return;
            _intentIndex = (_intentIndex + 1) % _intents.Count;
        }
        #endregion
    }
}
