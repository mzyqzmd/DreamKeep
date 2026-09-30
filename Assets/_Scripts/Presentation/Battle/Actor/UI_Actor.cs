using _DreamKeep.GameBattle;
using _DreamKeep.RuntimeData;
using Cysharp.Threading.Tasks;
using UnityEngine;
using VContainer;

namespace _DreamKeep.Presentation
{
    public abstract class UI_Actor : MonoBehaviour
    {
        [SerializeField] protected GameObject _targetTip;
        [SerializeField] protected UI_InfoPanel _infoPanel;

        /// <summary>绑定的域侧生物数据：事件按它过滤，血量、格挡与状态真值都从它读。</summary>
        protected Creature _boundCreature;

        private bool _targetTipShown;

        #region 对子类暴露的装配入口：绑定生物数据与注册事件

        [Inject]
        public virtual void Construct(BattleContext battleContext_, BattleEventDispatcher eventDispatcher_)
        {
            _infoPanel.Init(_boundCreature);
            eventDispatcher_.Register<BE_DamageDealt>(OnDamageDealtAsync);
            eventDispatcher_.Register<BE_BlockGained>(OnBlockGainedAsync);
            eventDispatcher_.Register<BE_BlockCleared>(OnBlockClearedAsync);
            eventDispatcher_.Register<BE_HealthRestored>(OnHealthRestoredAsync);
            eventDispatcher_.Register<BE_PerkApplied>(OnPerkAppliedAsync);
            eventDispatcher_.Register<BE_PerkDecayed>(OnPerkDecayedAsync);
            eventDispatcher_.Register<BE_PerkRemoved>(OnPerkRemovedAsync);
        }

        #endregion

        #region 对子类暴露的动画钩子：默认空实现，覆写后在 HUD 数字刷新前播单位动画（await 演完才落账）

        protected virtual UniTask OnDamageDealtAnimAsync(BE_DamageDealt dealt_) => UniTask.CompletedTask;
        protected virtual UniTask OnBlockGainedAnimAsync(BE_BlockGained gained_) => UniTask.CompletedTask;

        #endregion

        #region 目标提示：拖拽的自目标卡悬在出牌区时由出牌流程点亮

        public void ShowTargetTip(bool show_)
        {
            if (_targetTipShown == show_) return;
            _targetTipShown = show_;
            _targetTip.gameObject.SetActive(show_);
        }

        #endregion


        // 伤害结算：域侧先扣格挡再扣血后才发事件，受击动画演完才一起对齐血与格挡真值
        private async UniTask OnDamageDealtAsync(BE_DamageDealt dealt_)
        {
            if (dealt_.Target != _boundCreature) return;
            await OnDamageDealtAnimAsync(dealt_);
            _infoPanel.SetBlock(dealt_.Target.Block);
            _infoPanel.SetHealth(dealt_.Target.CurrentHp, true);
        }

        // 格挡获得：受击动画演完才对齐显示值，获得、吸收衰减与回合清空三条路径共用这一个写入口
        private async UniTask OnBlockGainedAsync(BE_BlockGained gained_)
        {
            if (gained_.Target != _boundCreature) return;
            await OnBlockGainedAnimAsync(gained_);
            _infoPanel.SetBlock(gained_.Target.Block);
        }

        private UniTask OnBlockClearedAsync(BE_BlockCleared cleared_)
        {
            if (cleared_.Target != _boundCreature) return UniTask.CompletedTask;
            _infoPanel.SetBlock(0);
            return UniTask.CompletedTask;
        }

        private UniTask OnHealthRestoredAsync(BE_HealthRestored restored_)
        {
            if (restored_.Target != _boundCreature) return UniTask.CompletedTask;
            _infoPanel.SetHealth(restored_.Target.CurrentHp, true);
            return UniTask.CompletedTask;
        }

        private UniTask OnPerkAppliedAsync(BE_PerkApplied applied_)
        {
            if (applied_.Target != _boundCreature) return UniTask.CompletedTask;
            _infoPanel.AddPerk(applied_.Perk);
            return UniTask.CompletedTask;
        }

        // 状态层数递减（回合边界未归零）：刷新对应槽位的层数显示
        private UniTask OnPerkDecayedAsync(BE_PerkDecayed decayed_)
        {
            if (decayed_.Target != _boundCreature) return UniTask.CompletedTask;
            _infoPanel.RefreshPerkLayers(decayed_.Perk);
            return UniTask.CompletedTask;
        }

        // 状态移除（回合末衰减归零）：Perk 是被摘下的挂载实例，槽位按状态类型对号删除
        private UniTask OnPerkRemovedAsync(BE_PerkRemoved removed_)
        {
            if (removed_.Target != _boundCreature) return UniTask.CompletedTask;
            _infoPanel.RemovePerk(removed_.Perk);
            return UniTask.CompletedTask;
        }
    }
}