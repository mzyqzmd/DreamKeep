using System;
using _DreamKeep.ConfigData;
using _DreamKeep.GameBattle;
using _DreamKeep.RuntimeData;
using _DreamKeep.Utility;
using Cysharp.Threading.Tasks;
using DG.Tweening;
using Spine.Unity;
using TMPro;
using UnityEngine;

namespace _DreamKeep.Presentation
{
    /// <summary>战斗单位视图基类：绑定生物、面板装配、共享事件、目标提示、悬停名牌与描边高亮、演出编排（含"演完才落账"的时序）。
    /// 骨架句柄、四条公约动画名与播放原语都在 UI_ActorAnimTool；意图与移动在 UI_Enemy；悬停演出与命中框在 UI_ActorHoverBox。</summary>
    [RequireComponent(typeof(UI_ActorAnimTool))]
    [RequireComponent(typeof(UI_ActorHoverBox))]
    public abstract class UI_Actor : MonoBehaviour
    {
        [Header("基础配置")] [SerializeField] protected UI_HealthBar _healthBar;
        [SerializeField] protected UI_BlockView _blockView;
        [SerializeField] protected UI_PerkGroup _perkGroup;
        [Header("骨骼资源配置")] [SerializeField] protected SkeletonGraphic _skeleton; // 演出用骨架
        [Header("成为目标时提示")] [SerializeField] private GameObject _targetTip; // 箭头指向本单位时亮起的提示
        [Header("悬停信息显示")] [SerializeField] private TextMeshProUGUI _actorName;
        [SerializeField] private Shader _outlineShader; // 描边 shader 引用，留空则运行时按名查找
        [SerializeField] private Material _skeletonMaterial;

        // 公约四条动画名：十条骨架统一导出，全单位一致，写死不可改（将来某单位要做状态变体时再议）
        protected const string IdleLoopAnim = "idle_loop";
        protected const string AttackAnim = "attack";
        protected const string HitAnim = "hurt";
        protected const string DeathAnim = "die";

        // 悬停名牌与描边的配置出口：唯一消费者是 UI_ActorHoverBox（它按指针事件演出），放这里是因为配置本来就归单位
        public SkeletonGraphic Skeleton => _skeleton;
        public GameObject TargetTip => _targetTip;
        public TextMeshProUGUI ActorName => _actorName;
        public Shader OutlineShader => _outlineShader;
        public Material SkeletonMaterial => _skeletonMaterial;

        /// <summary>单位命中框：悬停演出、目标提示点亮与箭头命中判定都在它那里，由 UI_TargetDirector 取用。</summary>
        public UI_ActorHoverBox HoverBox => _hoverBox;

        /// <summary>绑定的域侧生物：事件按它过滤，血量、格挡与状态真值都从它读。</summary>
        protected Creature _boundCreature;

        /// <summary>战场单位区（槽位坐标 + 视图登记表），装配时向上就近查找。</summary>
        protected UI_ActorRoot _actorRoot;

        protected UI_ActorAnimTool _animTool;
        private UI_ActorHoverBox _hoverBox;

        /// <summary>已播完死亡动画：此后忽略一切动画请求（托管字段，宿主销毁后仍可安全读）。</summary>
        protected bool _deathAnimTriggered;

        protected void Awake()
        {
            _animTool = GetComponent<UI_ActorAnimTool>();
            _hoverBox = GetComponent<UI_ActorHoverBox>(); // 与单位视图同挂根节点
        }

        // 销毁即收掉悬停补间与描边副本，再注销登记（覆盖敌人死亡自毁这条路径）；子类覆写时先收自己的尾再转调
        protected virtual void OnDestroy()
        {
            if (_actorRoot) _actorRoot.Unregister(_boundCreature);
        }

        #region 装配入口：子类绑好自己的生物后转调

        /// <summary>统一装配：绑生物 → 校验公约动画名 → 订阅演出与共享事件 → 初始化面板 → 补建初始状态槽位 → 登记进注册表 → 进入待机循环。</summary>
        protected void Bind(BattleEventDispatcher eventDispatcher_, Creature creature_)
        {
            // 绑定事件和生物属性类
            eventDispatcher_.Register<BE_BattleStarted>(OnBattleStarted);
            eventDispatcher_.Register<BE_ActorDied>(OnActorDiedAsync);
            eventDispatcher_.Register<BE_DamageDealt>(OnDealtDamageAsync);
            eventDispatcher_.Register<BE_DamageDealt>(OnDamagedAsync);
            eventDispatcher_.Register<BE_BlockGained>(OnBlockGainedAsync);
            eventDispatcher_.Register<BE_BlockCleared>(OnBlockClearedAsync);
            eventDispatcher_.Register<BE_HealthRestored>(OnHealthRestoredAsync);
            eventDispatcher_.Register<BE_PerkApplied>(OnPerkAppliedAsync);
            eventDispatcher_.Register<BE_PerkDecayed>(OnPerkDecayedAsync);
            eventDispatcher_.Register<BE_PerkRemoved>(OnPerkRemovedAsync);
            _boundCreature = creature_;
            // 把自己注册给_actorRoot
            _actorRoot = GetComponentInParent<UI_ActorRoot>();
            _actorRoot.Register(creature_, this);
            //检查骨骼上是否有动画名
            _animTool.VerifyAnim(IdleLoopAnim, AttackAnim, HitAnim, DeathAnim); // 名字配错就在开局报错，而不是静默播空

            //配置血条格挡信息栏
            _healthBar.Initialize(creature_);
            _blockView.Initialize(creature_);
            // 初始状态随装配自带perk
            foreach (var p in creature_.Powers)
                _perkGroup.AddPerk(p);
        }

        #endregion

        #region 绑定战斗相关事件用于演绎

        protected virtual UniTask OnBattleStarted(BE_BattleStarted started_)
        {
            if (_boundCreature.IsDead) return UniTask.CompletedTask;
            _animTool.PlayLoop(IdleLoopAnim);
            return UniTask.CompletedTask;
        }

        // 死亡：标记停帧 → 播 die → 停 DeathHoldDuration 秒后自毁视图（等停帧这段不阻塞事件链）
        protected virtual async UniTask OnActorDiedAsync(BE_ActorDied died_)
        {
            if (died_.Creature != _boundCreature || _deathAnimTriggered) return;
            _deathAnimTriggered = true;
            if (_hoverBox) _hoverBox.enabled = false; // 尸体不再响应悬停：禁用会触发它自己的兜底收起
            await _animTool.PlayOnceAsync(DeathAnim);
            UniTask.Delay(TimeSpan.FromSeconds(1f))
                .ContinueWith(() =>
                {
                    if (this != null) Destroy(gameObject);
                })
                .Forget();
        }

        // 我打人：播攻击动画，演完回待机（多段伤害逐段挥拳）
        protected virtual async UniTask OnDealtDamageAsync(BE_DamageDealt dealt_)
        {
            if (_boundCreature.IsDead || dealt_.Source != _boundCreature) return;
            await _animTool.PlayOnceAsync(AttackAnim);
            _animTool.PlayLoop(IdleLoopAnim);
        }

        // 我挨打：播受击动画并刷新血与格挡。致死伤不播（die 由 BE_ActorDied 接管）、未破防无真伤也不播
        protected virtual async UniTask OnDamagedAsync(BE_DamageDealt dealt_)
        {
            if (dealt_.Target != _boundCreature) return;
            if (!_boundCreature.IsDead && dealt_.Amount > dealt_.Absorbed)
            {
                await _animTool.PlayOnceAsync(HitAnim);
                if (_boundCreature.IsDead) return;
                _animTool.PlayLoop(IdleLoopAnim);
            }

            // 域侧先扣格挡再扣血才发事件，这里演完再一起对齐真值
            if (_blockView == null || _healthBar == null) return; // 视图可能已随死亡销毁，而托管处理器仍活着
            _blockView.SetBlock(dealt_.Target.Block);
            _healthBar.SetHealth(dealt_.Target.CurrentHp, true);
        }

        // 获得格挡：没有格挡动画，只对齐显示值
        protected virtual UniTask OnBlockGainedAsync(BE_BlockGained gained_)
        {
            if (gained_.Target == _boundCreature && _blockView != null) _blockView.SetBlock(gained_.Target.Block);
            return UniTask.CompletedTask;
        }

        // 回合开始清格挡：同样只对齐显示值
        protected virtual UniTask OnBlockClearedAsync(BE_BlockCleared cleared_)
        {
            if (cleared_.Target == _boundCreature && _blockView != null) _blockView.SetBlock(0);
            return UniTask.CompletedTask;
        }

        // 回血：血条滚到真值
        protected virtual UniTask OnHealthRestoredAsync(BE_HealthRestored restored_)
        {
            if (restored_.Target == _boundCreature && _healthBar != null)
                _healthBar.SetHealth(restored_.Target.CurrentHp, true);
            return UniTask.CompletedTask;
        }

        // 施加状态：新建槽位或叠加层数
        protected virtual UniTask OnPerkAppliedAsync(BE_PerkApplied applied_)
        {
            if (applied_.Target == _boundCreature && _perkGroup != null) _perkGroup.AddPerk(applied_.Perk);
            return UniTask.CompletedTask;
        }

        // 状态层数递减：刷新对应槽位的层数显示
        protected virtual UniTask OnPerkDecayedAsync(BE_PerkDecayed decayed_)
        {
            if (decayed_.Target == _boundCreature && _perkGroup != null) _perkGroup.RefreshPerkLayers(decayed_.Perk);
            return UniTask.CompletedTask;
        }

        // 状态移除：Perk 是被摘下的挂载实例，槽位按状态类型对号删除
        protected virtual UniTask OnPerkRemovedAsync(BE_PerkRemoved removed_)
        {
            if (removed_.Target == _boundCreature && _perkGroup != null) _perkGroup.RemovePerk(removed_.Perk);
            return UniTask.CompletedTask;
        }

        #endregion
    }
}