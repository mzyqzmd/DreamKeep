using System;
using _DreamKeep.GameBattle;
using _DreamKeep.RuntimeData;
using _DreamKeep.Utility;
using Cysharp.Threading.Tasks;
using Spine;
using Spine.Unity;
using UnityEngine;

namespace _DreamKeep.Presentation
{
    /// <summary>战斗单位视图基类：绑定生物、面板装配、共享事件、目标提示与 Spine 演出（含"演完才落账"的时序）。
    /// 待机/攻击/受击/死亡四条动画名是公约，写死在基类；各单位专属动画由各自的类声明；意图与移动在 UI_Enemy。</summary>
    public abstract class UI_Actor : MonoBehaviour
    {
        // 公约四条动画名：十条骨架统一导出，全单位一致，写死不可改（将来某单位要做状态变体时再议）
        private const string IdleAnim = "idle_loop";
        private const string AttackAnim = "attack";
        private const string HitAnim = "hurt";
        private const string DeathAnim = "die";

        // 死亡停帧：die 播完停末帧这么多秒后自毁视图
        private const float DeathHoldDuration = 2f;

        [SerializeField] private SkeletonGraphic _skeleton; // 演出用骨架，未接线时自动就近取
        [SerializeField] private GameObject _targetTip;     // 箭头指向本单位时亮起的提示
        [SerializeField] private UI_HealthBar _healthBar;
        [SerializeField] private UI_BlockView _blockView;
        [SerializeField] private UI_PerkGroup _perkGroup;

        /// <summary>绑定的域侧生物：事件按它过滤，血量、格挡与状态真值都从它读。</summary>
        protected Creature _boundCreature;

        /// <summary>战场单位区（槽位坐标 + 视图登记表），装配时向上就近查找。</summary>
        protected UI_ActorRoot _actorRoot;

        private bool _targetTipShown;

        /// <summary>已播完死亡动画：此后忽略一切动画请求（托管字段，宿主销毁后仍可安全读）。</summary>
        private bool _dead;

        /// <summary>是否已死（含域侧真值）。</summary>
        protected bool IsDead => _dead || _boundCreature.IsDead;

        #region 目标提示：由 UI_TargetTipDirector 按"这张牌会命中的目标"点亮与熄灭

        /// <summary>显示或隐藏目标提示（状态没变就跳过，避免重复 SetActive）。</summary>
        public void ShowTargetTip(bool show_)
        {
            if (!_targetTip || _targetTipShown == show_) return;
            _targetTipShown = show_;
            _targetTip.gameObject.SetActive(show_);
        }

        /// <summary>世界点是否落在本单位命中区内：未接提示时回退判视图根矩形，供箭头命中裁定。</summary>
        public bool ContainsWorldPoint(Vector3 world_) =>
            UiGeometry.Contains((RectTransform)(_targetTip ? _targetTip.transform : transform), world_);

        #endregion

        #region 装配入口：子类绑好自己的生物后转调

        /// <summary>统一装配：绑生物 → 找骨架并校验公约动画名 → 订阅演出与共享事件 → 初始化面板 → 补建初始状态槽位 → 登记进注册表 → 进入待机循环。</summary>
        protected void Bind(BattleEventDispatcher eventDispatcher_, Creature creature_)
        {
            _boundCreature = creature_;
            // 视图一律生成在 ActorRoot 下：向上就近查找，登记与敌人槽位查询共用这份引用
            _actorRoot = GetComponentInParent<UI_ActorRoot>();
            // 骨架未接线时就近取，预制体零接线也能工作
            if (_skeleton == null) _skeleton = GetComponentInChildren<SkeletonGraphic>(true);
            VerifyAnim(IdleAnim, AttackAnim, HitAnim, DeathAnim); // 名字配错就在开局报错，而不是静默播空

            // 演出两件先订（与拆分前演出类的订阅顺序一致），共享面板事件随后
            eventDispatcher_.Register<BE_DamageDealt>(OnDealtDamageAsync);
            eventDispatcher_.Register<BE_ActorDied>(OnActorDiedAsync);
            eventDispatcher_.Register<BE_DamageDealt>(OnDamagedAsync);
            eventDispatcher_.Register<BE_BlockGained>(OnBlockGainedAsync);
            eventDispatcher_.Register<BE_BlockCleared>(OnBlockClearedAsync);
            eventDispatcher_.Register<BE_HealthRestored>(OnHealthRestoredAsync);
            eventDispatcher_.Register<BE_PerkApplied>(OnPerkAppliedAsync);
            eventDispatcher_.Register<BE_PerkDecayed>(OnPerkDecayedAsync);
            eventDispatcher_.Register<BE_PerkRemoved>(OnPerkRemovedAsync);

            _healthBar.Initialize(creature_);
            _blockView.Initialize(creature_);
            // 初始状态随装配自带、错过 BE_PerkApplied，这里补建槽位
            if (_perkGroup != null)
                for (int i = 0; i < creature_.Powers.Count; i++)
                    _perkGroup.AddPerk(creature_.Powers[i]);
            _actorRoot.Register(creature_, this);

            EnterIdleLoop();
        }

        #endregion

        #region 播放原语：子类自订专属演出时调用

        /// <summary>切回公约待机循环（已死则不动）：子类自订演出播完后的收尾入口。</summary>
        protected void EnterIdleLoop()
        {
            if (IsDead || _skeleton == null) return;
            if (!_skeleton.IsValid) _skeleton.Initialize(false);
            _skeleton.AnimationState.SetAnimation(0, IdleAnim, true);
        }

        /// <summary>播一次动画，播完或被截断后放行（骨架缺失时安静跳过）。</summary>
        protected async UniTask PlayOnceAsync(string animationName_)
        {
            if (_skeleton == null) return;
            if (!_skeleton.IsValid) _skeleton.Initialize(false);
            TrackEntry entry = _skeleton.AnimationState.SetAnimation(0, animationName_, false);
            UniTaskCompletionSource finished = new();
            entry.Complete += _ => finished.TrySetResult();
            entry.End += _ => finished.TrySetResult();
            await finished.Task;
        }

        /// <summary>校验动画名是否存在于本单位骨架：Spine 对不存在的名字会静默播空，装配时报出来（只报错，不打断装配）。</summary>
        protected void VerifyAnim(params string[] names_)
        {
            if (_skeleton == null)
            {
                Debug.LogError($"[{GetType().Name}] 未绑定 SkeletonGraphic，动画演出不可用");
                return;
            }

            if (!_skeleton.IsValid) _skeleton.Initialize(false);
            SkeletonData data = _skeleton.Skeleton?.Data;
            if (data == null)
            {
                Debug.LogError($"[{GetType().Name}] 骨架数据未就绪，无法校验动画名");
                return;
            }

            foreach (string name in names_)
                if (data.FindAnimation(name) == null)
                    Debug.LogError($"[{GetType().Name}] 骨架缺少动画：{name}");
        }

        #endregion

        // 销毁即注销登记（覆盖敌人死亡自毁这条路径）；子类覆写时先收自己的尾再转调
        // root 是 MonoBehaviour，判空必须走 Unity 的 ==，不能用 ?.（会绕开假 null 判定）
        protected virtual void OnDestroy()
        {
            if (_actorRoot) _actorRoot.Unregister(_boundCreature);
        }

        // 我打人：播攻击动画，演完回待机（多段伤害逐段挥拳）
        private async UniTask OnDealtDamageAsync(BE_DamageDealt dealt_)
        {
            if (this == null || _dead || dealt_.Source != _boundCreature) return;
            await PlayOnceAsync(AttackAnim);
            EnterIdleLoop();
        }

        // 我挨打：播受击动画并刷新血与格挡。致死伤不播（die 由 BE_ActorDied 接管）、未破防无真伤也不播
        private async UniTask OnDamagedAsync(BE_DamageDealt dealt_)
        {
            if (dealt_.Target != _boundCreature) return;
            if (!IsDead && dealt_.Amount > dealt_.Absorbed)
            {
                await PlayOnceAsync(HitAnim);
                EnterIdleLoop();
            }

            // 域侧先扣格挡再扣血才发事件，这里演完再一起对齐真值
            if (_blockView == null || _healthBar == null) return; // 视图可能已随死亡销毁，而托管处理器仍活着
            _blockView.SetBlock(dealt_.Target.Block);
            _healthBar.SetHealth(dealt_.Target.CurrentHp, true);
        }

        // 获得格挡：没有格挡动画，只对齐显示值
        private UniTask OnBlockGainedAsync(BE_BlockGained gained_)
        {
            if (gained_.Target == _boundCreature && _blockView != null) _blockView.SetBlock(gained_.Target.Block);
            return UniTask.CompletedTask;
        }

        // 回合开始清格挡：同样只对齐显示值
        private UniTask OnBlockClearedAsync(BE_BlockCleared cleared_)
        {
            if (cleared_.Target == _boundCreature && _blockView != null) _blockView.SetBlock(0);
            return UniTask.CompletedTask;
        }

        // 回血：血条滚到真值
        private UniTask OnHealthRestoredAsync(BE_HealthRestored restored_)
        {
            if (restored_.Target == _boundCreature && _healthBar != null) _healthBar.SetHealth(restored_.Target.CurrentHp, true);
            return UniTask.CompletedTask;
        }

        // 施加状态：新建槽位或叠加层数
        private UniTask OnPerkAppliedAsync(BE_PerkApplied applied_)
        {
            if (applied_.Target == _boundCreature && _perkGroup != null) _perkGroup.AddPerk(applied_.Perk);
            return UniTask.CompletedTask;
        }

        // 状态层数递减：刷新对应槽位的层数显示
        private UniTask OnPerkDecayedAsync(BE_PerkDecayed decayed_)
        {
            if (decayed_.Target == _boundCreature && _perkGroup != null) _perkGroup.RefreshPerkLayers(decayed_.Perk);
            return UniTask.CompletedTask;
        }

        // 状态移除：Perk 是被摘下的挂载实例，槽位按状态类型对号删除
        private UniTask OnPerkRemovedAsync(BE_PerkRemoved removed_)
        {
            if (removed_.Target == _boundCreature && _perkGroup != null) _perkGroup.RemovePerk(removed_.Perk);
            return UniTask.CompletedTask;
        }

        // 死亡：标记停帧 → 播 die → 停 DeathHoldDuration 秒后自毁视图（等停帧这段不阻塞事件链）
        private async UniTask OnActorDiedAsync(BE_ActorDied died_)
        {
            if (died_.Creature != _boundCreature || _dead) return;
            _dead = true;
            await PlayOnceAsync(DeathAnim);
            UniTask.Delay(TimeSpan.FromSeconds(DeathHoldDuration))
                .ContinueWith(() => { if (this != null) Destroy(gameObject); })
                .Forget();
        }
    }
}
