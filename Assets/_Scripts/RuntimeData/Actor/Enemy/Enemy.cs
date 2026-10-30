using System.Collections.Generic;

namespace _DreamKeep.RuntimeData
{
    /// <summary>敌人：占据一个纵深敌人槽位（1 最靠前、3 最靠后，可在槽位间移动），持有一串运行时意图并按顺序循环取用。
    /// 对外意图走三级覆盖：击晕 > 站最前槽位的替换意图 > 序列当前意图（执行与预告统一走 PreviewIntent）。</summary>
    public class Enemy
    {
        private static readonly EnemyIntent StunIntent = new EI_Stun(); // 无字段，全体被击晕的敌人共享同一实例

        private readonly List<EnemyIntent> _intents;
        private readonly EnemyIntent _frontSlotIntent;
        private int _intentIndex;

        public Creature Creature { get; }
        public int Slot { get; private set; }

        /// <summary>是否处于击晕：本回合意图被替换为击晕，执行时跳过行动且不推进意图。</summary>
        public bool IsStunned { get; private set; }

        /// <summary>本次将要执行的意图；没有配置意图时为 null。</summary>
        public EnemyIntent CurrentIntent => _intents.Count == 0 ? null : _intents[_intentIndex];

        /// <summary>对外展示的意图：击晕优先，其次站最前敌人槽位且配置了替换意图时显示它（打完即回位），否则显示序列当前意图。</summary>
        public EnemyIntent PreviewIntent =>
            IsStunned ? StunIntent
            : Slot == BattleSlots.FrontEnemy && _frontSlotIntent != null ? _frontSlotIntent
            : CurrentIntent;

        public Enemy(string name_, int maxHp_, int slot_, List<EnemyIntent> intents_, EnemyIntent frontSlotIntent_)
        {
            Creature = new Creature(name_, maxHp_);
            Slot = slot_;
            _intents = intents_ ?? new List<EnemyIntent>();
            _frontSlotIntent = frontSlotIntent_;
        }

        #region 击晕：由位移撞击施加，敌人回合执行一次后清除

        /// <summary>施加击晕，把本回合意图覆盖成击晕。</summary>
        public void ApplyStun() => IsStunned = true;

        /// <summary>消费击晕并恢复原意图（不推进意图序列），由敌人回合在跳过一次行动后调用。</summary>
        public void ConsumeStun() => IsStunned = false;

        #endregion

        #region 推进到下一条意图，按顺序循环
        public void AdvanceIntent()
        {
            if (_intents.Count == 0) return;
            _intentIndex = (_intentIndex + 1) % _intents.Count;
        }
        #endregion

        #region 变更所在槽位，由移动动作调用
        public void SetSlot(int slot_) => Slot = slot_;
        #endregion
    }
}
