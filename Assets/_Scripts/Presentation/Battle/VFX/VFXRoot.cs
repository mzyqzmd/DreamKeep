using _DreamKeep.GameBattle;
using _DreamKeep.RuntimeData;
using _DreamKeep.Utility;
using Cysharp.Threading.Tasks;
using UnityEngine;
using VContainer;

namespace _DreamKeep.Presentation
{
    /// <summary>战斗特效中枢：挂在场景 VFX 节点上，按域事件把一次性特效播到目标单位身上——受击（真掉血）出打击特效、
    /// 回血（含零回复）出治疗特效。实例临用挂到单位视图下（随其纵深移动与死亡自毁），播完归位还池；
    /// 这里只决定"谁在什么时候播什么、摆在哪"，通道识别与时长归 UI_OneShotVfx。两个处理器都不阻塞事件链。</summary>
    public sealed class VFXRoot : MonoBehaviour
    {
        [Header("特效预制体")] [SerializeField] private UI_OneShotVfx _hitVfxPrefab;
        [SerializeField] private UI_OneShotVfx _healVfxPrefab;

        [Header("尺寸倍率：挂单位根，不继承骨架自身缩放")] [SerializeField]
        private float _hitScale = 1f;

        [SerializeField] private float _healScale = 1f;

        private MonoObjectPool<UI_OneShotVfx> _hitPool;
        private MonoObjectPool<UI_OneShotVfx> _healPool;
        private UI_ActorRoot _actorRoot;

        private void Awake()
        {
            _hitPool = new MonoObjectPool<UI_OneShotVfx>(transform, 8, onReturn_: ReturnToRoot);
            _healPool = new MonoObjectPool<UI_OneShotVfx>(transform, 8, onReturn_: ReturnToRoot);
        }

        #region 装配：注入事件门面与单位视图登记表

        [Inject]
        public void Construct(BattleEventDispatcher dispatcher_, UI_ActorRoot actorRoot_)
        {
            _actorRoot = actorRoot_;
            dispatcher_.Register<BE_DamageDealt>(OnDamageDealtAsync);
            dispatcher_.Register<BE_HealthRestored>(OnHealthRestoredAsync);
        }

        #endregion

        // 受击：格挡全吸收不播；致死一击照样播（与伤害数字口径一致）
        private UniTask OnDamageDealtAsync(BE_DamageDealt dealt_)
        {
            if (dealt_.Amount <= dealt_.Absorbed) return UniTask.CompletedTask;
            Play(_hitPool, _hitVfxPrefab, dealt_.Target, EUiAnchor.MiddleCenter, _hitScale);
            return UniTask.CompletedTask;
        }

        // 回血：收到事件就播，回复量为 0 也播
        private UniTask OnHealthRestoredAsync(BE_HealthRestored restored_)
        {
            Play(_healPool, _healVfxPrefab, restored_.Target, EUiAnchor.BottomCenter, _healScale);
            return UniTask.CompletedTask;
        }

        // 取池 → 挂到单位视图下（随其移动与销毁）→ 按骨架锚点写世界位姿 → 播一次后还池
        private void Play(MonoObjectPool<UI_OneShotVfx> pool_, UI_OneShotVfx prefab_, Creature target_,
            EUiAnchor anchor_, float scale_)
        {
            if (!_actorRoot.TryGetView(target_, out UI_Actor view)) return;
            UI_OneShotVfx vfx = pool_.Get(prefab_);
            Transform vfxTransform = vfx.transform;
            vfxTransform.SetParent(view.transform, false);
            vfxTransform.SetAsLastSibling();
            vfxTransform.localScale = Vector3.one * scale_;
            vfxTransform.position = UiGeometry.AnchorPoint((RectTransform)view.Skeleton.transform, anchor_);
            vfx.Play(() => pool_.Release(vfx));
        }

        // 还池前归位到本组件名下：空闲实例不留在可能被销毁的单位下，下次取出再改挂
        private void ReturnToRoot(Component instance_) => instance_.transform.SetParent(transform, false);
    }
}
