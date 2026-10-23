using System;
using System.Collections.Generic;
using _DreamKeep.RuntimeData;
using _DreamKeep.EngineUtility;
using UnityEngine;
using VContainer;
using VContainer.Unity; // InjectGameObject 扩展方法住在这里

namespace _DreamKeep.Presentation
{
    /// <summary>状态槽位组：状态按类型去重挂在自己名下（格子布局自动排布），负责取槽、刷层与归槽；
    /// 悬停说明由每个槽位自己承接（悬停哪一格弹哪一格，气泡内容在槽位 Bind 时喂给它的气泡发起器）；
    /// 只认 UI_Actor 转发来的域真值，自己不订事件不判规则。</summary>
    public sealed class UI_PerkGroup : MonoBehaviour
    {
        [SerializeField] private UI_PerkSlot _perkSlotPrefab;

        private readonly Dictionary<Type, UI_PerkSlot> _perkSlots = new();
        private MonoObjectPool<UI_PerkSlot> _slotPool;
        private IObjectResolver _resolver;

        // 池化槽位只在新建时补一次容器注入（槽位上的气泡发起器等依赖），复用实例不重复注入；
        // Awake 早于 Construct，闭包里读字段而不是捕获局部变量
        private void Awake() => _slotPool = new MonoObjectPool<UI_PerkSlot>(transform,
            onCreate_: slot_ => _resolver.InjectGameObject(slot_.gameObject));

        [Inject]
        public void Construct(IObjectResolver resolver_) => _resolver = resolver_;

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

            slot = _slotPool.Get(_perkSlotPrefab);
            slot.Bind(perk_);
            _perkSlots[type] = slot;
        }

        // 效果层数递减：按类型找到槽位刷新显示（事件携带的是挂载实例，与槽位绑定同源）
        public void RefreshPerkLayers(BattlePerk perk_)
        {
            if (_perkSlots.TryGetValue(perk_.GetType(), out UI_PerkSlot slot)) slot.RefreshLayers();
        }

        // 移除效果：按效果类型找到槽位归还池（归还即失活，槽位自己的气泡发起器顺带收掉悬着的气泡）；
        // 事件携带的是挂载实例，与槽位绑定的原型不同源，只能按类型对号
        public void RemovePerk(BattlePerk perk_)
        {
            if (_perkSlots.Remove(perk_.GetType(), out UI_PerkSlot slot)) _slotPool.Release(slot);
        }

        #endregion
    }
}
