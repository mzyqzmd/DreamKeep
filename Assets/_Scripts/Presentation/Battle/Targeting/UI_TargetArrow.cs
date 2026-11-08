using System.Collections.Generic;
using DG.Tweening;
using _DreamKeep.EngineUtility;
using UnityEngine;
using UnityEngine.UI;

namespace _DreamKeep.Presentation
{
    /// <summary>目标箭头：从卡牌中心指向指针的瞄准指示器——由粗到细的箭头段沿一条带轻微弧度的曲线排布，头部贴指针并转向目标。
    /// 弧度按指针方向相对"起点正上方"的偏角连续变化（角度越大弯得越多，正对正上方是一条直线，跨过基准线不翻边）；
    /// 指向合法目标时整支变红、头部弹一下。它纯表现，不认识卡牌与战斗单位，只回答"从哪画到哪、有没有指向合法目标"。</summary>
    public sealed class UI_TargetArrow : MonoBehaviour
    {
        [Tooltip("段容器：它的子节点即箭头段，场景里按兄弟顺序从尾部段（最小）排到头部段（最大），脚本内部统一成头部端到尾部端")] [SerializeField]
        private RectTransform _segmentRoot;

        [Tooltip("箭头头部：贴指针并转向目标，段的排布从它之后让开一段距离再开始")] [SerializeField]
        private RectTransform _head;

        [Tooltip("最大弧高系数：指针偏离起点正上方达到满角度时，弧高 = 起终点距离 × 该系数；偏得越正弧度越小，正对正上方是一条直线")] [SerializeField]
        private float _bendFactor = 0.9f;

        [Tooltip("满角度（度）：指针偏离起点正上方达到该角度时弧度到达最大；角度越小弧度按比例越小")] [SerializeField]
        private float _bendFullAngleDeg = 60f;

        [Tooltip("头部让位系数：1 即让最靠头部那一节的近边正好贴住头部边缘；调小往头部底下压、调大往尾部退（重新留出空白）")] [SerializeField]
        private float _headGapFactor = 1f;

        [Tooltip("指向合法目标时整支箭头的颜色：各段与头部一起变色，未命中时回到各自本来的颜色")] [SerializeField]
        private Color _targetColor = Color.red;

        [Header("命中脉冲")] [Tooltip("头部在命中的那一瞬间的放大比例：0.3 即最多放大 30%，指向期间只弹这一次")] [SerializeField]
        private float _headPunchScale = 0.2f;

        [Tooltip("头部放大到峰值用的时长（秒）：极短，起来要快")] [SerializeField]
        private float _headPunchUpDuration = 0.1f;

        [Tooltip("头部从峰值回落到基准缩放的时长（秒）：减速轻着地")] [SerializeField]
        private float _headPunchDownDuration = 0.12f;

        private const float Epsilon = 0.0001f;
        private const float MaxStartT = 0.95f; // 首段让位占整条曲线的比例上限：距离比头部还短时不再退让

        private readonly List<RectTransform> _segments = new();
        private readonly List<Vector3> _segmentScales = new();
        private readonly List<Graphic> _graphics = new();
        private readonly List<Color> _baseColors = new();

        private Vector3 _headBaseScale;
        private Tween _headPunchTween;
        private bool _onTarget;

        private void Awake()
        {
            // 射线一律关掉（箭头画在最上层，开着会挡住手牌的悬停射线），顺带缓存图形与本来的颜色，供命中高亮写回
            foreach (Graphic graphic in GetComponentsInChildren<Graphic>(true))
            {
                _graphics.Add(graphic);
                _baseColors.Add(graphic.color);
            }

            // 段反向缓存：场景里从尾部最小排到头部最大，这里统一成"首个最大（头部端）、末个最小（尾部端）"，
            // 各段缩放取场景里摆好的初值
            for (int i = _segmentRoot.childCount - 1; i >= 0; i--)
            {
                RectTransform segment = (RectTransform)_segmentRoot.GetChild(i);
                _segments.Add(segment);
                _segmentScales.Add(segment.localScale);
            }

            // 头部基准缩放：脉冲的起点与终点都以它为准
            _headBaseScale = _head.localScale;
            gameObject.SetActive(false);
        }

        private void OnDestroy() => _headPunchTween?.Kill();

        #region 对外暴露的显示与收起：起点为卡牌中心、终点为指针，可逐帧重复调用

        /// <summary>按当前起终点重排箭头：段沿带轻微弧度的曲线从头端到尾端均布、每段朝向当地切线，头部贴终点并转向目标；
        /// 弧度由指针相对起点正上方的偏角决定，onTarget_ 为真时整支变红、且只在命中状态由无到有的那一帧弹一下头部。</summary>
        public void Show(Vector3 fromWorld_, Vector3 toWorld_, bool onTarget_)
        {
            gameObject.SetActive(true);

            Vector3 aim = toWorld_ - fromWorld_;
            float length = aim.magnitude;
            // 指针方向相对"起点正上方"的带符号夹角（负在左、正在右）：角度越大弧度越大，正对正上方时弧度归零，
            // 跨过基准线是连续过渡，不会像固定侧向那样瞬间翻边
            float angle = Mathf.Atan2(aim.x, aim.y);
            float ratio = Mathf.Min(Mathf.Abs(angle) * Mathf.Rad2Deg / Mathf.Max(_bendFullAngleDeg, Epsilon), 1f);
            // 控制点从中点朝指针所在侧的**反侧**鼓出（指针在左则向右鼓、在右则向左鼓）：弧高 = 弦长 × 系数 × 角度占比，
            // 方向恒为世界水平，圆心在竖直基准线上
            Vector3 control = (fromWorld_ + toWorld_) * 0.5f -
                              new Vector3(Mathf.Sign(angle), 0f, 0f) * (length * _bendFactor * ratio);

            _head.position = UiGeometry.KeepPlane(toWorld_, _head.position.z);
            _head.rotation = UiGeometry.UpRotation(-Curve.Tangent(toWorld_, control, fromWorld_, 0f));
            // 命中脉冲：只在"未命中 → 命中"那一刻弹一次，指着目标期间不再动
            if (onTarget_ && !_onTarget)
                UiTween.PlayPunch(ref _headPunchTween, _head, _headBaseScale, _headPunchScale,
                    _headPunchUpDuration, _headPunchDownDuration);

            _onTarget = onTarget_;
            // 命中高亮：指向合法目标时整支写目标色，否则写回各图形本来的颜色；同色短路，避免每帧白标脏画布
            for (int i = 0; i < _graphics.Count; i++)
            {
                Color color = onTarget_ ? _targetColor : _baseColors[i];
                if (_graphics[i].color != color) _graphics[i].color = color;
            }

            int last = _segments.Count - 1;
            // 让位取父级 lossyScale × 各自基准缩放（头部脉冲时不让段跟着抖、画布缩放变化仍自适应）：让"最靠头部那一节的近边"
            // 落在头部边缘（头部半高 × 系数）上，而不是只把它的中心放在那里——只对中心让位时，这一节自身还差半个身位没铺到
            // 头部，头与段之间就会空出一截，空多少还取决于落在头部端的这一节有多大
            float headReach = _head.rect.height * _head.parent.lossyScale.y * _headBaseScale.y * 0.5f * _headGapFactor;
            float firstReach = last < 0 ? 0f
                : _segments[0].rect.height * _head.parent.lossyScale.y * _segmentScales[0].y * 0.5f;
            float start = length < Epsilon ? 0f : Mathf.Clamp(Mathf.Max(0f, headReach - firstReach) / length, 0f, MaxStartT);
            for (int i = 0; i <= last; i++)
            {
                float t = last <= 0 ? start : Mathf.Lerp(start, 1f, i / (float)last);
                RectTransform segment = _segments[i];
                segment.position =
                    UiGeometry.KeepPlane(Curve.Point(toWorld_, control, fromWorld_, t), segment.position.z);
                segment.rotation = UiGeometry.UpRotation(-Curve.Tangent(toWorld_, control, fromWorld_, t));
                segment.localScale = _segmentScales[i];
            }
        }

        /// <summary>收起整支箭头：脉冲进行中被收起时跳过剩余部分直接复原，下次 Show 时再展开（届时按当时的命中状态写回颜色、重新判断要不要弹）。</summary>
        public void Hide()
        {
            _headPunchTween?.Complete(); // 脉冲终点恒为基准缩放，故"跳到终点"就是复原；从未弹过或已结束时它是空操作
            _onTarget = false;
            gameObject.SetActive(false);
        }

        #endregion
    }
}