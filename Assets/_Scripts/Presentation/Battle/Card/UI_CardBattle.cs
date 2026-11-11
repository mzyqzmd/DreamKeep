using System.Collections.Generic;
using _DreamKeep.GameBattle;
using _DreamKeep.RuntimeData;
using _DreamKeep.EngineUtility;
using _DreamKeep.CommonPresentation;
using Cysharp.Threading.Tasks;
using DG.Tweening;
using UnityEngine;
using UnityEngine.EventSystems;

namespace _DreamKeep.Presentation
{
    /// <summary>战斗卡本体：牌面由同节点的选择器按卡类型切换，本类只把绑定与不可打外观转发给当前那张。
    /// 位置 / 朝向 / 所在层、按下拖动松手、悬停进出也全在这里；牌面只管长什么样，展示卡直接摆牌面、不包这一层，于是天然收不到指针事件。
    /// 位置与朝向一律是**世界姿态**：区域（卡层）只负责挂载与遮挡，槽位由区域按自己的点换算好后下发，本卡只管写到 transform 上；
    /// 换层走 MoveToLayer 唯一通道且一律保世界位置，故各层矩形怎么摆都不会错位。
    /// 能不能抓一律问宿主闸门（非我方回合、还在入场飞行中、已让出牌区等结算的牌都抓不起来），本卡只报事件不判规则；
    /// 悬停不受可打限制：不能打的牌照样抬起来给你看，只是按下去抓不动、拖不出去；
    /// 按下后走哪种交互由本卡自己的目标类型回答：AnyEnemy 卡进瞄准模式——卡定住不动，拖动越过启动阈值后交给宿主去驱动目标箭头，
    /// 其余卡以阻尼插值跟随指针（鼠标停下也会自己滑到位）；
    /// 动画一概不参与判据，滑行合拢中的牌照样能按下即抓（抓起来先 Kill 掉自己的补间）。
    /// 悬停的偏移与放大由同节点两个组件执行（均为被驱动模式，本类只在进出时交接）：位置取"卡自己的槽位沿区域给的方向偏移一段"
    /// （原位偏移、不跟随指针）、朝向取区域给的基准朝向（回正），同时放大；还原值同样由区域按槽位真源给出，
    /// 本卡不记进入前的瞬时值，补间途中进出也不会写回半截姿态。
    /// 每类姿态同一时刻只有一个写者：悬停进出归两个组件（进入前先杀本卡的两条通道），重排/让位/回位仍走本卡的两条通道；
    /// 本卡的两条通道按"已声明的目标"判要不要重开（同目标续用、换目标杀旧）——比的是目标而不是滞后的当前姿态，
    /// 故同一帧里被连续声明两次（例如先让位、随即被驳回退回）也以第二次为准，不会把命令静默吞掉。
    /// 词条说明交给同节点的气泡发起器（外部驱动）：内容随绑定写入，显隐由手牌区按自己的悬停裁决调它。</summary>
    [RequireComponent(typeof(UI_HoverTip))]
    [RequireComponent(typeof(UI_CardFaceSelector))]
    [RequireComponent(typeof(UI_HoverOffset))]
    [RequireComponent(typeof(UI_HoverScaler))]
    public sealed class UI_CardBattle : MonoBehaviour, IPointerDownHandler, IDragHandler, IPointerUpHandler,
        IPointerClickHandler
    {
        [Tooltip("拖拽跟手高度（画布像素）：拖拽时让卡锚点（下沿中点）上方该距离的点与指针重合，指针便停在卡内这个高度")] [SerializeField]
        private float _anchorLiftPx = 120f;

        [Tooltip("瞄准模式的启动阈值（屏幕像素）：按住 AnyEnemy 卡移动超过该距离才把它定住并显示目标箭头，纯点击不触发")] [SerializeField]
        private float _aimThresholdPx = 12f;

        [Tooltip("拖拽跟随的时间常数（秒）：越小越跟手、越大拖尾越明显；鼠标停下时卡会自己滑到指针锚点位")] [SerializeField]
        private float _dragFollowDamp = 0.15f;

        private UI_CardFaceSelector _faces; // 四张内嵌牌面的持有者：当前是哪张由它回答
        private UI_HoverTip _hoverTip;
        private UI_HoverOffset _hoverOffset; // 悬停偏移与缩放的通道写者（被驱动模式）：本卡只在进出时交接
        private UI_HoverScaler _hoverScaler;
        private RectTransform _rect;
        private Tween _moveTween;
        private Tween _scaleTween;

        // 词条说明的内容（复用列表）：Bind 时按牌面的逐行文案重填一次，预览变化时经同源渲染重填，再递给气泡发起器
        private readonly List<(string Name, string Description)> _affixTips = new();

        // 数值预览台账（复用列表）：上一次渲染的各伤害/格挡词条最终值——每帧对账先算进草稿，变了才接受并重建
        private readonly List<int> _previewShown = new();
        private readonly List<int> _previewDraft = new();

        // 两条补间通道"已声明的目标"：判"要不要重开补间"比它、不比滞后的当前姿态——补间刚建立、
        // transform 还没挪动的同一帧里，当前姿态会把"我马上要离开的位置"误判成"已就位"
        private Vector3 _moveTargetPosition;
        private Quaternion _moveTargetRotation;
        private float _scaleTarget;

        private bool _dragging;
        private bool _hovered;
        private Vector2 _pressScreenPosition;

        // "已就位"的判定阈值：重排据此跳过空转补间（位置差不足 0.01 世界单位约合 1.4 画布像素，肉眼不可见）
        private const float PoseEpsilon = 0.01f;
        private const float AngleEpsilon = 0.05f;
        private const float ScaleEpsilon = 0.001f;

        public UI_HandView Host { get; private set; }

        public CardModel Card => _faces.Current.Card;

        /// <summary>当前世界位置（卡根 pivot＝卡下沿中点）：拖拽跟随与落点判定都以这个锚点起算。</summary>
        public Vector3 WorldPosition => _rect.position;

        /// <summary>当前世界朝向：起飞抖动等需要原位重写姿态的场合与 WorldPosition 配对使用。</summary>
        public Quaternion WorldRotation => _rect.rotation;

        /// <summary>卡牌矩形的世界中心：目标箭头的起点，卡对齐、缩放、旋转时自动跟随。</summary>
        public Vector3 WorldCenter => UiGeometry.AnchorPoint(_rect, EUiAnchor.MiddleCenter);

        /// <summary>拖拽跟手高度（画布像素）：拖拽时由所在区域换算成"指针锚点位"，让指针停在卡内该高度。</summary>
        public float AnchorLiftPx => _anchorLiftPx;

        /// <summary>悬停进出补间时长（秒）：区域的悬停避让与它对齐，避开"牌已移邻牌才动"的错位感。</summary>
        public float HoverDuration => _hoverOffset.Duration;

        /// <summary>悬停偏移量（手牌区画布像素）：区域按它把槽位换算成悬停位姿，进入悬停前也要用它算偏移后的范围。</summary>
        public Vector2 HoverOffsetPx => _hoverOffset.OffsetPx;

        /// <summary>词条说明的气泡发起器：显隐由手牌区在它裁决出"该显示"的时刻调用，内容已随 Bind 写入。</summary>
        public UI_HoverTip HoverTip => _hoverTip;

        // 位置与朝向是否已等于目标姿态：只作"不在飞时不起空转补间"的判据，故收回本类内部（在飞的判据比已声明的目标，见 MoveToAsync）
        private bool IsAtPose(Vector3 worldPosition_, Quaternion worldRotation_) =>
            (_rect.position - worldPosition_).sqrMagnitude < PoseEpsilon * PoseEpsilon &&
            Quaternion.Angle(_rect.rotation, worldRotation_) < AngleEpsilon;

        // 缩放是否已等于目标：同上
        private bool IsAtScale(float scale_) => Mathf.Abs(transform.localScale.x - scale_) < ScaleEpsilon;

        private void Awake()
        {
            _rect = (RectTransform)transform;
            _hoverTip = GetComponent<UI_HoverTip>();
            _faces = GetComponent<UI_CardFaceSelector>();
            _hoverOffset = GetComponent<UI_HoverOffset>();
            _hoverScaler = GetComponent<UI_HoverScaler>();
        }

        // 拖拽跟随：非瞄准卡每帧向"锚点上方该距离的点落在指针处"的目标指数插值趋近——鼠标停下卡也会自己滑到位；
        // 瞄准卡（AnyEnemy）定住不跟手，只由 DragAim 上报阈值与指针；牌已离手（拖拽途中被弃置 / 结算中）则让位给后续飞行
        private void Update()
        {
            if (!_dragging || Card.IsAnyEnemyTarget || !Host.IsInRow(this)) return;
            _rect.position = Vector3.Lerp(_rect.position,
                Host.PointerAnchorPosition(this), 1f - Mathf.Exp(-Time.deltaTime / _dragFollowDamp));
        }

        // 拖动中被禁用（回收 / 关闭）时直接落回槽位，不停在半途；悬停状态一并清掉，防实例复用后 EnterHover 被早退挡掉
        // （悬着的词条气泡由同节点的气泡发起器自己的 OnDisable 收掉）
        private void OnDisable()
        {
            ClearHover();
            if (!_dragging) return;
            _dragging = false;
            Host.OnCardDragCancelled(this);
        }

        public void SetHost(UI_HandView host_) => Host = host_;

        #region 选脸与绑定：按卡类型切到对应牌面，再把写入转发给当前那张

        // 切脸：目标脸与当前脸相同就一个字都不写（同类型重绑零 SetActive 抖动）；某个类型漏接引用时报错跳过，不静默半成品
        public void Bind(CardModel card_)
        {
            UI_CardFace face = _faces.Select(card_);
            face.Bind(card_);
            // 预览台账随卡重置：防池化实例把上一张卡的修正值串到新卡（手牌区下一帧的预览对账会重填）
            _previewShown.Clear();
            _previewDraft.Clear();
            // 词条说明随绑定写一次（纯文本——此时牌面门面刚清空，渲染器退化原值；下一帧预览对账换成带修正值的版本）：
            // 只认带出词条的行（其余行由牌面自己陈列），整列为空时气泡不弹
            _faces.Current.CollectTipLines(_affixTips);
            _hoverTip.SetContents(_affixTips);
        }

        public void SetPlayableLook(bool playable_) => _faces.Current.SetPlayableLook(playable_);

        #endregion

        #region 数值预览：每帧差分刷新卡面与气泡

        /// <summary>刷新数值预览（手牌区对账调用）：任一伤害/格挡词条的最终值变化才重建卡面与气泡，未变零字符串构建。
        /// 目标集合按"各目标的最终值是否一致"折叠求值，多目标卡在全体一致时也能显示准确数字。</summary>
        public void RefreshPreview(BattleQueryDispatcher queryDispatcher_, IReadOnlyList<Creature> targets_)
        {
            // 门面与目标集合每帧先推：卡面与气泡共用同一份，任何一次重建（如可打状态切换）都按当帧的目标算，不留旧值
            _faces.Current.SetPreviewContext(queryDispatcher_, targets_);

            _previewDraft.Clear();
            IReadOnlyList<CardAffix> affixes = Card.ActiveAffixes;
            for (int i = 0; i < affixes.Count; i++)
            {
                switch (affixes[i])
                {
                    case CA_Damage damage:
                        _previewDraft.Add(CardValueRenderer.Fold(queryDispatcher_, targets_, damage.Amount.Value,
                            EStatChannel.Damage));
                        break;
                    case CA_Block block:
                        _previewDraft.Add(CardValueRenderer.Fold(queryDispatcher_, targets_, block.Amount.Value,
                            EStatChannel.Block));
                        break;
                }
            }

            if (_previewDraft.Count == _previewShown.Count)
            {
                bool changed = false;
                for (int i = 0; i < _previewDraft.Count; i++)
                {
                    if (_previewDraft[i] == _previewShown[i]) continue;
                    changed = true;
                    break;
                }

                if (!changed) return;
            }

            _previewShown.Clear();
            _previewShown.AddRange(_previewDraft);

            // 卡面：重建描述（数值着色写在牌面文本里；不可打时随整卡压暗由牌面自理）
            _faces.Current.RebuildDescription();

            // 气泡：与卡面同源（同一修正口径与配色，不随卡面压暗），正显示中立即重刷
            _faces.Current.CollectTipLines(_affixTips);
            _hoverTip.SetContents(_affixTips);
        }

        #endregion

        #region 拖拽：按下先问宿主能不能抓、拖动由 Update 阻尼跟随、松手与中断交宿主处置

        // 按下：UGUI 只会把事件送到指针下最上层那张牌，它不可抓就静默忽略，不穿透到下面那张；无宿主即没被手牌区管，同样静默
        public void OnPointerDown(PointerEventData eventData)
        {
            if (!Host || !Host.CanGrab(this)) return;
            _dragging = true;
            _pressScreenPosition = eventData.position;
            Host.OnCardGrabbed(this);
        }

        // 拖动：瞄准模式（AnyEnemy 卡）下卡牌定住不跟手，越过启动阈值后把拖动交给宿主去驱动目标箭头；
        // 其余卡不在这里写位姿——跟随由 Update 的阻尼插值统一驱动，故指针不动卡也会继续滑到位
        public void OnDrag(PointerEventData eventData)
        {
            if (!_dragging) return;
            if (Card.IsAnyEnemyTarget)
            {
                // 瞄准拖动：指针越过启动阈值才通知宿主，此后每帧都通知（箭头随指针刷新）；阈值前什么都不做，故纯点击不留痕迹。
                // 阈值判定无需本地锁存：按下位置每次按下重置、指针越过后再拖回阈值内也无妨——箭头已展开的视觉状态由流程锁存，
                // 它的每帧驱动照旧刷新位姿与颜色，与本卡是否继续上报无关
                if ((eventData.position - _pressScreenPosition).sqrMagnitude <
                    _aimThresholdPx * _aimThresholdPx) return;
                Host.OnAimDragged(this);
            }
        }

        public void OnPointerUp(PointerEventData eventData)
        {
            if (!_dragging) return;
            _dragging = false;
            Host.OnCardReleased(this); // 瞄准与否由宿主分流：松手后是打出还是回位都不在这里判断
        }

        // 选牌态点击：此态 CanGrab 恒为假，按下不会进入拖拽，纯点击经这里上报宿主
        public void OnPointerClick(PointerEventData eventData)
        {
            if (eventData.button != PointerEventData.InputButton.Left) return;
            if (Host && Host.IsSelectMode) Host.OnCardClicked(this);
        }

        #endregion

        #region 悬停进出：区域裁定出唯一悬停牌后调用，进入按槽位偏移位对齐，退出按槽位真源一次还原

        // 进入：先把两条通道从本卡手里交出去（杀自己的补间），再由两个组件补到区域给的偏移位与悬停缩放；
        // 换层已由区域先行完成，这里只管姿态
        public void EnterHover(Vector3 worldPosition_, Quaternion worldRotation_)
        {
            if (_hovered) return;
            _hovered = true;
            KillMotion();
            _hoverOffset.Play(worldPosition_, worldRotation_);
            _hoverScaler.PlayScaleRelative(_hoverScaler.HoverScale);
        }

        // 退出：同样先交接再还原——按区域给的槽位世界姿态一次还原位置、朝向与缩放三项，与进入时的瞬时状态无关，
        // 补间途中进出也不会写回半截姿态
        public void ExitHover(Vector3 worldPosition_, Quaternion worldRotation_, float scale_)
        {
            if (!_hovered) return;
            _hovered = false;
            KillMotion();
            _hoverOffset.Play(worldPosition_, worldRotation_);
            _hoverScaler.PlayScale(scale_);
        }

        #endregion

        // 离手 / 被拖走 / 入场中等不还原场景：只清状态并收掉两个组件的补间，姿态交给后续补间（重排会重新声明目标）
        public void ClearHover()
        {
            _hovered = false;
            _hoverScaler.Kill();
            _hoverOffset.Kill();
        }

        #region 落位与层归属：世界姿态与所在层的唯一写入通道

        public void Place(Vector3 worldPosition_, Quaternion worldRotation_)
        {
            _rect.position = worldPosition_;
            _rect.rotation = worldRotation_;
        }

        // 换层：保世界位置重挂（区域只负责挂载与遮挡，矩形怎么摆都不会让牌跳），index_ < 0 表示追加置末，否则插到指定兄弟位；
        // 排位没变时一个字都不写——写兄弟序号会标脏画布触发重建
        public void MoveToLayer(Transform layer_, int index_)
        {
            if (_rect.parent != layer_) _rect.SetParent(layer_, true);
            int target = index_ < 0 ? _rect.parent.childCount - 1 : index_;
            if (_rect.GetSiblingIndex() != target) _rect.SetSiblingIndex(target);
        }

        #endregion

        #region 动效：位置与缩放两通道补间，同目标不重开、换目标先 Kill 同通道

        // 滑翔落位：位置与朝向按补间推进，起终点都是世界姿态。通道独占的判据比"通道已声明的目标"、不比当前姿态——
        // 补间刚建立、transform 还没挪动的同一帧里，当前姿态会把"我马上要离开的位置"误判成"已就位"；
        // 故在飞时同目标续用（返回该补间的到岸等待）、异目标杀旧重开，不在飞时已就位才不起空转补间
        public UniTask MoveToAsync(Vector3 worldPosition_, Quaternion worldRotation_, float duration_)
        {
            if (_moveTween.IsActive())
            {
                if (!MatchesMoveTarget(worldPosition_, worldRotation_))
                    StartMove(worldPosition_, worldRotation_, duration_);
                return _moveTween.WaitAsync();
            }

            return IsAtPose(worldPosition_, worldRotation_)
                ? UniTask.CompletedTask
                : StartMove(worldPosition_, worldRotation_, duration_);
        }

        // 缩放走同一口径：在飞同目标续用、异目标杀旧重开，不在飞时已就位不起空转补间
        public UniTask ScaleToAsync(float scale_, float duration_)
        {
            if (_scaleTween.IsActive())
            {
                if (Mathf.Abs(_scaleTarget - scale_) >= ScaleEpsilon) StartScale(scale_, duration_);
                return _scaleTween.WaitAsync();
            }

            return IsAtScale(scale_) ? UniTask.CompletedTask : StartScale(scale_, duration_);
        }

        // 起一条移动补间：先记住本次目标（重开判据以它为准），再把位置与朝向按补间推进（位姿通道与悬停偏移共用同一实现）
        private UniTask StartMove(Vector3 worldPosition_, Quaternion worldRotation_, float duration_)
        {
            _moveTargetPosition = worldPosition_;
            _moveTargetRotation = worldRotation_;
            return UiTween.PlayPose(ref _moveTween, _rect, worldPosition_, worldRotation_, duration_).WaitAsync();
        }

        // 起一条缩放补间：同样先记住目标；同通道独占由 UiTween 建前杀旧值保证
        private UniTask StartScale(float scale_, float duration_)
        {
            _scaleTarget = scale_;
            return UiTween.PlayScalar(ref _scaleTween, () => transform.localScale.x,
                value_ => transform.localScale = Vector3.one * value_, scale_, duration_).WaitAsync();
        }

        // 两条通道的目标比较：阈值复用"已就位"那三项，不另立常量
        private bool MatchesMoveTarget(Vector3 worldPosition_, Quaternion worldRotation_) =>
            (_moveTargetPosition - worldPosition_).sqrMagnitude < PoseEpsilon * PoseEpsilon &&
            Quaternion.Angle(_moveTargetRotation, worldRotation_) < AngleEpsilon;

        #endregion

        #region 清干净播放的动画

        // 回收前统一清干净补间（含悬停组件那两条），防止补间继续写已被复用的对象
        public void KillMotion()
        {
            _moveTween?.Kill();
            _moveTween = null;
            _scaleTween?.Kill();
            _scaleTween = null;
            _hoverScaler.Kill();
            _hoverOffset.Kill();
        }

        #endregion
    }
}