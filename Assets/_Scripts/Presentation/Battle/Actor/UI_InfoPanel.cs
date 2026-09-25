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
    /// 状态槽位按状态类型去重挂在 PerkGroup 下（HorizontalLayoutGroup 自动排布），悬停槽位在其图标上方弹复用的 UI_PerkPopTip。</summary>
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

        [Tooltip("详情提示与图标顶沿的间距（画布像素）")] [SerializeField]
        private float _perkTipLiftPx = 12f;

        // AC_BlockFrame 的显隐参数：Bool，true 播 BlockFrameShow、false 播 BlockFrameHide；缓存哈希避免字符串查找
        private static readonly int BlockShow = Animator.StringToHash("Show");

        private Creature _creature;
        private Image _fillImage;
        private Tween _healthTween;
        private int _shownBlock = -1;
        private readonly Dictionary<Type, UI_PerkSlot> _perkSlots = new();
        private UI_PerkSlot _perkSlotPrefab;
        private UI_PerkPopTip _perkPopTipPrefab;
        private UI_PerkPopTip _perkPopTip;

        #region 对外写入入口（UI_Actor 驱动）

        // 初始化：装配期调用——缓存 Fill、按域真值直写血量与格挡，不播补间
        public void Init(Creature creature_, UI_PerkSlot perkSlotPrefab_, UI_PerkPopTip perkPopTipPrefab_)
        {
            _creature = creature_;
            _perkSlotPrefab = perkSlotPrefab_;
            _perkPopTipPrefab = perkPopTipPrefab_;
            _fillImage = _healthSlider.fillRect ? _healthSlider.fillRect.GetComponent<Image>() : null;
            _healthSlider.maxValue = _creature.MaxHp;
            SetHealth(_creature.CurrentHp, false);
            SetBlock(_creature.Block);
        }

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

        // 格挡：数值恒写域真值；显隐交给 AC_BlockFrame 的 Show 布尔，值没变就一个字都不写
        public void SetBlock(int block_)
        {
            if (_shownBlock == block_) return;
            _shownBlock = block_;
            _blockAmount.text = block_ > 0 ? block_.ToString() : "";
            _anim.SetBool(BlockShow, block_ > 0);
        }

        // 施加状态：按状态类型建槽；同状态重复施加（槽位已存在）只刷新层数显示，图标与绑定不变
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

        // 状态层数递减：按类型找到槽位刷新显示（事件携带的是挂载实例，与槽位绑定同源）
        public void RefreshPerkLayers(BattlePerk perk_)
        {
            if (_perkSlots.TryGetValue(perk_.GetType(), out UI_PerkSlot slot)) slot.RefreshLayers();
        }

        // 移除状态：按状态类型找到槽位销毁（事件携带的是挂载实例，与槽位绑定的原型不同源，只能按类型对号）；
        // 弹着的详情提示一并收掉，防它还指向已消失的状态
        public void RemovePerk(BattlePerk perk_)
        {
            if (!_perkSlots.Remove(perk_.GetType(), out UI_PerkSlot slot)) return;
            HidePerkTip();
            Destroy(slot.gameObject);
        }

        // 弹详情：内容写好、重建一次布局拿到真实高度后再定位到图标上方，显示时置顶保证盖过其他单位
        public void ShowPerkTip(UI_PerkSlot slot_)
        {
            EnsurePerkTip();
            _perkPopTip.SetContent(slot_.Perk);
            _perkPopTip.gameObject.SetActive(true);
            _perkPopTip.transform.SetAsLastSibling();
            RectTransform tipRect = (RectTransform)_perkPopTip.transform;
            LayoutRebuilder.ForceRebuildLayoutImmediate(tipRect);
            tipRect.position = slot_.TipAnchorPosition() + tipRect.parent.up * _perkTipLiftPx;
        }

        public void HidePerkTip()
        {
            if (_perkPopTip) _perkPopTip.gameObject.SetActive(false);
        }

        #endregion

        // 懒生成唯一一个详情提示实例：挂在信息面板所在的 Canvas 下复用，隐藏只是关激活，下一次弹出直接换内容
        private void EnsurePerkTip()
        {
            if (_perkPopTip) return;
            _perkPopTip = Instantiate(_perkPopTipPrefab, GetComponentInParent<Canvas>().transform);
            ((RectTransform)_perkPopTip.transform).pivot = new Vector2(0.5f, 0f); // 底沿中点对准定位点：高度随内容变化只向上生长
            _perkPopTip.gameObject.SetActive(false);
        }

        // 血条的可视三件套：Slider 值、滚动文本与按血量占比从色带取的 Fill 颜色，补间与直写共用
        private void WriteHealthVisual(float value_)
        {
            _healthSlider.value = value_;
            _healthText.text = $"{Mathf.RoundToInt(value_)}/{_creature.MaxHp}";
            if (_fillImage) _fillImage.color = _gradient.Evaluate(value_ / _creature.MaxHp);
        }

        private void OnDestroy() => _healthTween?.Kill();
    }
}