using Cysharp.Threading.Tasks;
using UnityEngine;

namespace _DreamKeep.Presentation
{
    /// <summary>卡牌悬停能力：把这张牌抬到最上层并放大，退出时按宿主给的槽位层序与缩放写回；只写缩放与层序，位置与朝向一律归宿主排布（重排让位时它跟着挪，但不经本组件）。</summary>
    /// 由手牌宿主统一裁决驱动——宿主逐帧裁定指针下唯一一张悬停牌并调用 EnterHover/ExitHover，同一时刻全局至多一张牌处于悬停，重叠手牌不会互相争抢。
    /// 还原值一律由宿主按手牌槽位真源给出，组件不记进入前的瞬时值，补间途中进出也不会写回半截尺寸；有该组件才有该效果，未配该组件的卡不会被驱动。</summary>
    [RequireComponent(typeof(Obj_Card))]
    public class Comp_CardHover : MonoBehaviour, ICardAbility
    {
        [Tooltip("悬停时的层序：需压过手牌与出牌区排队牌，但低于拖拽层序（Comp_CardDrag 的 100），保证抓在手里的牌永远在最上层")]
        [SerializeField] private int _hoverOrder = 90;

        [Tooltip("悬停时的缩放目标值（绝对值而非倍率）：手牌基准尺寸为 1，1.15 即比常态放大 15%")]
        [SerializeField] private float _hoverScale = 1.15f;

        [Tooltip("悬停进出的补间时长（秒）：比手牌重排的 0.28s 短，进出要跟手")]
        [SerializeField] private float _hoverDuration = 0.12f;

        private Obj_Card _card;
        private bool _hovered;

        // 认领卡根；悬停不改位置，无需相机与世界坐标换算
        public void Attach(Obj_Card card_) => _card = card_;

        #region 悬停进出：宿主裁定出唯一悬停牌后调用，进入抬层放大，退出按槽位值写回

        public void EnterHover()
        {
            if (_hovered) return;
            _hovered = true;
            _card.Place(transform.position, transform.rotation, _hoverOrder);
            _card.ScaleToAsync(_hoverScale, _hoverDuration).Forget();
        }

        // 退出：写回宿主给的槽位层序与槽位缩放，与进入时的瞬时状态无关，补间途中进出也不会写回半截尺寸
        public void ExitHover(int order_, float scale_)
        {
            if (!_hovered) return;
            _hovered = false;
            _card.Place(transform.position, transform.rotation, order_);
            _card.ScaleToAsync(scale_, _hoverDuration).Forget();
        }

        #endregion

        // 离手 / 被拖走 / 入场中等不还原场景：只清状态，把层序与尺寸交给后续补间
        public void ClearHover() => _hovered = false;

        // 悬停中被池化回收时同样只清状态，防实例复用后 EnterHover 被早退挡掉
        private void OnDisable() => ClearHover();
    }
}
