using System;
using System.Collections.Generic;
using _DreamKeep.ConfigData;
using UnityEngine;

namespace _DreamKeep.Presentation
{
    /// <summary>状态槽位组：状态按类型去重挂在自己名下（格子布局自动排布），负责建槽、刷层与销槽；
    /// 悬停说明由每个槽位自己承接（悬停哪一格弹哪一格），组只把气泡宿主交给新建的槽位；
    /// 只认 UI_Actor 转发来的域真值，自己不订事件不判规则。</summary>
    public class UI_PerkGroup : MonoBehaviour
    {
        [SerializeField] private UI_PerkSlot _perkSlotPrefab;

        private readonly Dictionary<Type, UI_PerkSlot> _perkSlots = new();
        private UI_PopTextRoot _popTextRoot;

        private UI_PopTextRoot PopTextRoot
        {
            get
            {
                if (_popTextRoot) return _popTextRoot;
                _popTextRoot = FindFirstObjectByType<UI_PopTextRoot>();
                return _popTextRoot ? _popTextRoot : throw new Exception("UI_PerkGroup 找不到 PopTextRoot 实例");
            }
        }

        #region 施加效果

        // 施加效果：按效果类型建槽；同状态重复施加（槽位已存在）只刷新层数显示，图标与绑定不变
        public void AddPerk(BattlePerk perk_)
        {
            Type type = perk_.GetType();
            if (_perkSlots.TryGetValue(type, out UI_PerkSlot slot))
            {
                slot.RefreshLayers();
                return;
            }

            slot = Instantiate(_perkSlotPrefab, transform);
            slot.Initialize(PopTextRoot);
            slot.Bind(perk_);
            _perkSlots[type] = slot;
        }

        // 效果层数递减：按类型找到槽位刷新显示（事件携带的是挂载实例，与槽位绑定同源）
        public void RefreshPerkLayers(BattlePerk perk_)
        {
            if (_perkSlots.TryGetValue(perk_.GetType(), out UI_PerkSlot slot)) slot.RefreshLayers();
        }

        // 移除效果：按效果类型找到槽位销毁（事件携带的是挂载实例，与槽位绑定的原型不同源，只能按类型对号）
        public void RemovePerk(BattlePerk perk_)
        {
            if (_perkSlots.Remove(perk_.GetType(), out UI_PerkSlot slot)) Destroy(slot.gameObject);
        }

        #endregion
    }
}
