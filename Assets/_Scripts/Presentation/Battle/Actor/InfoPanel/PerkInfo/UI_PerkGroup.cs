using System;
using System.Collections.Generic;
using _DreamKeep.ConfigData;
using _DreamKeep.Utility;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace _DreamKeep.Presentation
{
    /// <summary>状态槽位组：状态按类型去重挂在自己名下（格子布局自动排布）；
    /// 悬停整组时把全部状态的说明一次性弹成一列气泡，移出收起，悬停期间层数变化只改写对应气泡；
    /// 悬停判定由组本身承担（子槽位不参与交互），只认 UI_Actor 转发来的域真值，自己不订事件不判规则。</summary>
    public class UI_PerkGroup : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
    {
        [Header("Perk管理")] 
        [SerializeField] private UI_PerkSlot _perkSlotPrefab;

        [Tooltip("描述气泡底沿与图标集合顶沿的间距（画布像素）")] [SerializeField]
        private float _perkTipLiftPx = 1f;

        private UI_PopTextRoot PopTextRoot
        {
            get
            {
                if (_popTextRoot) return _popTextRoot;
                _popTextRoot = FindFirstObjectByType<UI_PopTextRoot>();
                return _popTextRoot ? _popTextRoot : throw new Exception("UI_PerkGroup 找不到 PopTextRoot 实例");
            }
        }

        private UI_PopTextRoot _popTextRoot;

        private readonly Dictionary<Type, UI_PerkSlot> _perkSlots = new();
        private readonly List<UI_PerkSlot> _slotOrder = new();                      // 槽位创建序＝格子布局的显示序，气泡条目按它排列
        private readonly List<(string Name, string Description)> _tipItems = new(); // 悬停中展示的条目快照，供层数刷新按序号改写
        private readonly List<UI_PerkSlot> _tipSlots = new();                       // 与 _tipItems 同序：气泡序号 → 槽位
        private bool _tipsShown;

        #region 施加效果

        // 施加效果：按效果类型建槽；同状态重复施加（槽位已存在）只刷新层数显示，图标与绑定不变
        public void AddPerk(BattlePerk perk_)
        {
            Type type = perk_.GetType();
            if (_perkSlots.TryGetValue(type, out UI_PerkSlot slot))
            {
                slot.RefreshLayers();
                if (_tipsShown) SetTipOf(slot);
                return;
            }

            slot = Instantiate(_perkSlotPrefab,transform);
            slot.Bind(perk_);
            _perkSlots[type] = slot;
            _slotOrder.Add(slot);
            if (_tipsShown) ShowTips(); // 多一格是结构变化：悬停中整组重排
        }

        // 效果层数递减：按类型找到槽位刷新显示（事件携带的是挂载实例，与槽位绑定同源）；
        // 气泡正开着时同步改写对应条目，防悬停期间回合衰减后气泡还挂着旧层数
        public void RefreshPerkLayers(BattlePerk perk_)
        {
            if (!_perkSlots.TryGetValue(perk_.GetType(), out UI_PerkSlot slot)) return;
            slot.RefreshLayers();
            if (_tipsShown) SetTipOf(slot);
        }

        // 移除效果：按效果类型找到槽位销毁（事件携带的是挂载实例，与槽位绑定的原型不同源，只能按类型对号）；
        // 气泡正开着时整组重排甚至收掉，防它还指向已消失的状态
        public void RemovePerk(BattlePerk perk_)
        {
            if (!_perkSlots.Remove(perk_.GetType(), out UI_PerkSlot slot)) return;
            _slotOrder.Remove(slot);
            Destroy(slot.gameObject);
            if (!_tipsShown) return;
            if (_slotOrder.Count == 0) HideTips();
            else ShowTips();
        }

        #endregion

        public void OnPointerEnter(PointerEventData eventData_) => ShowTips();

        public void OnPointerExit(PointerEventData eventData_) => HideTips();

        private void OnDisable() => HideTips(); // 面板收起等被动关激活时 Exit 不来，兜底收掉悬着的气泡

        // 组装全部槽位的条目与锚点交给容器：一组条目一次弹出，锚点＝图标集合包围盒顶沿中点 + lift
        private void ShowTips()
        {
            _tipItems.Clear();
            _tipSlots.Clear();
            foreach (UI_PerkSlot slot in _slotOrder)
            {
                _tipItems.Add((slot.Perk.Name, slot.Perk.GetDescription()));
                _tipSlots.Add(slot);
            }

            if (_tipItems.Count == 0)
            {
                HideTips();
                return;
            }

            _tipsShown = true;
            PopTextRoot.DescriptionTipGroup.Show(this, _tipItems, TipAnchor());
        }

        private void HideTips()
        {
            if (!_tipsShown) return;
            _tipsShown = false;
            _tipItems.Clear();
            _tipSlots.Clear();
            if (_popTextRoot) _popTextRoot.DescriptionTipGroup.Hide(this);
        }

        // 悬停中某格内容变了：只改写对应序号的气泡，不整组重建
        private void SetTipOf(UI_PerkSlot slot_)
        {
            int index = _tipSlots.IndexOf(slot_);
            if (index < 0) return;
            _tipItems[index] = (slot_.Perk.Name, slot_.Perk.GetDescription());
            PopTextRoot.DescriptionTipGroup.SetItem(index, _tipItems[index].Name, _tipItems[index].Description);
        }

        // 锚点：所有槽位图标的世界包围盒顶沿中点，再抬 lift（画布像素按 lossyScale 换算成世界距离），包围盒计算交给 UiGeometry；
        // 先强制重排一次，防刚新建的槽位在同一帧还没被布局组摆好
        private Vector3 TipAnchor()
        {
            LayoutRebuilder.ForceRebuildLayoutImmediate((RectTransform)transform);
            return UiGeometry.BoundingTopCenter(_tipSlots, _perkTipLiftPx * ((RectTransform)transform).lossyScale.y);
        }
    }
}
