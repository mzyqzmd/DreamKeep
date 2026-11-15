using System;
using System.Collections.Generic;
using VContainer;
using _DreamKeep.ConfigData;
using _DreamKeep.EngineUtility;
using UnityEngine;
using UnityEngine.UI;

namespace _DreamKeep.CommonPresentation
{
    /// <summary>药水研究所页：普通 / 稀有两组各挂一份药水池，空组连标题隐藏；
    /// 槽位池化陈列，只绑定图标与悬停说明，不接点击（本页没有信息面板）；
    /// 本页返回键自持，点击只上报返回意图，开合编排归根面板。</summary>
    public sealed class UI_PotionEncyclopediaPanel : UI_Panel
    {
        [SerializeField] private GameObject[] _groupRoots;
        [SerializeField] private RectTransform[] _groupContents;
        [SerializeField] private SO_PotionPool[] _groupPools;
        [SerializeField] private UI_PotionSlot _potionSlotPrefab;
        [Header("返回")] [Tooltip("本页返回键：点击只上报返回意图")] [SerializeField]
        private Button _closeButton;

        public event Action CloseRequested;

        private readonly List<UI_PotionSlot> _shown = new();
        private MonoObjectPool<UI_PotionSlot> _pool;
        private PopFactory _popFactory;

        [Inject]
        public void Construct(PopFactory popFactory_) => _popFactory = popFactory_;

        private void Awake()
        {
            _pool = new MonoObjectPool<UI_PotionSlot>(transform, 64,
                onCreate_: slot_ => slot_.GetComponent<UI_HoverTip>().Construct(_popFactory));
            _closeButton.onClick.AddListener(() => CloseRequested?.Invoke());
        }

        protected override void OnShown() => Refresh();

        protected override void OnHidden() => ReturnAll();

        #region 陈列：两组各自读池生成槽位

        private void Refresh()
        {
            ReturnAll();
            for (int i = 0; i < _groupPools.Length; i++)
            {
                SO_PotionPool pool = _groupPools[i];
                bool has = pool && pool.Potions.Count > 0;
                _groupRoots[i].SetActive(has);
                if (!has) continue;
                IReadOnlyList<SO_Potion> potions = pool.Potions;
                for (int j = 0; j < potions.Count; j++)
                {
                    if (!potions[j]) continue;
                    UI_PotionSlot slot = _pool.Get(_potionSlotPrefab);
                    slot.transform.SetParent(_groupContents[i], false);
                    slot.transform.SetSiblingIndex(j); // GridLayoutGroup 按兄弟序摆格，池复用不改兄弟位
                    slot.Bind(potions[j]);
                    _shown.Add(slot);
                }
            }
        }

        // HideOverlays 可能在 Awake 之前调 Hide，故容忍池未建
        private void ReturnAll()
        {
            if (_pool == null) return;
            for (int i = _shown.Count - 1; i >= 0; i--) _pool.Release(_shown[i]);
            _shown.Clear();
        }

        #endregion
    }
}
