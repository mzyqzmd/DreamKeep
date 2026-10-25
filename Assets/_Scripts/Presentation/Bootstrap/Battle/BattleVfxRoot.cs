using _DreamKeep.CommonPresentation;
using _DreamKeep.GameBattle;
using _DreamKeep.RuntimeData;
using _DreamKeep.EngineUtility;
using UnityEngine;
using VContainer;

namespace _DreamKeep.Presentation
{
    /// <summary>战斗特效编排：挂在场景 VFX 节点上，只决定"谁在什么时候播什么、摆在哪"——
    /// 订阅受击（真掉血）、回血（含零回复）、格挡获得与状态施加事件，算出目标单位视图与骨架锚点后交给通用特效工厂
    /// VfxFactory；特效预制体、实例池、通道识别与时长都归工厂/播放器。处理器一律不阻塞事件链。</summary>
    public sealed class BattleVfxRoot : MonoBehaviour
    {
        [Header("尺寸倍率：挂单位根，不继承骨架自身缩放")] [SerializeField]
        private float _hitScale = 1f;

        [SerializeField] private float _healScale = 1f;
        [SerializeField] private float _blockScale = 1f;
        [SerializeField] private float _perkScale = 1f;

        private UI_ActorRoot _actorRoot;
        private VfxFactory _vfxFactory;

        #region 装配：注入事件门面、单位视图登记表与通用特效工厂

        [Inject]
        public void Construct(BattleEventDispatcher dispatcher_, UI_ActorRoot actorRoot_, VfxFactory vfxFactory_)
        {
            _actorRoot = actorRoot_;
            _vfxFactory = vfxFactory_;
            dispatcher_.Register<BE_DamageDealt>(OnDamageDealt);
            dispatcher_.Register<BE_HealthRestored>(OnHealthRestored);
            dispatcher_.Register<BE_BlockGained>(OnBlockGained);
            dispatcher_.Register<BE_PerkApplied>(OnPerkApplied);
        }

        #endregion

        // 受击：格挡全吸收不播；致死一击照样播（与伤害数字口径一致）
        private void OnDamageDealt(BE_DamageDealt dealt_)
        {
            if (dealt_.Amount <= dealt_.Absorbed) return;
            if (TryGetMount(dealt_.Target, EUiAnchor.BottomCenter, out Transform parent, out Vector3 position))
                _vfxFactory.PlayHit(parent, position, _hitScale);
        }

        // 回血：收到事件就播，回复量为 0 也播
        private void OnHealthRestored(BE_HealthRestored restored_)
        {
            if (TryGetMount(restored_.Target, EUiAnchor.BottomCenter, out Transform parent, out Vector3 position))
                _vfxFactory.PlayHeal(parent, position, _healScale);
        }

        // 格挡：只认真正加上的格挡（0 或负数由域侧挡掉，这里不判）；没接预制体时工厂自己跳过
        private void OnBlockGained(BE_BlockGained gained_)
        {
            if (TryGetMount(gained_.Target, EUiAnchor.BottomCenter, out Transform parent, out Vector3 position))
                _vfxFactory.PlayBlock(parent, position, _blockScale);
        }

        // 施加状态：落在目标骨架中心（相对锚点的上抬量归特效自己），内容随事件交出的状态运行时实例
        private void OnPerkApplied(BE_PerkApplied applied_)
        {
            if (TryGetMount(applied_.Target, EUiAnchor.BottomCenter, out Transform parent, out Vector3 position))
                _vfxFactory.PlayPerk(parent, position, _perkScale, applied_.Perk);
        }

        // 查单位视图并按骨架锚点算世界位姿；视图缺失时不播
        private bool TryGetMount(Creature target_, EUiAnchor anchor_, out Transform parent_, out Vector3 position_)
        {
            parent_ = null;
            position_ = default;
            if (!_actorRoot.TryGetView(target_, out UI_Actor view)) return false;
            parent_ = view.transform;
            position_ = UiGeometry.AnchorPoint((RectTransform)view.Skeleton.transform, anchor_);
            return true;
        }
    }
}
