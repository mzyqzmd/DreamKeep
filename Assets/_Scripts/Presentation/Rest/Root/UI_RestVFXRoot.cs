using Cysharp.Threading.Tasks;
using UnityEngine;
using _DreamKeep.CommonPresentation;
using _DreamKeep.EngineUtility;

namespace _DreamKeep.Presentation.Root
{
    /// <summary>休息段特效编排根：只决定"何时播、摆在哪"，实例池/预制体/时长归 VfxFactory 与 UI_OneShotVfx。
    /// 落点取锚点矩形中心（与 pivot 无关）：锻造默认画布中心，休息默认营火高亮；两者都无需在场景里重新接线。
    /// 父节点仍是特效层（画布内），落点只是世界坐标，所以父层被误设成 0 缩放时这里修一次。</summary>
    public sealed class UI_RestVFXRoot : MonoBehaviour
    {
        [Header("落点")] [SerializeField] private Transform _playVFXParent;

        [Header("倍率")] [SerializeField] private float _restSmokeScale = 1f;

        private VfxFactory _vfxFactory;
        private UI_RestBackgroundRoot _restBackground;

        /// <summary>运行时交底：工厂由父容器注册，跨场景无法用 Inspector 拖引用。</summary>
        public void Initialize(VfxFactory vfxFactory_) =>
            _vfxFactory = vfxFactory_;

        #region 对外播放：播完才返回，供段入口按序回血/收段

        public UniTask PlayRestSmokeAsync()
        {
            if (!_vfxFactory) return UniTask.CompletedTask;
            UniTaskCompletionSource done = new();
            _vfxFactory.PlayRestSmoke(_playVFXParent,
                UiGeometry.AnchorPoint((RectTransform)_playVFXParent, EUiAnchor.MiddleCenter), _restSmokeScale,
                () => done.TrySetResult());
            return done.Task;
        }

        #endregion
    }
}