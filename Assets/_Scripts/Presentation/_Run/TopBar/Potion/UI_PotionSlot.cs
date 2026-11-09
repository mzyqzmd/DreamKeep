using System;
using _DreamKeep.CommonPresentation;
using _DreamKeep.ConfigData;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace _DreamKeep.Presentation
{
    /// <summary>药水槽位：显示一瓶药水的图标，悬停气泡给出名称与描述；空槽显示预制体上摆的占位图与通用说明。
    /// 点击只广播自己的下标，路由归药水栏（槽位不反向依赖栏，与 UI_RelicSlot 同款）。</summary>
    public sealed class UI_PotionSlot : MonoBehaviour, IPointerClickHandler
    {
        [Tooltip("空槽悬停说明的标题")] [SerializeField]
        private string _emptyTipName = "药水栏位";

        [Tooltip("空槽悬停说明的正文")] [SerializeField]
        private string _emptyTipDescription = "在战斗中使用药水可以\n获得<color=#7BD37B>强化</color>" +
                                              "或<color=#E05B5B>阻碍</color>敌人。";

        private Image _icon;
        private UI_HoverTip _hoverTip;
        private Sprite _emptySprite; // 占位图：第一次改写图标前从预制体上取一次
        private int _slotIndex;

        /// <summary>点击播报：只交出自己的下标，点空槽与否由药水栏按当前数据判断。</summary>
        public event Action<int> Clicked;

        // 下标由药水栏在收集子槽位时写入：槽位只回答"我被点了"，不认栏
        internal void SetSlotIndex(int index_) => _slotIndex = index_;

        #region 绑定药水与清空

        public void Bind(SO_Potion potion_)
        {
            EnsureRefs();
            if (!_emptySprite) _emptySprite = _icon.sprite;
            _icon.sprite = potion_.Icon;
            _hoverTip.SetContents(potion_.PotionName, potion_.Description);
        }

        public void SetEmpty()
        {
            EnsureRefs();
            if (!_emptySprite) _emptySprite = _icon.sprite;
            _icon.sprite = _emptySprite;
            _hoverTip.SetContents(_emptyTipName, _emptyTipDescription);
        }

        #endregion

        /// <summary>临时关闭 / 恢复本槽的悬停说明：药水菜单打开期间关掉，关闭后由它恢复（指针还在槽位就立刻补弹）。</summary>
        internal void SuppressTip(bool suppressed_)
        {
            EnsureRefs();
            _hoverTip.SetSuppressed(suppressed_);
        }

        public void OnPointerClick(PointerEventData eventData_) => Clicked?.Invoke(_slotIndex);

        // 两个同节点组件自愈取值：预制体挂上即用，无需 Inspector 接线（装配早于 Awake 时同样可用）
        private void EnsureRefs()
        {
            if (!_icon) _icon = GetComponent<Image>();
            if (!_hoverTip) _hoverTip = GetComponent<UI_HoverTip>();
        }
    }
}