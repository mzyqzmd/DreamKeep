using System;
using DG.Tweening;
using TMPro;
using UnityEngine;

namespace _DreamKeep.CommonPresentation
{
    /// <summary>数字飘字：一次伤害/治疗/格挡提示的单条表现实例，只负责"写文字、按形态播动作、播完通知宿主回收"；
    /// 实例取还与挂载归 PopRoot（挂到单位视图下）。Rise = 向上缓慢飘升并渐隐，Toss = 斜上抛出后受重力下坠。
    /// 位移单位统一为画布像素（anchoredPosition），形态与时长参数全部留在预制体 Inspector。</summary>
    public sealed class UI_NumberPopTip : MonoBehaviour
    {
        [Header("基础配置")] [SerializeField] private CanvasGroup _canvasGroup;
        [SerializeField] private TextMeshProUGUI _numberText;

        [Header("入弹")] [Tooltip("入场缩放的起点倍率")] [SerializeField]
        private float _popScaleFrom = 0.6f;

        [SerializeField] private float _popDuration = 0.12f;

        [Header("上飘（Rise）")] [Tooltip("飘升距离（画布像素）")] [SerializeField]
        private float _riseDistancePx = 130f;

        [SerializeField] private float _riseDuration = 1f;

        [Header("斜抛（Toss）")] [Tooltip("初始速度（画布像素/秒）：y 为正即向上抛出")] [SerializeField]
        private Vector2 _tossVelocityPx = new(220f, 620f);

        [Tooltip("重力加速度（画布像素/秒²），向下为负")] [SerializeField]
        private float _tossGravityPx = 1800f;

        [SerializeField] private float _tossDuration = 1.1f;
        [Tooltip("抛出时绕自身旋转的总角度（度），0 即不转")] [SerializeField]
        private float _tossSpinDeg = 25f;

        [Tooltip("横向初速与旋转方向每帧随机左右翻转")] [SerializeField]
        private bool _tossRandomFlip = true;

        private RectTransform _rect;
        private ENumberPopTipType _type;
        private Vector2 _velocity;
        private float _elapsed;
        private float _spinSpeed;
        private bool _finished;
        private Tween _riseTween;
        private Tween _fadeTween;
        private Tween _popTween;
        private Action _onFinished;

        private void Awake() => _rect = (RectTransform)transform;

        // 还池失活时兜底收掉在跑的补间：下一次 Play 会重写全部状态
        private void OnDisable() => KillTweens();

        // Toss 的位移在 Update 里做重力积分（起点与初速任意、轨迹天然正确）；Rise 走补间
        private void Update()
        {
            if (_type != ENumberPopTipType.Toss || _finished) return;
            float deltaTime = Time.deltaTime;
            _elapsed += deltaTime;
            _velocity.y -= _tossGravityPx * deltaTime;
            _rect.anchoredPosition += _velocity * deltaTime;
            _rect.Rotate(0f, 0f, _spinSpeed * deltaTime);
            if (_elapsed >= _tossDuration) Finish();
        }

        #region 对外入口：播放一条飘字与还池复位

        /// <summary>播放一条飘字：text_ 为显示文字，color_ 为文字颜色，canvasPosition_ 为已换算到本画布平面的世界点，
        /// onFinished_ 在播完时回调一次（宿主用它还池）。</summary>
        public void Play(string text_, Color color_, ENumberPopTipType type_, Vector3 canvasPosition_, Action onFinished_)
        {
            KillTweens();
            _finished = false;
            _onFinished = onFinished_;
            _type = type_;

            _numberText.text = text_;
            _numberText.color = color_;

            _rect.position = canvasPosition_;
            _rect.localRotation = Quaternion.identity;
            _rect.localScale = Vector3.one * _popScaleFrom;
            _canvasGroup.alpha = 1f;

            _popTween = _rect.DOScale(Vector3.one, _popDuration).SetEase(Ease.OutBack);
            if (type_ == ENumberPopTipType.Rise) PlayRise();
            else PlayToss();
        }

        /// <summary>还池复位：杀补间、清回调；下一次 Play 会重写文字、颜色、姿态与计时。</summary>
        public void ResetView()
        {
            KillTweens();
            _onFinished = null;
            _finished = true;
        }

        #endregion

        // 上飘：位置与透明度同时补间，靠 OutQuad/InQuad 让"先飘后隐"的比重更大
        private void PlayRise()
        {
            Vector2 target = _rect.anchoredPosition + new Vector2(0f, _riseDistancePx);
            _riseTween = DOTween.To(() => _rect.anchoredPosition, value_ => _rect.anchoredPosition = value_, target,
                _riseDuration).SetEase(Ease.OutQuad);
            _fadeTween = DOTween.To(() => _canvasGroup.alpha, value_ => _canvasGroup.alpha = value_, 0f, _riseDuration)
                .SetEase(Ease.InQuad).OnComplete(Finish);
        }

        // 斜抛：横向初速与自转方向同步随机翻转，纵向初速向上、每帧被重力拉回；位移积分在 Update
        private void PlayToss()
        {
            float direction = _tossRandomFlip && UnityEngine.Random.value < 0.5f ? -1f : 1f;
            _velocity = new Vector2(_tossVelocityPx.x * direction, _tossVelocityPx.y);
            _elapsed = 0f;
            _spinSpeed = -direction * _tossSpinDeg;
            _fadeTween = DOTween.To(() => _canvasGroup.alpha, value_ => _canvasGroup.alpha = value_, 0f, _tossDuration)
                .SetEase(Ease.InQuad);
        }

        // 播完只放行一次：宿主回调会立即还池，重复触发会二次归还
        private void Finish()
        {
            if (_finished) return;
            _finished = true;
            _onFinished?.Invoke();
        }

        private void KillTweens()
        {
            _riseTween?.Kill();
            _fadeTween?.Kill();
            _popTween?.Kill();
            _riseTween = _fadeTween = _popTween = null;
        }
    }
}
