using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using Unity.Mathematics;
using UnityEngine;
using UnityEngine.Splines;

namespace _DreamKeep.Presentation
{
    /// <summary>手牌空间：前半是槽位真源——把"第 index 张、共 count 张"求解成曲线上的位置、朝向、缩放与层序，并提供整体重排、补间回位与瞬移落位三种写入方式。
    /// 排布规则——以曲线中点为锚向两侧等距张开（所需区间超出可用范围时整段自动压缩，第一张恒居中），朝向取切线、越靠中越大，层序自基准递增；
    /// 横向跨度随窗口宽高比伸缩（位置与切线一起乘系数），保持手牌占屏比例恒定；不可打状态下整手牌沿 y 下沉，下沉量也在这里叠加。
    /// 因此它是槽位真源：悬停退出、被拒退回、拖拽中断都向它取还原值，任何调用方都不许记进入前的瞬时状态。
    /// 位置与朝向永远归它（悬停中的牌也照常跟着让位，只走不写层序的滑移通道），缩放与层序则由悬停临时独占。
    /// 后半是指针裁定——每帧算出指针下唯一一张手牌并只驱动它：目标不变就什么都不做，变化时先撤旧再进新，重叠手牌不会互相争抢；
    /// 它不吃指针事件，指针不动时手牌增删也会被下次裁决接管或让位，悬停身份记在手牌登记处，排布与清悬停都从那里读。
    /// 悬停只独占缩放与层序，退出还原值一律按槽位真源现算，绝不记进入前的瞬时值，补间途中进出也不会写回半截尺寸；
    /// 离手 / 被拖走 / 挂出 / 入场中才只清状态，把层序与尺寸交给后续补间。
    /// 指针命中与层序比较交给手牌登记处的共用裁决（ResolveTopCard），与按下抓取同源，重叠时两边都取视觉最上层那张；
    /// 指针的屏幕坐标 → 卡牌所在世界平面的换算也收在这里，门面的宿主契约方法转发它；
    /// 窗口宽高比变化也由它察觉，察觉即让手牌按新的可视宽度重排一次。
    /// 可打状态、排布例外（被拖拽或挂出等待结算）与悬停身份一律向手牌登记处现读，不自行记状态。</summary>
    public class HandSpace
    {
        private readonly SplineContainer _spline;
        private readonly float _startT;
        private readonly float _endT;
        private readonly float _stepT;
        private readonly float _moveDuration;
        private readonly int _fullCount;
        private readonly float _scaleStep;
        private readonly float _minScale;
        private readonly int _baseOrder;
        private readonly float _unplayableDropY;
        private readonly HandCardRegistry _registry;
        private readonly Camera _camera;
        private readonly float _referenceHalfWidth;
        private float _lastAspect;

        public HandSpace(SplineContainer spline_, float startT_, float endT_, float stepT_, float moveDuration_,
            int fullCount_, float scaleStep_, float minScale_, int baseOrder_, float unplayableDropY_,
            HandCardRegistry registry_, Camera camera_, float referenceHalfWidth_)
        {
            _spline = spline_;
            _startT = startT_;
            _endT = endT_;
            _stepT = stepT_;
            _moveDuration = moveDuration_;
            _fullCount = fullCount_;
            _scaleStep = scaleStep_;
            _minScale = minScale_;
            _baseOrder = baseOrder_;
            _unplayableDropY = unplayableDropY_;
            _registry = registry_;
            _camera = camera_;
            _referenceHalfWidth = referenceHalfWidth_;
            _lastAspect = camera_.aspect;
        }

        // 槽位求解：曲线上的位置与切线决定朝向，间距超出可用区间时整段压缩；不可打时在 y 上叠下沉量，层序自基准递增
        public void EvaluateSlot(int index_, int count_, out Vector3 position_, out Quaternion rotation_, out float scale_,
            out int order_)
        {
            float step = count_ <= 1 ? 0f : Mathf.Min(_stepT, (_endT - _startT) / (count_ - 1));
            float t = (_startT + _endT) * 0.5f + (index_ - (count_ - 1) * 0.5f) * step;
            _spline.Evaluate(t, out float3 position, out float3 tangent, out _);
            // 可视宽度伸缩：位置与切线一起乘 k，朝向由乘后切线求出，卡牌本身不变形
            float widthScale = EvaluateWidthScale();
            position_ = new Vector3(position.x * widthScale, position.y, position.z);
            if (!_registry.CardsPlayable) position_.y += _unplayableDropY;
            Vector3 direction = new(tangent.x * widthScale, tangent.y, tangent.z);
            rotation_ = Quaternion.Euler(0f, 0f, Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg);
            scale_ = Mathf.Max(_minScale, 1f - _scaleStep * Mathf.Max(0, count_ - _fullCount));
            order_ = _baseOrder + index_;
        }

        // 可视宽度伸缩系数：手牌横向跨度随窗口宽高比收放以保持占屏比例恒定，极端比例由上下限兜住
        private float EvaluateWidthScale() =>
            Mathf.Clamp(_camera.aspect * _camera.orthographicSize / _referenceHalfWidth, 0.75f, 1.5f);

        // 整体重排：手牌增删或可打状态切换后调用；跳过例外牌（刚起飞的新牌）与登记处判定为忙的牌（被拖拽、已挂出），
        // 那些牌各自有独占通道的补间，这里再动一次会互相 Kill。
        // 悬停中的牌照常跟排布走位：位置与朝向归排布（滑移通道不写层序），缩放与层序留在悬停手上
        public void ApplyLayout(Obj_Card except_ = null)
        {
            IReadOnlyList<Obj_Card> hand = _registry.Hand;
            int count = hand.Count;
            for (int i = 0; i < count; i++)
            {
                Obj_Card card = hand[i];
                if (card == except_ || _registry.IsBusy(card)) continue;

                EvaluateSlot(i, count, out Vector3 position, out Quaternion rotation, out float scale, out int order);
                if (_registry.HoveredCard == card)
                {
                    card.MoveToAsync(position, rotation, _moveDuration).Forget();
                    continue;
                }

                card.MoveToAsync(position, rotation, order, _moveDuration).Forget();
                card.ScaleToAsync(scale, _moveDuration).Forget();
            }
        }

        // 补间回槽位：松手未命中出牌区、命令被拒退回都走它
        public UniTask ReturnToSlotAsync(Obj_Card card_)
        {
            if (!TryEvaluateSlot(card_, out Vector3 position, out Quaternion rotation, out float scale, out int order))
                return UniTask.CompletedTask;

            return UniTask.WhenAll(
                card_.MoveToAsync(position, rotation, order, _moveDuration),
                card_.ScaleToAsync(scale, _moveDuration));
        }

        // 瞬移回槽位：拖拽被强制中断时用，不播补间
        public void PlaceToSlot(Obj_Card card_)
        {
            if (!TryEvaluateSlot(card_, out Vector3 position, out Quaternion rotation, out float scale, out int order))
                return;

            card_.Place(position, rotation, order);
            card_.transform.localScale = Vector3.one * scale;
        }

        // 槽位查询：以手牌序号现算槽位，已离手的牌没有槽位可回，返回 false 由调用方静默
        private bool TryEvaluateSlot(Obj_Card card_, out Vector3 position_, out Quaternion rotation_, out float scale_,
            out int order_)
        {
            int index = _registry.IndexOf(card_);
            if (index < 0)
            {
                position_ = default;
                rotation_ = default;
                scale_ = 1f;
                order_ = 0;
                return false;
            }

            EvaluateSlot(index, _registry.Count, out position_, out rotation_, out scale_, out order_);
            return true;
        }

        // 屏幕指针 → 世界坐标（投射到卡牌所在的 z=0 平面）：悬停命中与拖拽跟手共用
        public Vector3 PointerWorldPosition()
        {
            Vector3 pointer = Input.mousePosition;
            pointer.z = -_camera.transform.position.z;
            return _camera.ScreenToWorldPoint(pointer);
        }

        // 每帧裁定一次：指针下没有可交互的牌时目标为 null，等价于退出；裁决次序为"先定目标 → 撤旧 → 写新身份 → 进新"
        public void Tick()
        {
            // 窗口宽高比变化（拉伸窗口、改分辨率）时先按新的可视宽度重排一次手牌，不必等下一次抽牌
            if (!Mathf.Approximately(_lastAspect, _camera.aspect))
            {
                _lastAspect = _camera.aspect;
                ApplyLayout();
            }

            Obj_Card current = _registry.HoveredCard;
            Obj_Card target = _registry.ResolveTopCard(PointerWorldPosition(), candidate_ =>
                _registry.IsHoverable(candidate_, candidate_ == current) && _registry.TryGetHover(candidate_, out _));
            if (target == current) return;

            if (current) ExitHover(current);
            _registry.SetHovered(target);
            if (target && _registry.TryGetHover(target, out Comp_CardHover hover)) hover.EnterHover();
        }

        // 让出悬停身份：切换不可打、牌离手或被拖走时由演出侧调用，只清组件状态与身份，尺寸与层序一律交给后续补间
        public void ClearHover()
        {
            Obj_Card hovered = _registry.HoveredCard;
            if (!hovered) return;
            if (_registry.TryGetHover(hovered, out Comp_CardHover hover)) hover.ClearHover();
            _registry.SetHovered(null);
        }

        // 撤下悬停：只要牌还在手牌里、本回合可打、没被拖拽或挂出、没在入场飞行，就按当前槽位值写回缩放与层序；
        // 位置不在此处补——它归排布，正在飞的滑移补间会把它送到槽位；其余情况（离手 / 被拖走 / 挂出 / 入场中）只清状态交给后续补间
        private void ExitHover(Obj_Card card_)
        {
            if (!_registry.TryGetHover(card_, out Comp_CardHover hover)) return;
            if (!_registry.IsHoverable(card_, ignoreMotion_: true))
            {
                hover.ClearHover();
                return;
            }

            EvaluateSlot(_registry.IndexOf(card_), _registry.Count, out _, out _, out float scale, out int order);
            hover.ExitHover(order, scale);
        }
    }
}
