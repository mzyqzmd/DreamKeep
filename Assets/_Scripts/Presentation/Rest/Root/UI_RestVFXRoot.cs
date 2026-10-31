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
        [SerializeField] private Transform _forgeAnchor;
        [SerializeField] private Transform _restAnchor;

        [Header("倍率")] [SerializeField] private float _restSmokeScale = 1f;
        [SerializeField] private float _smithForgeScale = 1f;

        private VfxFactory _vfxFactory;
        private UI_RestBackgroundRoot _restBackground;

        /// <summary>运行时交底：工厂由父容器注册，跨场景无法用 Inspector 拖引用。</summary>
        public void Initialize(VfxFactory vfxFactory_)
        {
            _vfxFactory = vfxFactory_;
            EnsureEffectLayerUsable();
        }

        #region 对外播放：播完才返回，供段入口按序回血/收段

        public UniTask PlayRestSmokeAsync()
        {
            if (!_vfxFactory) return UniTask.CompletedTask;
            UniTaskCompletionSource done = new();
            _vfxFactory.PlayRestSmoke(_playVFXParent, AnchorWorld(RestAnchor()), _restSmokeScale,
                () => done.TrySetResult());
            return done.Task;
        }

        public UniTask PlaySmithForgeAsync()
        {
            if (!_vfxFactory) return UniTask.CompletedTask;
            UniTaskCompletionSource done = new();
            _vfxFactory.PlaySmithForge(_playVFXParent, AnchorWorld(_forgeAnchor), _smithForgeScale,
                () => done.TrySetResult());
            return done.Task;
        }

        #endregion

        // 营火锚点：优先接线值，空则从场景背景根自愈取；都取不到就落到特效层中心
        private Transform RestAnchor()
        {
            if (_restAnchor) return _restAnchor;
            if (!_restBackground) _restBackground = FindObjectOfType<UI_RestBackgroundRoot>(true);
            return _restBackground ? _restBackground.FireAnchor : null;
        }

        // 落点＝锚点矩形中心（营火有真实矩形）；没有锚点时取特效层正对镜头的中心
        private Vector3 AnchorWorld(Transform anchor_)
        {
            if (anchor_ is RectTransform anchorRect) return UiGeometry.AnchorPoint(anchorRect, EUiAnchor.MiddleCenter);
            return LayerCenterWorld();
        }

        // 屏幕中心的世界坐标：走相机视口中心，不依赖特效层 RectTransform 是否已被 Canvas 驱动出尺寸
        private Vector3 LayerCenterWorld()
        {
            if (!_playVFXParent) return transform.position;
            Canvas canvas = _playVFXParent.GetComponentInParent<Canvas>(true);
            Camera camera = canvas && canvas.worldCamera ? canvas.worldCamera : Camera.main;
            if (camera)
                return camera.ViewportToWorldPoint(new Vector3(0.5f, 0.5f, canvas ? canvas.planeDistance : 10f));
            return _playVFXParent.position;
        }

        // 专用特效层被误设成 0 缩放时 UI 与粒子都会变成 0 尺寸：这里只修这一层，不碰其它节点
        private void EnsureEffectLayerUsable()
        {
            Transform layer = _playVFXParent ? _playVFXParent : transform;
            Vector3 scale = layer.lossyScale;
            if (scale.x <= 0f || scale.y <= 0f) layer.localScale = Vector3.one;
        }
    }
}
