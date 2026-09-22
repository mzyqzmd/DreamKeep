using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using UnityEngine;
using UnityEngine.EventSystems;

namespace _DreamKeep.Presentation
{
    /// <summary>手牌空间：前半是槽位真源——把"第 index 张、共 count 张"求解成卡牌坐标框架里的锚点坐标、朝向与缩放，并提供整体重排、补间回位与瞬移落位三种写入方式。
    /// 坐标框架就是构造时传入的手牌中心节点（即本类的容器）：正中那张牌的锚点落在节点上，其余槽位以它为中心向两侧等距张开
    /// （张数多到间距撑破可用半宽时整段自动压缩），位置落在一条浅弧上：正中最高、两端比正中低出拱高；朝向取弧线切角，越靠两端向外倾得越多，正中回正。
    /// 因此它是槽位真源：悬停退出、被拒退回、拖拽中断都向它取还原值，任何调用方都不许记进入前的瞬时状态。
    /// 悬停中的牌除外——它被拎起来端详，位置、朝向、缩放与所在层整段归包装的悬停姿态独占，重排一律跳过它，正在看的牌不会被让位拽走。
    /// 层不在这里判断：排布、回位与悬停退出都只是向手牌登记处说一句"这张牌回手牌层，按它的手牌序号插位"。
    /// 后半是指针裁定——每帧向界面问一次"指针下最上层是哪张牌"（射线顺序即层级顺序即渲染顺序），只驱动唯一一张：
    /// 目标不变就什么都不做，变化时先撤旧再进新，重叠手牌不会互相争抢；
    /// 它不吃指针事件，指针不动时手牌增删也会被下次裁决接管或让位，悬停身份记在手牌登记处，排布与清悬停都从那里读。
    /// 悬停姿态由包装（UI_CardHolder）自己播，这里只决定"进哪张、退到哪"；退出还原值一律按槽位真源现算，
    /// 离手 / 被拖走 / 挂出 / 入场中才只清状态，把姿态交给后续补间。
    /// 指针的屏幕坐标 → 手牌中心节点本地坐标的换算也收在这里，门面的宿主契约方法转发它。
    /// 可打状态、排布例外（被拖拽或挂出等待结算）与悬停身份一律向手牌登记处现读，不自行记状态。</summary>
    public class HandSpace
    {
        private readonly RectTransform _container; // 卡牌坐标框架：手牌中心节点，四个卡层挂在它名下
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
        private readonly HandCardRegistry _registry;

        // 逐帧射线裁决的复用件：画布下的 Graphic 每帧都要过一遍矩形测试，命中列表与指针数据只建一次，不为每帧产生垃圾
        private readonly List<RaycastResult> _raycastResults = new();
        private PointerEventData _pointerData;
        private EventSystem _pointerEventSystem;

        public HandSpace(RectTransform container_, Camera canvasCamera_, float halfRun_, float stepPx_,
            float sagPx_, float tiltMaxDeg_, float moveDuration_, int fullCount_, float scaleStep_, float minScale_,
            float unplayableDropPx_, HandCardRegistry registry_)
        {
            _container = container_;
            _canvasCamera = canvasCamera_;
            _halfRunPx = halfRun_;
            _stepPx = stepPx_;
            _sagPx = sagPx_;
            _tiltMaxDeg = tiltMaxDeg_;
            _moveDuration = moveDuration_;
            _fullCount = fullCount_;
            _scaleStep = scaleStep_;
            _minScale = minScale_;
            _unplayableDropPx = unplayableDropPx_;
            _registry = registry_;
        }

        // 槽位求解：x 等距张开（间距撑不破可用半宽就保持原间距，撑破则整段压缩），y 落在浅弧上，朝向取切角；
        // 不可打时在 y 上叠下沉量。弧顶（正中那张）恒落在手牌中心节点上，故这里不需要任何基准高度
        public void EvaluateSlot(int index_, int count_, out Vector2 anchoredPosition_, out Quaternion rotation_,
            out float scale_)
        {
            float gap = count_ <= 1 ? 0f : Mathf.Min(_stepPx, 2f * _halfRunPx / (count_ - 1));
            float x = (index_ - (count_ - 1) * 0.5f) * gap;
            // 弧线按可用半宽归一化：正中落在节点上（y = 0），两端比中间低出拱高
            float offset = x / _halfRunPx;
            float y = -_sagPx * offset * offset;
            if (!_registry.CardsPlayable) y += _unplayableDropPx;
            anchoredPosition_ = new Vector2(x, y);
            rotation_ = Quaternion.Euler(0f, 0f, -_tiltMaxDeg * offset);
            scale_ = Mathf.Max(_minScale, 1f - _scaleStep * Mathf.Max(0, count_ - _fullCount));
        }

        // 整体重排：手牌增删或可打状态切换后调用；跳过例外牌（刚起飞的新牌）、正被悬停的牌与登记处判定为忙的牌（被拖拽、已挂出），
        // 这些牌各自有独占通道的补间（悬停独占位置、朝向、缩放与所在层），这里再动一次会互相 Kill，正在端详的牌也会被拽回槽位
        public void ApplyLayout(UI_CardHolder except_ = null)
        {
            IReadOnlyList<UI_CardHolder> hand = _registry.Hand;
            int count = hand.Count;
            for (int i = 0; i < count; i++)
            {
                UI_CardHolder card = hand[i];
                if (card == except_ || card == _registry.HoveredCard || _registry.IsBusy(card)) continue;

                _registry.MoveToHand(card); // 手牌层内按手牌序号插位：靠右压靠左（序号没变时一个字都不写）
                EvaluateSlot(i, count, out Vector2 position, out Quaternion rotation, out float scale);
                card.MoveToAsync(position, rotation, _moveDuration).Forget();
                card.ScaleToAsync(scale, _moveDuration).Forget();
            }
        }

        // 补间回槽位：松手未命中出牌区、命令被拒退回、回合对账都走它，顺手把牌换回手牌层并按手牌序号插位
        public UniTask ReturnToSlotAsync(UI_CardHolder card_)
        {
            if (!TryEvaluateSlot(card_, out Vector2 position, out Quaternion rotation, out float scale))
                return UniTask.CompletedTask;

            _registry.MoveToHand(card_);
            return UniTask.WhenAll(
                card_.MoveToAsync(position, rotation, _moveDuration),
                card_.ScaleToAsync(scale, _moveDuration));
        }

        // 瞬移回槽位：拖拽被强制中断时用，不播补间
        public void PlaceToSlot(UI_CardHolder card_)
        {
            if (!TryEvaluateSlot(card_, out Vector2 position, out Quaternion rotation, out float scale))
                return;

            _registry.MoveToHand(card_);
            card_.Place(position, rotation);
            card_.transform.localScale = Vector3.one * scale;
        }

        // 槽位查询：以手牌序号现算槽位，已离手的牌没有槽位可回，返回 false 由调用方静默
        private bool TryEvaluateSlot(UI_CardHolder card_, out Vector2 anchoredPosition_, out Quaternion rotation_,
            out float scale_)
        {
            int index = _registry.IndexOf(card_);
            if (index < 0)
            {
                anchoredPosition_ = default;
                rotation_ = default;
                scale_ = 1f;
                return false;
            }

            EvaluateSlot(index, _registry.Count, out anchoredPosition_, out rotation_, out scale_);
            return true;
        }

        // 屏幕指针 → 卡牌坐标框架（手牌中心节点）里的本地坐标：拖拽跟手与落点判定共用。
        // 画布是 ScreenSpaceCamera，换算必须传画布相机
        public Vector2 PointerLocalPosition()
        {
            RectTransformUtility.ScreenPointToLocalPointInRectangle(_container, Input.mousePosition, _canvasCamera,
                out Vector2 local);
            return local;
        }

        // 界面上任意节点 → 卡牌坐标框架（手牌中心节点）里的本地坐标，与卡牌锚点同一坐标系：
        // 牌堆锚点与出牌区靠它落进同一坐标系，反变换把画布缩放一并算进去
        public Vector2 ToContainer(Transform target_) => _container.InverseTransformPoint(target_.position);

        // 每帧裁定一次：指针下没有可交互的牌时目标为 null，等价于退出；裁决次序为"先定目标 → 撤旧 → 写新身份 → 进新"。
        // 命中顺序即层级顺序即渲染顺序，资格过滤在登记处；命中不合格的牌继续往下找（穿透）
        public void Tick()
        {
            UI_CardHolder current = _registry.HoveredCard;
            UI_CardHolder target = RaycastTopCard(current);
            if (target == current) return;

            if (current) ExitHover(current);
            _registry.SetHovered(target);
            if (!target) return;
            _registry.MoveToFocus(target); // 悬停期间位置、朝向、缩放与所在层整段归包装的悬停姿态独占
            target.EnterHover();
        }

        // 射线裁决：按命中先后取第一张资格通过的牌；同一张卡的多个命中面只判一次。
        // 当前悬停中的牌照常作为候选参与（指针还停在它身上就是"目标不变"），因此它的进出补间不会把自己顶掉
        private UI_CardHolder RaycastTopCard(UI_CardHolder current_)
        {
            if (_registry.Count == 0) return null; // 手牌空时指针下不可能有可悬停的牌，这次射线直接省掉
            EventSystem eventSystem = EventSystem.current;
            if (!eventSystem) return null;

            if (_pointerEventSystem != eventSystem)
            {
                _pointerEventSystem = eventSystem;
                _pointerData = new PointerEventData(eventSystem);
            }

            _pointerData.position = Input.mousePosition;
            _raycastResults.Clear();
            eventSystem.RaycastAll(_pointerData, _raycastResults);

            UI_CardHolder previous = null;
            foreach (RaycastResult result in _raycastResults)
            {
                UI_CardHolder card = result.gameObject.GetComponentInParent<UI_CardHolder>();
                if (card == previous) continue; // 同一张卡可能被多个命中面报出，只判一次
                previous = card;
                if (!card) continue;
                if (_registry.IsHoverable(card, card == current_)) return card;
            }

            return null;
        }

        // 让出悬停身份：切换不可打、牌离手或被拖走时由演出侧调用，只清状态与身份，姿态一律交给后续补间
        public void ClearHover()
        {
            UI_CardHolder hovered = _registry.HoveredCard;
            if (!hovered) return;
            hovered.ClearHover();
            _registry.SetHovered(null);
        }

        // 撤下悬停：只要牌还在手牌里、本回合可打、没被拖拽或挂出、没在入场飞行，就换回手牌层并按当前槽位真源一次还原位置、朝向与缩放；
        // 其余情况（离手 / 被拖走 / 挂出 / 入场中）只清状态交给后续补间
        private void ExitHover(UI_CardHolder card_)
        {
            if (!_registry.IsHoverable(card_, ignoreMotion_: true))
            {
                card_.ClearHover();
                return;
            }

            _registry.MoveToHand(card_);
            EvaluateSlot(_registry.IndexOf(card_), _registry.Count, out Vector2 position, out Quaternion rotation,
                out float scale);
            card_.ExitHover(position, rotation, scale);
        }
    }
}
