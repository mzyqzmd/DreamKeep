using _DreamKeep.GameBattle;
using _DreamKeep.RuntimeData;
using Cysharp.Threading.Tasks;
using UnityEngine;

namespace _DreamKeep.Presentation
{
    /// <summary>战斗单位视图基类：玩家与敌人共用的一套战场反馈——目标提示、血条、格挡与状态槽位。
    /// 自己订阅域事件并按 Target 生物引用过滤（同一事件多单位在场互不串扰），写视图的活全权交给 UI_InfoPanel；
    /// 子类只负责装配：Bind 域侧生物数据后调用 RegisterActorEvents 接上事件。</summary>
    public class UI_Actor : MonoBehaviour
    {
        [SerializeField] protected UI_TargetTip _targetTip;
        [SerializeField] protected UI_InfoPanel _infoPanel;

        /// <summary>绑定的域侧生物数据：事件按它过滤，血量、格挡与状态真值都从它读。</summary>
        protected Creature BoundCreature { get; private set; }

        private bool _targetTipShown;

        #region 对子类暴露的装配入口：绑定生物数据与注册事件

        public void Bind(Creature creature_) => BoundCreature = creature_;

        // 接线：七个事件的清单集中在这里，处理就在下面；注册前必须先 Bind，否则事件一律被过滤掉
        protected void RegisterActorEvents(BattleEventDispatcher dispatcher_)
        {
            dispatcher_.Register<BE_BlockGained>(OnBlockGainedAsync);
            dispatcher_.Register<BE_DamageDealt>(OnDamageDealtAsync);
            dispatcher_.Register<BE_BlockCleared>(OnBlockClearedAsync);
            dispatcher_.Register<BE_HealthRestored>(OnHealthRestoredAsync);
            dispatcher_.Register<BE_PerkApplied>(OnPerkAppliedAsync);
            dispatcher_.Register<BE_PerkDecayed>(OnPerkDecayedAsync);
            dispatcher_.Register<BE_PerkRemoved>(OnPerkRemovedAsync);
        }

        #endregion

        #region 目标提示：拖拽的自目标卡悬在出牌区时由出牌流程点亮

        public void ShowTargetTip(bool show_)
        {
            if (!_targetTip || _targetTipShown == show_) return;
            _targetTipShown = show_;
            _targetTip.gameObject.SetActive(show_);
        }

        #endregion

        // 格挡获得：显示值一律对齐域侧真值，获得、吸收衰减与回合清空三条路径共用这一个写入口
        private UniTask OnBlockGainedAsync(BE_BlockGained gained_)
        {
            if (gained_.Target != BoundCreature) return UniTask.CompletedTask;
            _infoPanel.SetBlock(gained_.Target.Block);
            return UniTask.CompletedTask;
        }

        // 伤害结算：域侧先扣格挡再扣血后才发事件，血与格挡一起对齐真值
        private UniTask OnDamageDealtAsync(BE_DamageDealt dealt_)
        {
            if (dealt_.Target != BoundCreature) return UniTask.CompletedTask;
            _infoPanel.SetBlock(dealt_.Target.Block);
            _infoPanel.SetHealth(dealt_.Target.CurrentHp, true);
            return UniTask.CompletedTask;
        }

        private UniTask OnBlockClearedAsync(BE_BlockCleared cleared_)
        {
            if (cleared_.Target != BoundCreature) return UniTask.CompletedTask;
            _infoPanel.SetBlock(0);
            return UniTask.CompletedTask;
        }

        private UniTask OnHealthRestoredAsync(BE_HealthRestored restored_)
        {
            if (restored_.Target != BoundCreature) return UniTask.CompletedTask;
            _infoPanel.SetHealth(restored_.Target.CurrentHp, true);
            return UniTask.CompletedTask;
        }

        private UniTask OnPerkAppliedAsync(BE_PerkApplied applied_)
        {
            if (applied_.Target != BoundCreature) return UniTask.CompletedTask;
            _infoPanel.AddPerk(applied_.Perk);
            return UniTask.CompletedTask;
        }

        // 状态层数递减（回合边界未归零）：刷新对应槽位的层数显示
        private UniTask OnPerkDecayedAsync(BE_PerkDecayed decayed_)
        {
            if (decayed_.Target != BoundCreature) return UniTask.CompletedTask;
            _infoPanel.RefreshPerkLayers(decayed_.Perk);
            return UniTask.CompletedTask;
        }

        // 状态移除（回合末衰减归零）：Perk 是被摘下的挂载实例，槽位按状态类型对号删除
        private UniTask OnPerkRemovedAsync(BE_PerkRemoved removed_)
        {
            if (removed_.Target != BoundCreature) return UniTask.CompletedTask;
            _infoPanel.RemovePerk(removed_.Perk);
            return UniTask.CompletedTask;
        }
    }
}
