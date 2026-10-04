using System;
using System.Collections.Generic;
using _DreamKeep.Presentation.Intent;
using _DreamKeep.RuntimeData;
using _DreamKeep.Utility;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace _DreamKeep.Presentation
{
    /// <summary>敌人意图预告组：把展示意图翻译成头顶图标串并承接悬停说明——
    /// 图标生成后分发给各具体意图脚本做视觉绑定；悬停整组时当前全部意图各弹一条说明（复合意图展开成两条），移出收起；
    /// 悬停判定由组本身承担（子图标不参与交互），刷新归持有它的敌人视图 UI_Enemy，自己不订阅域事件。</summary>
    public class UI_EnemyIntentGroup : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
    {
        [Header("意图预制体")] [SerializeField] private RectTransform _attackIntentPrefab;
        [SerializeField] private RectTransform _blockIntentPrefab;
        [SerializeField] private RectTransform _healIntentPrefab;
        [SerializeField] private RectTransform _moveIntentPrefab;
        [SerializeField] private RectTransform _applyPerkIntentPrefab;

        [Tooltip("描述气泡底沿与图标集合顶沿的间距（画布像素）")] [SerializeField]
        private float _intentTipLiftPx = 1f;

        private UI_PopTextRoot PopTextRoot
        {
            get
            {
                if (_popTextRoot) return _popTextRoot;
                _popTextRoot = FindFirstObjectByType<UI_PopTextRoot>();
                return _popTextRoot ? _popTextRoot : throw new Exception("UI_EnemyIntentGroup 找不到 PopTextRoot 实例");
            }
        }

        private UI_PopTextRoot _popTextRoot;
        private readonly List<EnemyIntent> _shownIntents = new();                    // 当前展示意图（复合已展平），悬停时逐条出说明
        private readonly List<(string Name, string Description)> _tipItems = new();
        private readonly List<RectTransform> _icons = new();                         // 已生成图标，供锚点算包围盒
        private bool _tipsShown;

        #region 对外展示：按展示意图整组重建头顶图标

        /// <summary>重建图标组：清空旧图标后按意图类型递归生成并分发绑定，null 即清空；悬停中重建会同步刷新说明。</summary>
        public void Show(EnemyIntent intent_)
        {
            Clear();
            if (intent_ != null) Append(intent_);
            if (_tipsShown) ShowTips();
        }

        #endregion

        public void OnPointerEnter(PointerEventData eventData_) => ShowTips();

        public void OnPointerExit(PointerEventData eventData_) => HideTips();

        private void OnDisable() => HideTips(); // 敌人销毁/面板关激活时 Exit 不来，兜底收掉悬着的气泡

        // 递归展开：复合意图并排放两条子意图，其余按类型生成图标、交给对应脚本绑定、并记入展示意图列表
        private void Append(EnemyIntent intent_)
        {
            switch (intent_)
            {
                case EI_Mixed mixed:
                    if (mixed.First != null) Append(mixed.First);
                    if (mixed.Second != null) Append(mixed.Second);
                    break;
                case EI_Damage damage:
                    Spawn(_attackIntentPrefab)?.GetComponent<UI_AttackIntent>().Bind(damage);
                    _shownIntents.Add(damage);
                    break;
                case EI_Block block:
                    Spawn(_blockIntentPrefab);
                    _shownIntents.Add(block);
                    break;
                case EI_Heal heal:
                    Spawn(_healIntentPrefab);
                    _shownIntents.Add(heal);
                    break;
                case EI_Move move:
                    Spawn(_moveIntentPrefab);
                    _shownIntents.Add(move);
                    break;
                case EI_ApplyPerk applyPerk:
                    Spawn(_applyPerkIntentPrefab)?.GetComponent<UI_ApplyPerkIntent>().Bind(applyPerk);
                    _shownIntents.Add(applyPerk);
                    break;
            }
        }

        // 说明条目：意图没有标题，描述直接给容器（标题栏由气泡自行收起）；没有意图可讲就顺手收起
        private void ShowTips()
        {
            _tipItems.Clear();
            foreach (EnemyIntent intent in _shownIntents) _tipItems.Add((null, intent.GetDescription()));
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
            if (_popTextRoot) _popTextRoot.DescriptionTipGroup.Hide(this);
        }

        // 锚点：所有图标的世界包围盒顶沿中点，再抬 lift（画布像素按 lossyScale 换算成世界距离），包围盒计算交给 UiGeometry；
        // 先强制重排一次，防刚生成的图标在同一帧还没被布局组摆好
        private Vector3 TipAnchor()
        {
            LayoutRebuilder.ForceRebuildLayoutImmediate((RectTransform)transform);
            return UiGeometry.BoundingTopCenter(_icons, _intentTipLiftPx * ((RectTransform)transform).lossyScale.y);
        }

        private RectTransform Spawn(RectTransform prefab_)
        {
            if (prefab_ == null) return null;
            RectTransform icon = Instantiate(prefab_, transform);
            _icons.Add(icon);
            return icon;
        }

        private void Clear()
        {
            for (int i = transform.childCount - 1; i >= 0; i--) Destroy(transform.GetChild(i).gameObject);
            _icons.Clear();
            _shownIntents.Clear();
        }
    }
}
