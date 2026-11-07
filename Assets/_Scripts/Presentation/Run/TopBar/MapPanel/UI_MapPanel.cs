using DG.Tweening;
using UnityEngine;
using UnityEngine.UI;
using _DreamKeep.EngineUtility;

namespace _DreamKeep.Presentation
{
    /// <summary>地图面板：地图视图从上方整屏滑入的开关动画（收起反向，播完才失活）。
    /// 开合动画期间再点忽略；点面板背景（左键）收起，顶栏按钮等效。另持全地图共享的连线点池（UI_MapLine 借还点实例）。</summary>
    public class UI_MapPanel : MonoBehaviour
    {
        private const int DotPoolCapacity = 256; // 共享池缓存上限：超限的归还按池契约直接销毁，上限随地图规模调整

        [Header("开合动画")] [Tooltip("地图视图（滑入滑出）")] [SerializeField]
        private RectTransform _mapView;

        [Tooltip("背景关闭钮（面板直属的全屏透明叶子，首序位；纸面等点击不会冒泡到它，Button 只认左键）")] [SerializeField]
        private Button _backdropButton;

        [Tooltip("视图滑入时长（秒）")] [SerializeField]
        private float _slideDuration = 0.35f;

        private MonoObjectPool<UI_MapDot> _dotPool;
        private Tween _slideTween;
        private UI_MapView _view;
        private bool _refsReady;
        private bool _opening;
        private bool _closing;
        private float _switchEndsAt;
        private Vector2 _restPosition;

        /// <summary>全地图共享的连线点池：出生父级为本面板，实例的父节点与兄弟位由线自己摆。</summary>
        internal MonoObjectPool<UI_MapDot> DotPool
        {
            get
            {
                if (_dotPool == null) _dotPool = new MonoObjectPool<UI_MapDot>(transform, DotPoolCapacity);
                return _dotPool;
            }
        }

        private void Awake()
        {
            EnsureRefs();
            if (_backdropButton) _backdropButton.onClick.AddListener(Hide); // 点背景左键收起：与顶栏按钮等效，动画中 Hide 自身会忽略
        }

        // 容器 Build 可能早于 Awake（装配期面板 inactive）：引用与停靠位一律懒取，避免 HideImmediate 用默认值污染布局
        private void EnsureRefs()
        {
            if (_refsReady) return;
            _refsReady = true;
            _view = GetComponentInChildren<UI_MapView>(true);
            _restPosition = _mapView.anchoredPosition;
        }

        private void OnDestroy() => _slideTween?.Kill();

        // 开合状态由计时器收口（不依赖 DOTween 的 OnComplete：tween 复用/Kill 会丢回调，面板会停在"透明但激活"吃掉点击）
        private void Update()
        {
            if (!_opening && !_closing) return;
            if (Time.unscaledTime < _switchEndsAt) return;
            if (_closing) gameObject.SetActive(false);
            _opening = false;
            _closing = false;
        }

        /// <summary>打开：视图从上方一整屏滑入停靠位；开合动画进行中忽略。</summary>
        public void Show()
        {
            EnsureRefs();
            if (_opening || _closing) return;
            _opening = true;
            _closing = false;
            _switchEndsAt = Time.unscaledTime + _slideDuration + 0.02f;
            gameObject.SetActive(true);
            if (_view) _view.Refresh(); // 每次打开按域状态重绑三态并迁圆圈

            Vector2 from = _restPosition + new Vector2(0f, _mapView.rect.height);
            _mapView.anchoredPosition = from;
            _slideTween?.Kill();
            _slideTween = DOTween.To(() => _mapView.anchoredPosition, value_ => _mapView.anchoredPosition = value_,
                _restPosition, _slideDuration).SetEase(Ease.OutCubic).SetUpdate(true);
        }

        /// <summary>收起：反向滑出，计时到点失活；动画进行中忽略。</summary>
        public void Hide()
        {
            if (!gameObject.activeSelf) return; // 已收起：不起动画（inactive 下 Update 不跑，开合标志会锁死）
            if (_opening || _closing) return;
            _closing = true;
            _opening = false;
            _switchEndsAt = Time.unscaledTime + _slideDuration + 0.02f;

            Vector2 to = _restPosition + new Vector2(0f, _mapView.rect.height);
            _slideTween?.Kill();
            _slideTween = DOTween.To(() => _mapView.anchoredPosition, value_ => _mapView.anchoredPosition = value_,
                to, _slideDuration).SetEase(Ease.InCubic).SetUpdate(true);
        }

        /// <summary>装配期立即隐藏：不起动画、不留透明激活层（顶栏 Construct 调一次）。</summary>
        public void HideImmediate()
        {
            EnsureRefs();
            _slideTween?.Kill();
            _slideTween = null;
            _opening = _closing = false;
            _mapView.anchoredPosition = _restPosition;
            gameObject.SetActive(false);
        }

        public void Toggle()
        {
            if (gameObject.activeSelf) Hide();
            else Show();
        }
    }
}