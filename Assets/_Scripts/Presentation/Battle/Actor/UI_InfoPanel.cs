using System;
using System.Collections.Generic;
using _DreamKeep.ConfigData;
using _DreamKeep.RuntimeData;
using DG.Tweening;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace _DreamKeep.Presentation
{
    /// <summary>战斗单位信息面板：血条、格挡与状态槽位显示的唯一写入点，只认 UI_Actor 转发来的域真值，自己不订事件不判规则。
    /// 血条走 DOTween 单通道补间（数值滚动 + 按 _gradient 染 Fill），格挡由 AC_BlockFrame 的 Show 布尔驱动显隐，
    /// 状态槽位按状态类型去重挂在 PerkGroup 下（HorizontalLayoutGroup 自动排布），悬停槽位在其图标上方弹复用的 UI_DescriptionPopTip（挂场景 Canvas_Pop/PopTextRoot 下）。</summary>
    public class UI_InfoPanel : MonoBehaviour
    {
        [Header("血条")] [SerializeField] private Gradient _gradient;
        [SerializeField] private Slider _healthSlider;
        [SerializeField] private TextMeshProUGUI _healthText;

        [Tooltip("血量补间时长（秒）：受伤与回血的滚动速度")] [SerializeField]
        private float _healthTweenDuration = 0.25f;

        [Header("格挡")] [SerializeField] private Animator _anim;
        [SerializeField] private TextMeshProUGUI _blockAmount;

        [Header("Perk管理")] [SerializeField] private GameObject _perkGroupRoot;
        [SerializeField] private UI_PerkSlot _perkSlotPrefab;
        [SerializeField] private UI_DescriptionPopTip _descriptionPopTipPrefab;

        [Tooltip("详情提示与图标顶沿的间距（画布像素）")] [SerializeField]
        private float _perkTipLiftPx = 1f;

        private UI_PopTextRoot PopTextRoot
        {
            get
            {
                if (_popTextRoot) return _popTextRoot;
                _popTextRoot = FindFirstObjectByType<UI_PopTextRoot>();
                return _popTextRoot ? _popTextRoot : throw new Exception("UI_InfoPanel 找不到 PopTextRoot 实例");
            }
        }

        private UI_PopTextRoot _popTextRoot;

        private Creature _creature;
        private Image _fillImage;
        private Tween _healthTween;
        private int _shownBlock = -1;
        private readonly Dictionary<Type, UI_PerkSlot> _perkSlots = new();
        private UI_DescriptionPopTip _descriptionPopTip;
        private UI_PerkSlot _shownTipSlot;


        // 初始化：装配期调用——缓存 Fill、按域真值直写血量与格挡，不播补间
        public void Init(Creature creature_)
        {
            _creature = creature_;
            _fillImage = _healthSlider.fillRect ? _healthSlider.fillRect.GetComponent<Image>() : null;
            _healthSlider.maxValue = _creature.MaxHp;
            SetHealth(_creature.CurrentHp, false);
            SetBlock(_creature.Block);
        }

        private void OnDestroy() => _healthTween?.Kill();

        #region 设置血量

        // 血量：animate_ 为 false 直写（开局初值），为 true 且值有变才起补间；补间途中再来一次则 Kill 旧的从当前值续走
        public void SetHealth(int current_, bool animate_)
        {
            int current = Mathf.Clamp(current_, 0, _creature.MaxHp);
            _healthTween?.Kill();
            if (animate_ && !Mathf.Approximately(_healthSlider.value, current))
            {
                _healthTween = DOTween.To(() => _healthSlider.value, WriteHealthVisual, current, _healthTweenDuration)
                    .SetEase(Ease.OutQuad);
                return;
            }

            WriteHealthVisual(current);
        }

        // 血条的可视三件套：Slider 值、滚动文本与按血量占比从色带取的 Fill 颜色，补间与直写共用
        private void WriteHealthVisual(float value_)
        {
            _healthSlider.value = value_;
            _healthText.text = $"{Mathf.RoundToInt(value_)}/{_creature.MaxHp}";
            if (_fillImage) _fillImage.color = _gradient.Evaluate(value_ / _creature.MaxHp);
        }

        #endregion

        #region 设置格挡

        // 格挡：数值恒写域真值；显隐交给 AC_BlockFrame 的 Show 布尔，值没变就一个字都不写
        public void SetBlock(int block_)
        {
            if (_shownBlock == block_) return;
            _shownBlock = block_;
            _blockAmount.text = block_ > 0 ? block_.ToString() : "";
            _anim.SetBool(Animator.StringToHash("Show"), block_ > 0);
        }

        #endregion

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

            slot = Instantiate(_perkSlotPrefab, _perkGroupRoot.transform);
            slot.SetHost(this);
            slot.Bind(perk_);
            _perkSlots[type] = slot;
        }

        // 效果层数递减：按类型找到槽位刷新显示（事件携带的是挂载实例，与槽位绑定同源）；
        // 弹窗正开着且显示的正是这个槽位时同步重写弹窗，防悬停期间回合衰减后弹窗还挂着旧层数
        public void RefreshPerkLayers(BattlePerk perk_)
        {
            if (!_perkSlots.TryGetValue(perk_.GetType(), out UI_PerkSlot slot)) return;
            slot.RefreshLayers();
            if (_descriptionPopTip && _descriptionPopTip.gameObject.activeSelf && _shownTipSlot == slot)
                WriteTipContent(slot);
        }

        // 移除效果：按效果类型找到槽位销毁（事件携带的是挂载实例，与槽位绑定的原型不同源，只能按类型对号）；
        // 弹着的详情提示一并收掉，防它还指向已消失的状态
        public void RemovePerk(BattlePerk perk_)
        {
            if (!_perkSlots.Remove(perk_.GetType(), out UI_PerkSlot slot)) return;
            HidePerkTip();
            Destroy(slot.gameObject);
        }

        #endregion

        #region 效果提示弹窗

        // 弹效果详情：单实例复用——有则换内容重新定位，无则懒生成到 Canvas_Pop/PopTextRoot（按 UI_PopTextRoot 标记查找，
        // 场景里没有标记则回退自身所在 Canvas）；记录显示中的槽位，供悬停期间层数变化时同步刷新
        public void ShowPerkTip(UI_PerkSlot slot_)
        {
            if (!_descriptionPopTip)
            {
                _descriptionPopTip = Instantiate(_descriptionPopTipPrefab, PopTextRoot.transform);
                ((RectTransform)_descriptionPopTip.transform).pivot = new Vector2(0.5f, 0f); // 底沿中点对准定位点：高度随内容变化只向上生长
            }

            _shownTipSlot = slot_;
            WriteTipContent(slot_);
        }

        // 弹层内容与定位的统一写入口：悬停进入与悬停期间层数变化共用；先收起再写内容，重建一次布局拿到真实高度后定位到图标上方，显示时置顶保证盖过其他单位
        private void WriteTipContent(UI_PerkSlot slot_)
        {
            _descriptionPopTip.gameObject.SetActive(false);
            _descriptionPopTip.SetContent(slot_.Perk.Name, slot_.Perk.GetDescription());
            _descriptionPopTip.gameObject.SetActive(true);
            _descriptionPopTip.transform.SetAsLastSibling();
            RectTransform tipRect = (RectTransform)_descriptionPopTip.transform;
            LayoutRebuilder.ForceRebuildLayoutImmediate(tipRect);
            tipRect.position = slot_.TipAnchorPosition() + tipRect.parent.up * _perkTipLiftPx;
        }

        public void HidePerkTip()
        {
            if (_descriptionPopTip)
                _descriptionPopTip.gameObject.SetActive(false);
        }

        #endregion
    }
}