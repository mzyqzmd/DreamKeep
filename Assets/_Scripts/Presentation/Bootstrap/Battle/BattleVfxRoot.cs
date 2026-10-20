using _DreamKeep.CommonPresentation;
using _DreamKeep.GameBattle;
using _DreamKeep.RuntimeData;
using _DreamKeep.EngineUtility;
using Cysharp.Threading.Tasks;
using UnityEngine;
using VContainer;

namespace _DreamKeep.Presentation
{
    /// <summary>战斗特效编排：挂在场景 VFX 节点上，只决定"谁在什么时候播什么、摆在哪"——
    /// 订阅受击（真掉血）与回血（含零回复）事件，算出目标单位视图与骨架锚点后交给通用特效工厂 VfxFactory；
    /// 特效预制体、实例池、通道识别与时长都归工厂/播放器。两个处理器都不阻塞事件链。</summary>
    public sealed class BattleVfxRoot : MonoBehaviour
    {
        [Header("尺寸倍率：挂单位根，不继承骨架自身缩放")] [SerializeField]
        private float _hitScale = 1f;

        [SerializeField] private float _healScale = 1f;

        private UI_ActorRoot _actorRoot;
        private VfxFactory _vfxFactory;

        #region 装配：注入事件门面、单位视图登记表与通用特效工厂

        [Inject]
        public void Construct(BattleEventDispatcher dispatcher_, UI_ActorRoot actorRoot_, VfxFactory vfxFactory_)
        {
            _actorRoot = actorRoot_;
            _vfxFactory = vfxFactory_;
            dispatcher_.Register<BE_DamageDealt>(OnDamageDealtAsync);
            dispatcher_.Register<BE_HealthRestored>(OnHealthRestoredAsync);
        }

        #endregion

        // 受击：格挡全吸收不播；致死一击照样播（与伤害数字口径一致）
        private UniTask OnDamageDealtAsync(BE_DamageDealt dealt_)
        {
            if (dealt_.Amount <= dealt_.Absorbed) return UniTask.CompletedTask;
            if (TryGetMount(dealt_.Target, EUiAnchor.MiddleCenter, out Transform parent, out Vector3 position))
                _vfxFactory.PlayHit(parent, position, _hitScale);
            return UniTask.CompletedTask;
        }

        // 回血：收到事件就播，回复量为 0 也播
        private UniTask OnHealthRestoredAsync(BE_HealthRestored restored_)
        {
            if (TryGetMount(restored_.Target, EUiAnchor.BottomCenter, out Transform parent, out Vector3 position))
                _vfxFactory.PlayHeal(parent, position, _healScale);
            return UniTask.CompletedTask;
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
