using UnityEngine;

namespace _DreamKeep.Presentation
{
    /// <summary>悬停裁决者：每帧算出指针下唯一一张手牌并只驱动它——目标不变就什么都不做，变化时先撤旧再进新，重叠手牌不会互相争抢。
    /// 不吃指针事件：指针不动时手牌增删也会被下次裁决接管或让位。它自身不留状态，悬停身份记在交互闸门上，排布与清悬停都从那里读。
    /// 悬停只独占缩放与层序：位置与朝向一律归手牌排布，所以悬停中的牌会跟着整手牌一起让位，不会滞留在旧槽位；
    /// 退出还原值一律向手牌槽位真源现算，绝不记进入前的瞬时值，补间途中进出也不会写回半截尺寸；
    /// 离手 / 被拖走 / 挂出 / 入场中才只清状态，把层序与尺寸交给后续补间。
    /// 指针命中与层序比较交给登记的共用裁决（ResolveTopCard），与按下抓取同源，重叠时两边都取视觉最上层那张；
    /// 指针的屏幕坐标 → 卡牌所在世界平面的换算也收在这里，门面的宿主契约方法转发它。</summary>
    public class HandHoverArbiter
    {
        private readonly HandCardRegistry _registry;
        private readonly HandFanLayout _layout;
        private readonly HandInteractionGate _gate;
        private readonly Camera _camera;

        public HandHoverArbiter(HandCardRegistry registry_, HandFanLayout layout_, HandInteractionGate gate_,
            Camera camera_)
        {
            _registry = registry_;
            _layout = layout_;
            _gate = gate_;
            _camera = camera_;
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
            Obj_Card current = _gate.HoveredCard;
            Obj_Card target = _registry.ResolveTopCard(PointerWorldPosition(), candidate_ =>
                _gate.IsHoverable(candidate_, candidate_ == current) && _registry.TryGetHover(candidate_, out _));
            if (target == current) return;

            if (current) ExitHover(current);
            _gate.SetHovered(target);
            if (target && _registry.TryGetHover(target, out Comp_CardHover hover)) hover.EnterHover();
        }

        // 让出悬停身份：切换不可打、牌离手或被拖走时由演出侧调用，只清组件状态与身份，尺寸与层序一律交给后续补间
        public void ClearHover()
        {
            Obj_Card hovered = _gate.HoveredCard;
            if (!hovered) return;
            if (_registry.TryGetHover(hovered, out Comp_CardHover hover)) hover.ClearHover();
            _gate.SetHovered(null);
        }

        // 撤下悬停：只要牌还在手牌里、本回合可打、没被拖拽或挂出、没在入场飞行，就按当前槽位值写回缩放与层序；
        // 位置不在此处补——它归排布，正在飞的滑移补间会把它送到槽位；其余情况（离手 / 被拖走 / 挂出 / 入场中）只清状态交给后续补间
        private void ExitHover(Obj_Card card_)
        {
            if (!_registry.TryGetHover(card_, out Comp_CardHover hover)) return;
            if (!_gate.IsHoverable(card_, ignoreMotion_: true))
            {
                hover.ClearHover();
                return;
            }

            _layout.EvaluateSlot(_registry.IndexOf(card_), _registry.Count, out _, out _, out float scale, out int order);
            hover.ExitHover(order, scale);
        }
    }
}
