using _DreamKeep.GameBattle;
using _DreamKeep.RuntimeData;
using _DreamKeep.Utility;
using Cysharp.Threading.Tasks;
using UnityEngine;

namespace _DreamKeep.Presentation
{
    /// <summary>战斗单位视图基类：玩家与敌人共用的那部分表现——绑定生物、装配血条/格挡/状态面板、订阅七个共享域事件、提供目标提示；
    /// 受击与获得格挡经点播契约先演动画再落账，动画与其余演出归 UI_ActorAnimator。
    /// 它不认识意图与阵型移动：玩家与敌人的差异分别在 UI_Player 与 UI_Enemy 两个子类里，敌人专属能力归具体敌人类。</summary>
    [RequireComponent(typeof(UI_ActorAnimator))]
    public abstract class UI_Actor : MonoBehaviour
    {
        [SerializeField] private GameObject _targetTip;
        [SerializeField] private UI_HealthBar _healthBar;
        [SerializeField] private UI_BlockView _blockView;
        [SerializeField] private UI_PerkGroup _perkGroup;

        /// <summary>绑定的域侧生物数据：事件按它过滤，血量、格挡与状态真值都从它读。</summary>
        protected Creature _boundCreature;

        private UI_ActorAnimator _animator;
        private ActorViewRegistry _registry;
        private bool _targetTipShown;

        #region 对外暴露的目标提示：由目标提示驱动按"这张牌会命中的目标"点亮与熄灭

        public void ShowTargetTip(bool show_)
        {
            if (_targetTip == null || _targetTipShown == show_) return;
            _targetTipShown = show_;
            _targetTip.gameObject.SetActive(show_);
        }

        /// <summary>给定世界点是否落在本视图的矩形内：目标提示驱动按它裁定箭头指向了哪个敌人（与出牌区落点判定共用 UiGeometry.Contains 的口径）。</summary>
        public bool ContainsWorldPoint(Vector3 world_) => UiGeometry.Contains((RectTransform)transform, world_);

        #endregion

        #region 对外暴露的装配入口：子类绑到自己的生物后转调

        /// <summary>统一装配：绑生物 → 面板初始化 → 转发动画器 → 订阅共享事件 → 登记进视图注册表；登记后提示驱动即可按生物找到本视图。</summary>
        protected void Bind(BattleEventDispatcher eventDispatcher_, Creature creature_, ActorViewRegistry registry_)
        {
            _boundCreature = creature_;
            _registry = registry_;
            _healthBar.Initialize(creature_);
            _blockView.Initialize(creature_);
            _animator = GetComponent<UI_ActorAnimator>();
            _animator?.Bind(creature_, eventDispatcher_);
            eventDispatcher_.Register<BE_DamageDealt>(OnDamageDealtAsync);
            eventDispatcher_.Register<BE_BlockGained>(OnBlockGainedAsync);
            eventDispatcher_.Register<BE_BlockCleared>(OnBlockClearedAsync);
            eventDispatcher_.Register<BE_HealthRestored>(OnHealthRestoredAsync);
            eventDispatcher_.Register<BE_PerkApplied>(OnPerkAppliedAsync);
            eventDispatcher_.Register<BE_PerkDecayed>(OnPerkDecayedAsync);
            eventDispatcher_.Register<BE_PerkRemoved>(OnPerkRemovedAsync);
            registry_.Register(creature_, this);
        }

        #endregion

        // 销毁即注销登记：覆盖敌人死亡自我销毁这条路径；子类覆写时先处理自己的收尾再转调
        protected virtual void OnDestroy() => _registry?.Unregister(_boundCreature);

        // 伤害结算：域侧先扣格挡再扣血后才发事件，受击动画演完才一起对齐血与格挡真值
        private async UniTask OnDamageDealtAsync(BE_DamageDealt dealt_)
        {
            if (dealt_.Target != _boundCreature) return;
            if (_animator != null) await _animator.PlayHitAsync(dealt_);
            if (_blockView == null || _healthBar == null) return; // 视图已随死亡销毁：托管处理器仍存活，但不再碰 Unity 对象
            _blockView.SetBlock(dealt_.Target.Block);
            _healthBar.SetHealth(dealt_.Target.CurrentHp, true);
        }

        // 格挡获得：演出演完才对齐显示值，获得、吸收衰减与回合清空三条路径共用这一个写入口
        private async UniTask OnBlockGainedAsync(BE_BlockGained gained_)
        {
            if (gained_.Target != _boundCreature) return;
            if (_animator != null) await _animator.PlayBlockGainAsync(gained_);
            if (_blockView == null) return;
            _blockView.SetBlock(gained_.Target.Block);
        }

        private UniTask OnBlockClearedAsync(BE_BlockCleared cleared_)
        {
            if (cleared_.Target != _boundCreature || _blockView == null) return UniTask.CompletedTask;
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
    }
}
