using System;
using _DreamKeep.ConfigData;
using _DreamKeep.GameBattle;
using _DreamKeep.RuntimeData;
using _DreamKeep.Utility;
using Cysharp.Threading.Tasks;
using Spine;
using Spine.Unity;
using UnityEngine;

namespace _DreamKeep.Presentation
{
    /// <summary>战斗单位视图基类：玩家与敌人共用的那部分表现——绑定生物、装配血条/格挡/状态面板、订阅共享域事件、提供目标提示，
    /// 并承载 Spine 演出时序（受击/格挡/出招/技能/死亡，演完才落账）；受击/攻击/死亡三动画为全体单位骨骼资产统一命名的公约演出，
    /// 待机/技能/格挡等其余动画名由具体单位类覆写，返回 null 即禁用该项演出；
    /// 体型适配以骨架包围盒为单真源，TargetTip 高亮框、命中区、血条宽度全部由它派生；
    /// 它不认识意图与阵型移动：玩家与敌人的差异分别在 UI_Player 与 UI_Enemy 两个子类里。</summary>
    public abstract class UI_Actor : MonoBehaviour
    {
        /// <summary>死亡停留时长：death 播完后停末帧的秒数，到时自毁视图（全单位通用演出规则）。</summary>
        private const float DeathHoldDuration = 2f;

        // 公约动画名：全体单位骨骼资产统一命名的导出契约（受击/攻击/死亡），直接写死
        private const string HitAnim = "hurt";
        private const string AttackAnim = "attack";
        private const string DeathAnim = "die";

        [SerializeField] private GameObject _targetTip;
        [SerializeField] private UI_HealthBar _healthBar;
        [SerializeField] private UI_BlockView _blockView;
        [SerializeField] private UI_PerkGroup _perkGroup;

        /// <summary>绑定的域侧生物数据：事件按它过滤，血量、格挡与状态真值都从它读。</summary>
        protected Creature _boundCreature;

        /// <summary>骨架包围盒（相对骨架原点的画布 px）：TargetTip 几何、命中区与血条宽度的派生真源，
        /// 零宽 = 骨架未导出包围盒，派生链全部停用、保持预制体现状。</summary>

        // 各单位自有动画名（默认 null = 禁用该演出，具体单位类按各自骨骼资产的动画清单覆写）
        protected virtual string StandAnim => null;

        protected virtual string SkillAnim => null;
        protected virtual string BlockAnim => null;
        protected virtual string ParryAnim => null;
        protected virtual string ParryStandAnim => null;

        /// <summary>格挡过渡动画只播前段的比例（动画资源过长时裁剪），跟 parry 动画走。</summary>
        protected virtual float ParryClipEnd01 => 1f;

        private SkeletonGraphic _skeleton;
        protected UI_ActorRoot _actorRoot;
        private bool _targetTipShown;

        /// <summary>死亡停帧标记：death 播完后忽略一切动画请求（托管字段，宿主销毁后仍可安全读取）。</summary>
        private bool _dead;

        /// <summary>是否处于格挡站姿：parry 过渡播过即视为已进入，避免重复获得格挡时重播过渡。</summary>
        private bool _inParryStance;

        #region 对外暴露的目标提示：由目标提示驱动按"这张牌会命中的目标"点亮与熄灭

        public void ShowTargetTip(bool show_)
        {
            if (!_targetTip || _targetTipShown == show_) return;
            _targetTipShown = show_;
            _targetTip.gameObject.SetActive(show_);
        }

        /// <summary>给定世界点是否落在本单位命中区内：优先判适配后的 TargetTip 骨架盒（与高亮框同源、随体型伸缩），
        /// 未接提示或包围盒无效时回退判视图根矩形；目标提示驱动按它裁定箭头指向了哪个单位。</summary>
        public bool ContainsWorldPoint(Vector3 world_) =>
            UiGeometry.Contains((RectTransform)(_targetTip ? _targetTip.transform : transform), world_);

        #endregion

        #region 对外暴露的装配入口：子类绑到自己的生物后转调

        /// <summary>统一装配：绑生物 → 取预制体烤死的骨架进标准站姿 → 骨架包围盒体型适配 → 订阅演出与共享事件
        /// → 面板初始化 → 补建初始状态出场槽位 → 登记进视图注册表；骨架未导出包围盒则适配停用（面板照常工作）。</summary>
        protected void Bind(BattleEventDispatcher eventDispatcher_, Creature creature_)
        {
            _boundCreature = creature_;
            // 视图一律生成在本节点（ActorRoot）之下：装配时向上就近查找并缓存，登记与敌人槽位查询共用同一来源
            _actorRoot = GetComponentInParent<UI_ActorRoot>();

            // 演出三件先订（与拆分前演出类的订阅顺序一致），共享面板事件随后
            eventDispatcher_.Register<BE_CardPlayed>(OnCardPlayedAsync);
            eventDispatcher_.Register<BE_DamageDealt>(OnSelfDamageDealtAsync);
            eventDispatcher_.Register<BE_ActorDied>(OnActorDiedAsync);
            eventDispatcher_.Register<BE_DamageDealt>(OnDamageDealtAsync);
            eventDispatcher_.Register<BE_BlockGained>(OnBlockGainedAsync);
            eventDispatcher_.Register<BE_BlockCleared>(OnBlockClearedAsync);
            eventDispatcher_.Register<BE_HealthRestored>(OnHealthRestoredAsync);
            eventDispatcher_.Register<BE_PerkApplied>(OnPerkAppliedAsync);
            eventDispatcher_.Register<BE_PerkDecayed>(OnPerkDecayedAsync);
            eventDispatcher_.Register<BE_PerkRemoved>(OnPerkRemovedAsync);

            _healthBar.Initialize(creature_);
            _blockView.Initialize(creature_);
            RestoreStartPerks(creature_);
            _actorRoot.Register(creature_, this);
        }

        #endregion


        // 出场补建初始状态槽位：初始 perk 随装配自带、错过 BE_PerkApplied，事件之外唯一的建槽路径
        private void RestoreStartPerks(Creature creature_)
        {
            if (_perkGroup == null) return;
            for (int i = 0; i < creature_.Powers.Count; i++)
                _perkGroup.AddPerk(creature_.Powers[i]);
        }

        // 销毁即注销登记：覆盖敌人死亡自我销毁这条路径；子类覆写时先处理自己的收尾再转调
        // root 是 MonoBehaviour，判空必须走 Unity 重载的 ==，不能用 ?.（会绕开假 null 判定）
        protected virtual void OnDestroy()
        {
            if (_actorRoot != null) _actorRoot.Unregister(_boundCreature);
        }

        // 出牌手势：只属技能/能力卡（攻击卡静默——挥拳由伤害事件驱动；未配技能动画名则整条静默）
        private async UniTask OnCardPlayedAsync(BE_CardPlayed played_)
        {
            if (this == null || _dead || string.IsNullOrEmpty(SkillAnim)) return;
            switch (played_.Card.Type)
            {
                case ECardType.Skill:
                case ECardType.Ability:
                    await PlayOnceAsync(SkillAnim);
                    await ReturnToStanceAsync();
                    break;
            }
        }

        // 自己造成伤害即攻击演出：多段伤害逐段挥拳，演完按格挡真值回位
        private async UniTask OnSelfDamageDealtAsync(BE_DamageDealt dealt_)
        {
            if (this == null || _dead || dealt_.Source != _boundCreature) return;
            await PlayOnceAsync(AttackAnim);
            await ReturnToStanceAsync();
        }

        // 伤害结算：域侧先扣格挡再扣血后才发事件，受击演出演完才一起对齐血与格挡真值
        private async UniTask OnDamageDealtAsync(BE_DamageDealt dealt_)
        {
            if (dealt_.Target != _boundCreature) return;
            await PlayHitAsync(dealt_);
            if (_blockView == null || _healthBar == null) return; // 视图已随死亡销毁：托管处理器仍存活，但不再碰 Unity 对象
            _blockView.SetBlock(dealt_.Target.Block);
            _healthBar.SetHealth(dealt_.Target.CurrentHp, true);
        }

        // 受击演出：致死伤不播（death 由 BE_ActorDied 接管）、未破防无真伤不播（保持格挡站姿），演完回标准站姿
        private async UniTask PlayHitAsync(BE_DamageDealt dealt_)
        {
            if (_dead || _boundCreature.IsDead) return;
            if (dealt_.Amount <= dealt_.Absorbed) return;
            await PlayOnceAsync(HitAnim);
            await ReturnToStanceAsync(); // 破防时格挡必归零，正常回标准站姿
        }

        // 格挡获得：演出演完才对齐显示值，获得、吸收衰减与回合清空三条路径共用这一个写入口
        private async UniTask OnBlockGainedAsync(BE_BlockGained gained_)
        {
            if (gained_.Target != _boundCreature) return;
            await PlayBlockGainAsync(gained_);
            if (_blockView == null) return;
            _blockView.SetBlock(gained_.Target.Block);
        }

        // 获得格挡演出：配了格挡站姿则先播 parry 过渡进站姿，否则播一次性格挡动画
        private async UniTask PlayBlockGainAsync(BE_BlockGained gained_)
        {
            if (_dead) return;
            if (!string.IsNullOrEmpty(ParryStandAnim))
            {
                await EnterParryStanceAsync();
                return;
            }

            if (string.IsNullOrEmpty(BlockAnim)) return;
            await PlayOnceAsync(BlockAnim);
            await ReturnToStanceAsync();
        }

        // 回合开始清格挡：先退出格挡站姿硬切回标准站姿，再对齐显示值
        private UniTask OnBlockClearedAsync(BE_BlockCleared cleared_)
        {
            if (cleared_.Target != _boundCreature || _dead) return UniTask.CompletedTask;
            ExitParryStance();
            if (_blockView == null) return UniTask.CompletedTask;
            _blockView.SetBlock(0);
            return UniTask.CompletedTask;
        }

        private UniTask OnHealthRestoredAsync(BE_HealthRestored restored_)
        {
            if (restored_.Target != _boundCreature || _healthBar == null) return UniTask.CompletedTask;
            _healthBar.SetHealth(restored_.Target.CurrentHp, true);
            return UniTask.CompletedTask;
        }

        private UniTask OnPerkAppliedAsync(BE_PerkApplied applied_)
        {
            if (applied_.Target != _boundCreature || _perkGroup == null) return UniTask.CompletedTask;
            _perkGroup.AddPerk(applied_.Perk);
            return UniTask.CompletedTask;
        }

        // 状态层数递减（回合边界未归零）：刷新对应槽位的层数显示
        private UniTask OnPerkDecayedAsync(BE_PerkDecayed decayed_)
        {
            if (decayed_.Target != _boundCreature || _perkGroup == null) return UniTask.CompletedTask;
            _perkGroup.RefreshPerkLayers(decayed_.Perk);
            return UniTask.CompletedTask;
        }

        // 状态移除（回合末衰减归零）：Perk 是被摘下的挂载实例，槽位按状态类型对号删除
        private UniTask OnPerkRemovedAsync(BE_PerkRemoved removed_)
        {
            if (removed_.Target != _boundCreature || _perkGroup == null) return UniTask.CompletedTask;
            _perkGroup.RemovePerk(removed_.Perk);
            return UniTask.CompletedTask;
        }

        // 回位统一入口：有格挡且配了格挡站姿则进站姿，否则硬切标准站姿
        private UniTask ReturnToStanceAsync()
        {
            if (_dead || _boundCreature.IsDead) return UniTask.CompletedTask;
            if (_boundCreature.Block > 0 && !string.IsNullOrEmpty(ParryStandAnim))
                return EnterParryStanceAsync();
            ExitParryStance();
            return UniTask.CompletedTask;
        }

        // 进格挡站姿：先播一次 parry 过渡（可裁剪），已在站姿则仅保持循环
        private async UniTask EnterParryStanceAsync()
        {
            if (_dead) return;
            if (_inParryStance)
            {
                EnterLoop(ParryStandAnim);
                return;
            }

            _inParryStance = true; // 进入即置位，避免过渡播放期间被重入再播一遍
            if (!string.IsNullOrEmpty(ParryAnim))
            {
                await PlayOnceAsync(ParryAnim, ParryClipEnd01);
                if (_dead) return; // 过渡期间若已死亡，交由 death 演出去，勿覆盖
            }

            EnterLoop(ParryStandAnim);
        }

        // 退出格挡态：清标志并硬切回标准站姿，未处于站姿则不动
        private void ExitParryStance()
        {
            if (!_inParryStance) return;
            _inParryStance = false;
            if (!string.IsNullOrEmpty(StandAnim)) EnterLoop(StandAnim);
        }

        // 死亡：置停帧标记后播 death，播完停留再自毁整个视图（停留段不阻塞事件链）
        private async UniTask OnActorDiedAsync(BE_ActorDied died_)
        {
            if (died_.Creature != _boundCreature || _dead) return;
            _dead = true;
            await PlayOnceAsync(DeathAnim);
            HoldThenDestroyAsync().Forget();
        }

        // 死亡停留：到时自毁；期间宿主已被外部销毁（战斗结束清场）则安静收场
        private async UniTaskVoid HoldThenDestroyAsync()
        {
            await UniTask.Delay(TimeSpan.FromSeconds(DeathHoldDuration));
            if (this == null) return;
            Destroy(gameObject);
        }

        private void EnterLoop(string animationName_)
        {
            if (!_skeleton.IsValid) _skeleton.Initialize(false);
            _skeleton.AnimationState.SetAnimation(0, animationName_, true);
        }

        private async UniTask PlayOnceAsync(string animationName_, float clipEnd01_ = 1f)
        {
            if (!_skeleton.IsValid) _skeleton.Initialize(false);
            TrackEntry entry = _skeleton.AnimationState.SetAnimation(0, animationName_, false);
            if (clipEnd01_ < 1f) // 仅播放动画前 clipEnd01_ 比例，到时即完成并停在裁剪帧
                entry.TrackEnd = entry.Animation.Duration * Mathf.Clamp01(clipEnd01_);
            UniTaskCompletionSource finished = new();
            entry.Complete += _ => finished.TrySetResult();
            entry.End += _ => finished.TrySetResult();
            await finished.Task;
        }
    }
}