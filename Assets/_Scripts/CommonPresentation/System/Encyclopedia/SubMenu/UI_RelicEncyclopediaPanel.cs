using System;
using System.Collections.Generic;
using VContainer;
using _DreamKeep.ConfigData;
using _DreamKeep.EngineUtility;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace _DreamKeep.CommonPresentation
{
    /// <summary>遗物收集页：初始 / 普通 / 稀有 / 商店四组各挂一份遗物池，空组连标题隐藏；
    /// 先古组按 SO_Ancient[] 每位先古之民生成一个子组，名号与头像取自配置、遗物取自其专属池；
    /// 槽位池化陈列，左键点槽位开页面内的遗物信息面板，纯展示不写域；
    /// 本页返回键自持，点击只上报返回意图，开合编排归根面板。</summary>
    public sealed class UI_RelicEncyclopediaPanel : UI_Panel
    {
        [Header("常规分组")] [SerializeField] private GameObject[] _groupRoots;
        [SerializeField] private RectTransform[] _groupContents;
        [SerializeField] private SO_RelicPool[] _groupPools;
        [Header("先古分组")] [SerializeField] private SO_Ancient[] _ancients;
        [SerializeField] private RectTransform _ancientContainer;
        [SerializeField] private RectTransform _ancientSubGroupTemplate;
        [Header("槽位与详情")] [SerializeField] private UI_RelicSlot _relicSlotPrefab;
        [SerializeField] private UI_RelicsInfoPanel _relicInfoPanel;
        [Header("返回")] [Tooltip("本页返回键：点击只上报返回意图")] [SerializeField]
        private Button _closeButton;

        public event Action CloseRequested;

        private readonly List<UI_RelicSlot> _shown = new();
        private readonly List<RectTransform> _shownSubGroups = new();
        private MonoObjectPool<UI_RelicSlot> _slotPool;
        private MonoObjectPool<RectTransform> _subGroupPool;
        private PopFactory _popFactory;

        [Inject]
        public void Construct(PopFactory popFactory_) => _popFactory = popFactory_;

        private void Awake()
        {
            _slotPool = new MonoObjectPool<UI_RelicSlot>(transform, 64, onCreate_: slot_ =>
            {
                slot_.GetComponent<UI_HoverTip>().Construct(_popFactory);
                slot_.Clicked += OnRelicClicked;
            });
            _ancientSubGroupTemplate.gameObject.SetActive(false); // 场景里的 Neow 节点只作模板
            _subGroupPool = new MonoObjectPool<RectTransform>(_ancientContainer, 8);
            _closeButton.onClick.AddListener(() => CloseRequested?.Invoke());
        }

        private void OnDisable()
        {
            if (_relicInfoPanel) _relicInfoPanel.gameObject.SetActive(false);
            ReturnAll();
        }

        protected override void OnShown() => Refresh();

        protected override void OnHidden() => ReturnAll();

        #region 陈列：常规分组 → 先古子组 → 槽位

        private void Refresh()
        {
            ReturnAll();
            for (int i = 0; i < _groupPools.Length; i++)
            {
                SO_RelicPool pool = _groupPools[i];
                bool has = pool && pool.Relics.Count > 0;
                _groupRoots[i].SetActive(has);
                if (!has) continue;
                IReadOnlyList<SO_Relic> relics = pool.Relics;
                for (int j = 0; j < relics.Count; j++)
                    if (relics[j]) SpawnSlot(_groupContents[i], relics[j], j);
            }

            RefreshAncients();
        }

        private void RefreshAncients()
        {
            ReturnSubGroups();
            bool any = false;
            if (_ancients != null)
                foreach (SO_Ancient ancient in _ancients)
                    if (ancient && ancient.RelicPool && ancient.RelicPool.Relics.Count > 0)
                    {
                        any = true;
                        break;
                    }

            _ancientContainer.gameObject.SetActive(any);
            if (!any) return;
            for (int i = 0; i < _ancients.Length; i++)
            {
                SO_Ancient ancient = _ancients[i];
                if (!ancient || !ancient.RelicPool) continue;
                RectTransform sub = _subGroupPool.Get(_ancientSubGroupTemplate);
                sub.SetSiblingIndex(i + 1); // Title 占第 0 位，子组依次往后排
                sub.Find("Title/Icon").GetComponent<Image>().sprite = ancient.AncientView.Portrait;
                sub.Find("Title/Text (TMP)").GetComponent<TextMeshProUGUI>().text = ancient.AncientName;
                RectTransform group = sub.Find("Group") as RectTransform;
                IReadOnlyList<SO_Relic> relics = ancient.RelicPool.Relics;
                for (int j = 0; j < relics.Count; j++)
                    if (relics[j]) SpawnSlot(group, relics[j], j);
                _shownSubGroups.Add(sub);
            }
        }

        private void SpawnSlot(RectTransform group_, SO_Relic relic_, int index_)
        {
            UI_RelicSlot slot = _slotPool.Get(_relicSlotPrefab);
            slot.transform.SetParent(group_, false);
            slot.transform.SetSiblingIndex(index_); // GridLayoutGroup 按兄弟序摆格，池复用不改兄弟位
            slot.Bind(relic_);
            _shown.Add(slot);
        }

        private void OnRelicClicked(SO_Relic relic_, bool exhausted_) => _relicInfoPanel.Show(relic_, exhausted_);

        // HideOverlays 可能在 Awake 之前调 Hide，故容忍池未建
        private void ReturnAll()
        {
            ReturnSubGroups();
            if (_slotPool == null) return;
            for (int i = _shown.Count - 1; i >= 0; i--) _slotPool.Release(_shown[i]);
            _shown.Clear();
        }

        private void ReturnSubGroups()
        {
            if (_subGroupPool == null) return;
            for (int i = _shownSubGroups.Count - 1; i >= 0; i--)
                _subGroupPool.Release(_shownSubGroups[i]);
            _shownSubGroups.Clear();
        }

        #endregion
    }
}
