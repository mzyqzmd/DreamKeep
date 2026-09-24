using System.Collections.Generic;
using _DreamKeep.RuntimeData;
using Cysharp.Threading.Tasks;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.Pool;

namespace _DreamKeep.Presentation
{
    public class HandArea
    {
        private readonly ObjectPool<UI_CardHolder> _cardPool;
        private readonly UI_HandView _host;
        private readonly UI_PlayCardZone _dropZone;
        private readonly RectTransform _fanCenterPoint;
        private readonly Transform _handLayer;
        private readonly Transform _flyLayer;
        private readonly Transform _focusLayer;
        private readonly Camera _canvasCamera;
        private readonly float _halfRunPx;
        private readonly float _stepPx;
        private readonly float _sagPx;
        private readonly float _tiltMaxDeg;
        private readonly float _moveDuration;
        private readonly int _fullCount;
        private readonly float _scaleStep;
        private readonly float _minScale;
        private readonly float _unplayableDropPx;
        private readonly List<UI_CardHolder> _hand = new();
        private readonly Dictionary<CardModel, UI_CardHolder> _views = new();
        private readonly HashSet<UI_CardHolder> _entering = new();

        // 交互状态位：本回合可打、正被拖拽、正被悬停
        private bool _cardsPlayable;
        private UI_CardHolder _draggedCard;
        private UI_CardHolder _hoveredCard;

        // 逐帧射线裁决的复用件：画布下的 Graphic 每帧都要过一遍矩形测试，命中列表与指针数据只建一次，不为每帧产生垃圾
        private readonly List<RaycastResult> _raycastResults = new();
        private PointerEventData _pointerData;
        private EventSystem _pointerEventSystem;

        public HandArea(UI_CardHolder cardPrefab_, UI_HandView host_, UI_PlayCardZone dropZone_,
            RectTransform fanCenterPoint_, Transform handLayer_, Transform flyLayer_, Transform focusLayer_,
            Camera canvasCamera_, float halfRunPx_, float stepPx_, float sagPx_, float tiltMaxDeg_,
            float moveDuration_, int fullCount_, float scaleStep_, float minScale_, float unplayableDropPx_)
        {
            _host = host_;
            _dropZone = dropZone_;
            _fanCenterPoint = fanCenterPoint_;
            _handLayer = handLayer_;
            _flyLayer = flyLayer_;
            _focusLayer = focusLayer_;
            _canvasCamera = canvasCamera_;
            _halfRunPx = halfRunPx_;
            _stepPx = stepPx_;
            _sagPx = sagPx_;
            _tiltMaxDeg = tiltMaxDeg_;
            _moveDuration = moveDuration_;
            _fullCount = fullCount_;
            _scaleStep = scaleStep_;
            _minScale = minScale_;
            _unplayableDropPx = unplayableDropPx_;
            _cardPool = new ObjectPool<UI_CardHolder>(
                () => Object.Instantiate(cardPrefab_, handLayer_),
                view_ => view_.gameObject.SetActive(true),
                view_ =>
                {
                    view_.KillMotion();
                    view_.gameObject.SetActive(false);
                    _entering.Remove(view_); // 回收即注销入场标记与交互身份，防实例复用后残留旧状态
                    if (_hoveredCard == view_) _hoveredCard = null;
                    if (_draggedCard == view_) _draggedCard = null;
                }, defaultCapacity: 10, maxSize: 64); // 洗回时同时借出手牌与整摞飞行牌，池上限要容得下两者之和
        }

        /// <summary>行内张数（行＝手牌列减去已让位给结算的牌）：抽牌入场按它取新牌的槽位序号。</summary>
        public int Count => RowCount();

        /// <summary>本回合手牌是否可打：新牌入场刷外观与松手要不要打出都按它。</summary>
        public bool CardsPlayable => _cardsPlayable;

        #region 台账：取牌、离手、归宿与飞行借还

        // 取出并登记：取池 → 绑卡 → 接入宿主 → 进手牌列 → 归手牌层 → 标记入场中（入场期间命中面关着，落位才开）
        public UI_CardHolder Spawn(CardModel model_)
        {
            UI_CardHolder view = _cardPool.Get();
            view.Bind(model_);
            view.SetHost(_host);
            _views[model_] = view;
            _hand.Add(view);
            MoveToHand(view); // 池复用的实例可能残留别的父节点与兄弟位，取出即归位
            _entering.Add(view);
            RefreshPointer(view);
            return view;
        }

        // 离手：只从手牌列除名（模型索引仍保留——打出后还要按模型把它找回来等到达），顺手关掉命中面
        public void RemoveFromHand(UI_CardHolder card_)
        {
            _hand.Remove(card_);
            RefreshPointer(card_);
        }

        // 归宿收尾：注销模型索引并归还卡池（离场补间已由调用方等完，回收回调会清干净补间）
        public void Release(CardModel model_, UI_CardHolder view_)
        {
            _views.Remove(model_);
            _cardPool.Release(view_);
        }

        // 借一张飞行用的包装（洗回搬运）：只取池、不入手牌列与模型索引，牌面沿用上次 Bind 的残留内容，由调用方摆位后立刻归还
        public UI_CardHolder RentFlyView()
        {
            UI_CardHolder view = _cardPool.Get();
            MoveToFly(view); // 出池即换到飞行层，保证借出去的牌一定画在队列与手牌之上，也顺手关掉命中面
            return view;
        }

        // 归还飞行用的包装：与归宿收尾同一条回收路径，回收回调会清补间、注销入场标记与交互身份
        public void ReturnFlyView(UI_CardHolder view_) => _cardPool.Release(view_);

        public bool TryGetView(CardModel model_, out UI_CardHolder view_) => _views.TryGetValue(model_, out view_);

        public bool Contains(CardModel model_) => _views.ContainsKey(model_);

        /// <summary>这张包装此刻是否还在手牌列里：拖拽途中被弃置 / 回收时用它判"过期的松手"。</summary>
        public bool IsInHand(UI_CardHolder card_) => _hand.Contains(card_);

        // 入场中标记只由抽牌入场演出收起（含中途离场的收尾），落位时命中面随之打开
        public void UnmarkEntering(UI_CardHolder card_)
        {
            _entering.Remove(card_);
            RefreshPointer(card_);
        }

        #endregion

        #region 资格：可打状态、抓取与悬停

        // 切换可打状态：返回是否真的变了，调用方据此决定要不要跑清悬停、重排、刷外观这一串
        public bool SetPlayable(bool playable_)
        {
            if (_cardsPlayable == playable_) return false;
            _cardsPlayable = playable_;
            return true;
        }

        // 按当前可打状态刷整手牌外观：不可打时整卡压暗，可打时恢复本色
        public void RefreshPlayableLook(bool playable_)
        {
            foreach (UI_CardHolder card in _hand) card.SetPlayableLook(playable_);
        }

        /// <summary>这张牌此刻接不接指针（按下抓取与逐帧悬停共用）：只看合法性，不看动画——滑行中、正要合拢的牌照收。</summary>
        public bool IsInteractive(UI_CardHolder card_) =>
            _cardsPlayable && _draggedCard == null && IsInRow(card_) && !_entering.Contains(card_);

        // 拖拽身份：抓起时写入，松手与中断时清空
        public void SetDragged(UI_CardHolder card_) => _draggedCard = card_;

        public void ClearDragged() => _draggedCard = null;

        #endregion

        #region 层归属：谁此刻属于哪一层、在层内排第几

        // 手牌层：按行内序号插位（靠右压靠左）；不在行里（已让位、正在回收）只换层
        public void MoveToHand(UI_CardHolder card_) => card_.MoveToLayer(_handLayer, RowIndexOf(card_));

        // 飞行层：离场、洗回与遗忘借道一律追加置末；飞行的牌不再响应指针，命中面一并关掉
        public void MoveToFly(UI_CardHolder card_)
        {
            card_.MoveToLayer(_flyLayer, -1);
            RefreshPointer(card_);
        }

        // 交互层：悬停与拖拽一律追加置末，后进来的那张（抓起的）自然压过悬停那张
        public void MoveToFocus(UI_CardHolder card_) => card_.MoveToLayer(_focusLayer, -1);

        #endregion

        #region 槽位与排布：以手牌区的点为准

        // 槽位求解：x 等距张开（间距撑不破可用半宽就保持原间距，撑破则整段压缩），y 落在浅弧上，朝向取切角；
        // 不可打时在 y 上叠下沉量。弧顶（正中那张）恒落在手牌区的点上，点被拖动、缩放、旋转时整套跟着走，
        // 故这里不需要任何基准高度，也不需要知道参差多层的父节点长什么样
        public void EvaluateSlot(int index_, int count_, out Vector3 worldPosition_, out Quaternion worldRotation_,
            out float scale_)
        {
            float gap = count_ <= 1 ? 0f : Mathf.Min(_stepPx, 2f * _halfRunPx / (count_ - 1));
            float x = (index_ - (count_ - 1) * 0.5f) * gap;
            // 弧线按可用半宽归一化：正中落在点上（偏移 0），两端比中间低出拱高
            float offset = x / _halfRunPx;
            float y = -_sagPx * offset * offset;
            if (!_cardsPlayable) y += _unplayableDropPx;
            worldPosition_ = _fanCenterPoint.TransformPoint(new Vector2(x, y));
            worldRotation_ = _fanCenterPoint.rotation * Quaternion.Euler(0f, 0f, -_tiltMaxDeg * offset);
            scale_ = Mathf.Max(_minScale, 1f - _scaleStep * Mathf.Max(0, count_ - _fullCount));
        }

        // 整体重排：手牌增删、让位与可打状态切换后调用，也是命中面的统一刷新点。
        // 行内序号按"行"数（已让位等结算的牌不占号，也不被排布驱动）；入场中的牌独占入场补间，
        // 正被悬停与被拖拽的牌各有独占通道的补间（悬停独占位置、朝向、缩放与所在层），这些只占位、不写姿态；
        // 姿态已经等于目标的牌不起补间，省掉空转补间与白标的画布脏标记
        public void ApplyLayout()
        {
            int count = RowCount();
            int index = 0;
            for (int i = 0; i < _hand.Count; i++)
            {
                UI_CardHolder card = _hand[i];
                RefreshPointer(card); // 入队让位的牌在这里把关掉的命中面补上
                if (_dropZone.Contains(card)) continue;

                if (card == _hoveredCard || card == _draggedCard || _entering.Contains(card))
                {
                    index++;
                    continue;
                }

                MoveToHand(card); // 手牌层内按行内序号插位：靠右压靠左（序号没变时一个字都不写）
                EvaluateSlot(index, count, out Vector3 position, out Quaternion rotation, out float scale);
                index++;
                if (!card.IsAtPose(position, rotation)) card.MoveToAsync(position, rotation, _moveDuration).Forget();
                if (!card.IsAtScale(scale)) card.ScaleToAsync(scale, _moveDuration).Forget();
            }
        }

        // 补间回槽位：松手未命中出牌区时用，顺手把牌换回手牌层并按行内序号插位
        public UniTask ReturnToSlotAsync(UI_CardHolder card_)
        {
            if (!TryEvaluateSlot(card_, out Vector3 position, out Quaternion rotation, out float scale))
                return UniTask.CompletedTask;

            MoveToHand(card_);
            return UniTask.WhenAll(
                card_.MoveToAsync(position, rotation, _moveDuration),
                card_.ScaleToAsync(scale, _moveDuration));
        }

        // 瞬移回槽位：拖拽被强制中断时用，不播补间
        public void PlaceToSlot(UI_CardHolder card_)
        {
            if (!TryEvaluateSlot(card_, out Vector3 position, out Quaternion rotation, out float scale)) return;

            MoveToHand(card_);
            card_.Place(position, rotation);
            card_.transform.localScale = Vector3.one * scale;
        }

        #endregion

        #region 指针：屏幕坐标换算与每帧裁决

        // 指针在画布平面上的世界坐标：拖拽跟手与落点判定共用。画布是 ScreenSpaceCamera，换算必须传画布相机
        public Vector3 PointerWorldPosition()
        {
            RectTransformUtility.ScreenPointToWorldPointInRectangle(_fanCenterPoint,
                Pointer.current.position.ReadValue(),
                _canvasCamera, out Vector3 world);
            return world;
        }

        // 每帧裁定一次：指针下没有可交互的牌时目标为 null，等价于退出；裁决次序为"先定目标 → 撤旧 → 写新 → 进新"。
        // 悬停姿态由包装自己播，这里只决定"进哪张、退到哪"；进入姿态一律以槽位为基准
        // （槽位位置 + 沿点朝上的抬起、朝向回正到点的朝向），故滑到一半被悬停也不会停在半途
        public void Tick()
        {
            UI_CardHolder current = _hoveredCard;
            UI_CardHolder target = RaycastTopCard();
            if (target == current) return;

            if (current) ExitHover(current);
            _hoveredCard = target;
            if (!target) return;

            EvaluateSlot(RowIndexOf(target), Count, out Vector3 position, out _, out _);
            MoveToFocus(target); // 悬停期间位置、朝向、缩放与所在层整段归包装的悬停姿态独占
            target.EnterHover(position + _fanCenterPoint.TransformVector(Vector3.up * target.HoverLiftPx),
                _fanCenterPoint.rotation);
        }

        // 让出悬停身份：切换不可打、牌离手或被拖走时由演出侧调用，只清状态与身份，姿态一律交给后续补间
        public void ClearHover()
        {
            if (!_hoveredCard) return;
            _hoveredCard.ClearHover();
            _hoveredCard = null;
        }

        #endregion

        // 行内：手牌列里还没让位给出牌区等结算的牌。行内序号即槽位序号，已入队的牌不占号
        private bool IsInRow(UI_CardHolder card_) => _hand.Contains(card_) && !_dropZone.Contains(card_);

        // 行内张数：抽牌入场取新牌槽位序号与槽位求解共用
        private int RowCount()
        {
            int count = 0;
            for (int i = 0; i < _hand.Count; i++)
                if (!_dropZone.Contains(_hand[i]))
                    count++;
            return count;
        }

        // 行内序号：不在行里（已让位等结算 / 已离手）返回 -1，由调用方静默
        private int RowIndexOf(UI_CardHolder card_)
        {
            if (!IsInRow(card_)) return -1;
            int index = 0;
            for (int i = 0; i < _hand.Count; i++)
            {
                UI_CardHolder card = _hand[i];
                if (card == card_) break;
                if (!_dropZone.Contains(card)) index++;
            }

            return index;
        }

        // 命中面刷新：只有"真在手牌行里、且不在入场飞行中"的牌才接指针；入场中、已让位等结算、飞行的牌一律让开
        private void RefreshPointer(UI_CardHolder card_) =>
            card_.SetPointerEnabled(IsInRow(card_) && !_entering.Contains(card_));

        // 槽位查询：以行内序号现算槽位，已让位等结算与已离手的牌没有槽位可回，返回 false 由调用方静默
        private bool TryEvaluateSlot(UI_CardHolder card_, out Vector3 worldPosition_, out Quaternion worldRotation_,
            out float scale_)
        {
            int index = RowIndexOf(card_);
            if (index < 0)
            {
                worldPosition_ = default;
                worldRotation_ = default;
                scale_ = 1f;
                return false;
            }

            EvaluateSlot(index, RowCount(), out worldPosition_, out worldRotation_, out scale_);
            return true;
        }

        // 射线裁决：按命中先后取第一张资格通过的牌；同一张卡的多个命中面只判一次。
        // 当前悬停中的牌照常作为候选参与（指针还停在它身上就是"目标不变"），因此它的进出补间不会把自己顶掉
        private UI_CardHolder RaycastTopCard()
        {
            if (_hand.Count == 0) return null; // 手牌空时指针下不可能有可悬停的牌，这次射线直接省掉
            EventSystem eventSystem = EventSystem.current;
            if (!eventSystem) return null;

            if (_pointerEventSystem != eventSystem)
            {
                _pointerEventSystem = eventSystem;
                _pointerData = new PointerEventData(eventSystem);
            }

            _pointerData.position = Pointer.current.position.ReadValue();
            _raycastResults.Clear();
            eventSystem.RaycastAll(_pointerData, _raycastResults);

            UI_CardHolder previous = null;
            foreach (RaycastResult result in _raycastResults)
            {
                UI_CardHolder card = result.gameObject.GetComponentInParent<UI_CardHolder>();
                if (card == previous) continue; // 同一张卡可能被多个命中面报出，只判一次
                previous = card;
                if (!card) continue;
                if (IsInteractive(card)) return card;
            }

            return null;
        }

        // 撤下悬停：只要牌还在行里、本回合可打、没被拖拽或让位，就换回手牌层并按当前槽位真源一次还原位置、朝向与缩放；
        // 其余情况（离手 / 被拖走 / 已让位等结算）只清状态交给后续补间
        private void ExitHover(UI_CardHolder card_)
        {
            if (!IsInteractive(card_))
            {
                card_.ClearHover();
                return;
            }

            MoveToHand(card_);
            EvaluateSlot(RowIndexOf(card_), Count, out Vector3 position, out Quaternion rotation, out float scale);
            card_.ExitHover(position, rotation, scale);
        }
    }
}