using Cysharp.Threading.Tasks;
using UnityEngine;

namespace _DreamKeep.Presentation
{
    /// <summary>卡牌悬停能力：把这张牌"拎起来"端详——向上抬起、旋转回正、放大并抬到最上层，退出时按宿主给的槽位位置、朝向、缩放与层序原位还原；
    /// 位置、朝向、缩放、层序四项在悬停期间全归它独占，排布重排会跳过这张牌，正在看的牌不会被旁边的牌让位拽走。</summary>
    /// 由手牌宿主统一裁决驱动——宿主逐帧裁定指针下唯一一张悬停牌并调用 EnterHover/ExitHover，同一时刻全局至多一张牌处于悬停，重叠手牌不会互相争抢。
    /// 还原值一律由宿主按手牌槽位真源给出，组件不记进入前的瞬时值，补间途中进出也不会写回半截姿态；有该组件才有该效果，未配该组件的卡不会被驱动。</summary>
    [RequireComponent(typeof(Obj_Card))]
    public class Comp_CardHover : MonoBehaviour, ICardAbility
    {
        [Tooltip("悬停时的层序：需压过手牌与出牌区排队牌，但低于拖拽层序（Comp_CardDrag 的 100），保证抓在手里的牌永远在最上层")]
        [SerializeField] private int _hoverOrder = 90;

        [Tooltip("悬停时的缩放目标值（绝对值而非倍率）：手牌基准尺寸为 1，1.15 即比常态放大 15%")]
        [SerializeField] private float _hoverScale = 1.15f;

        [Tooltip("悬停时向上抬起的距离（世界单位）：抬高才不会被相邻手牌压住；上移量不得超过 卡半高 ×（悬停缩放 − 手牌槽位缩放），否则指针擦着卡的下边缘进入时会被甩出碰撞体、反复进出地抖")]
        [SerializeField] private float _hoverLiftY = 0.3f;

        [Tooltip("悬停进出的补间时长（秒）：抬起、回正、放大与回位共用，比手牌重排的 0.28s 短，进出要跟手")]
        [SerializeField] private float _hoverDuration = 0.12f;

        private Obj_Card _card;
        private bool _hovered;

        // 认领卡根；悬停不改朝向基准，无需相机与世界坐标换算
        public void Attach(Obj_Card card_) => _card = card_;

        #region 悬停进出：宿主裁定出唯一悬停牌后调用，进入拎起，退出按槽位真源原位还原

        // 进入：从当前位置（即槽位）抬起并回正，同时抬层放大；位置补间带层序，层序自首帧起就是悬停层序
        public void EnterHover()
        {
            if (_hovered) return;
            _hovered = true;
            _card.MoveToAsync(transform.position + Vector3.up * _hoverLiftY, Quaternion.identity, _hoverOrder,
                _hoverDuration).Forget();
            _card.ScaleToAsync(_hoverScale, _hoverDuration).Forget();
        }

        // 退出：按宿主给的槽位位置、朝向、层序与缩放一次还原四项，与进入时的瞬时状态无关，补间途中进出也不会写回半截姿态
        public void ExitHover(Vector3 position_, Quaternion rotation_, int order_, float scale_)
        {
            if (!_hovered) return;
            _hovered = false;
            _card.MoveToAsync(position_, rotation_, order_, _hoverDuration).Forget();
            _card.ScaleToAsync(scale_, _hoverDuration).Forget();
        }

        #endregion

        // 离手 / 被拖走 / 入场中等不还原场景：只清状态，把姿态交给后续补间
        public void ClearHover() => _hovered = false;

        // 悬停中被池化回收时同样只清状态，防实例复用后 EnterHover 被早退挡掉
        private void OnDisable() => ClearHover();
    }
}
