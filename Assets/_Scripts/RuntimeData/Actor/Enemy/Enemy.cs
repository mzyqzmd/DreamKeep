using System.Collections.Generic;
using _DreamKeep.ConfigData;

namespace _DreamKeep.RuntimeData
{
    /// <summary>敌人：占据一个纵深敌人槽位（1 最靠前、4 最靠后，可在槽位间移动），持有一串运行时意图并按顺序循环取用。
    /// 对外意图走三级覆盖：击晕 > 站最前槽位的替换意图 > 序列当前意图（执行与预告统一走 PreviewIntent）；
    /// 双阶段敌人在装配期收下二阶段意图表，按配置决定由掉血阈值还是阶段一意图执行完触发切换。</summary>
    public class Enemy
    {
        private static readonly EnemyIntent StunIntent = new EI_Stun(); // 无字段，全体被击晕的敌人共享同一实例

        private readonly List<EnemyIntent> _intents;
        private readonly EnemyIntent _frontSlotIntent;
        private readonly SO_EnemySpeechLines _speechLines;
        private readonly string _skinName;
        private List<EnemyIntent> _phaseTwoIntents;
        private EnemyIntent _phaseTwoFrontSlotIntent;
        private bool _isPhaseTwo;
        private bool _switchPhaseAfterFirstIntent;
        private int _intentIndex;

        public Creature Creature { get; }
        public int Slot { get; private set; }

        /// <summary>本场战斗的台词分组：装配期一次写入，null 表示全程不说话。</summary>
        public SO_EnemySpeechLines SpeechLines => _speechLines;

        /// <summary>本场战斗的 Spine 皮肤名：装配期一次写入，空串表示用默认皮肤。</summary>
        public string SkinName => _skinName;

        /// <summary>是否处于击晕：本回合意图被替换为击晕，执行时跳过行动且不推进意图。</summary>
        public bool IsStunned { get; private set; }

        /// <summary>是否处于阶段二：仅双阶段敌人经 SwitchToPhaseTwo 进入，普通敌人恒为阶段一。</summary>
        public bool IsPhaseTwo => _isPhaseTwo;

        /// <summary>是否应在本次阶段一意图执行后自动切阶段：仅由配置开关启用，切入阶段二后恒 false。</summary>
        public bool ShouldSwitchPhaseAfterIntent => !_isPhaseTwo && _switchPhaseAfterFirstIntent;

        /// <summary>本次将要执行的意图序号：表现层据此查台词目录，须在 AdvanceIntent() 之前读出。</summary>
        public int IntentIndex => _intentIndex;

        // 当前生效的意图表与前槽替换意图：阶段二未装载时回退阶段一，切换不至于把敌人变哑
        private List<EnemyIntent> ActiveIntents => _isPhaseTwo && _phaseTwoIntents != null ? _phaseTwoIntents : _intents;
        private EnemyIntent ActiveFrontSlotIntent => _isPhaseTwo && _phaseTwoFrontSlotIntent != null ? _phaseTwoFrontSlotIntent : _frontSlotIntent;

        /// <summary>本次将要执行的意图；没有配置意图时为 null。</summary>
        public EnemyIntent CurrentIntent => ActiveIntents.Count == 0 ? null : ActiveIntents[_intentIndex];

        /// <summary>对外展示的意图：击晕优先，其次站最前敌人槽位且配置了替换意图时显示它（打完即回位），否则显示序列当前意图。</summary>
        public EnemyIntent PreviewIntent =>
            IsStunned ? StunIntent
            : Slot == BattleSlots.FrontEnemy && ActiveFrontSlotIntent != null ? ActiveFrontSlotIntent
            : CurrentIntent;

        public Enemy(string name_, int maxHp_, int slot_, List<EnemyIntent> intents_, EnemyIntent frontSlotIntent_,
            SO_EnemySpeechLines speechLines_, string skinName_ = "")
        {
            Creature = new Creature(name_, maxHp_);
            Slot = slot_;
            _intents = intents_ ?? new List<EnemyIntent>();
            _frontSlotIntent = frontSlotIntent_;
            _speechLines = speechLines_;
            _skinName = skinName_;
        }

        #region 击晕：由位移撞击施加，敌人回合执行一次后清除

        /// <summary>施加击晕，把本回合意图覆盖成击晕。</summary>
        public void ApplyStun() => IsStunned = true;

        /// <summary>消费击晕并恢复原意图（不推进意图序列），由敌人回合在跳过一次行动后调用。</summary>
        public void ConsumeStun() => IsStunned = false;

        #endregion

        #region 双阶段：装配期收下二阶段意图表，触发时整表切换并归零意图索引

        /// <summary>装载二阶段意图：装配期调用一次，只收烘焙好的运行时意图，运行时零配置引用。</summary>
        public void LoadPhaseTwo(List<EnemyIntent> intents_, EnemyIntent frontSlotIntent_)
        {
            _phaseTwoIntents = intents_ ?? new List<EnemyIntent>();
            _phaseTwoFrontSlotIntent = frontSlotIntent_;
        }

        /// <summary>标记阶段一意图执行一次后自动切阶段：装配期由配置写入。</summary>
        public void MarkSwitchPhaseAfterFirstIntent() => _switchPhaseAfterFirstIntent = true;

        /// <summary>切入阶段二：整表换成二阶段并归零意图索引；已处于阶段二时直接返回（幂等）。</summary>
        public void SwitchToPhaseTwo()
        {
            if (_isPhaseTwo) return;
            _isPhaseTwo = true;
            _intentIndex = 0;
        }

        #endregion

        #region 推进到下一条意图，按顺序循环
        public void AdvanceIntent()
        {
            List<EnemyIntent> intents = ActiveIntents;
            if (intents.Count == 0) return;
            _intentIndex = (_intentIndex + 1) % intents.Count;
        }
        #endregion

        #region 变更所在槽位，由移动动作调用
        public void SetSlot(int slot_) => Slot = slot_;
        #endregion
    }
}
