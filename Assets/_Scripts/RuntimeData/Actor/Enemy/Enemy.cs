using System.Collections.Generic;

namespace _DreamKeep.RuntimeData
{
    /// <summary>敌人：占据一个纵深敌人槽位（1 最靠前、3 最靠后，可在槽位间移动），持有一串运行时意图并按顺序循环取用；站最前槽位时当前意图被替换意图临时顶替（执行与预告统一走 PreviewIntent）。</summary>
    public class Enemy
    {
        private readonly List<EnemyIntent> _intents;
        private readonly EnemyIntent _frontSlotIntent;
        private int _intentIndex;

        public Creature Creature { get; }
        public int Slot { get; private set; }
        public string Name { get; }

        /// <summary>本次将要执行的意图；没有配置意图时为 null。</summary>
        public EnemyIntent CurrentIntent => _intents.Count == 0 ? null : _intents[_intentIndex];

        /// <summary>对外展示的意图：站最前敌人槽位且配置了替换意图时显示它（打完即回位），否则显示序列当前意图。</summary>
        public EnemyIntent PreviewIntent =>
            Slot == 1 && _frontSlotIntent != null ? _frontSlotIntent : CurrentIntent;

        public Enemy(string name_, int maxHp_, int slot_, List<EnemyIntent> intents_, EnemyIntent frontSlotIntent_)
        {
            Name = name_;
            Creature = new Creature(maxHp_);
            Slot = slot_;
            _intents = intents_ ?? new List<EnemyIntent>();
            _frontSlotIntent = frontSlotIntent_;
        }

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
