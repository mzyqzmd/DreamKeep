using _DreamKeep.ConfigData;
using Cysharp.Threading.Tasks;
using UnityEngine;
using VContainer;
using VContainer.Unity; // InjectGameObject 扩展方法住在这里

namespace _DreamKeep.Presentation
{
    /// <summary>宝箱遗物选择面板：中心生成单件待选遗物、点选上报（点选一次即锁定）。
    /// 面板与槽位由段 scope 在层级解析，槽位预制体按需实例化到面板中心；继续按钮独立成 UI_ChestProceedButton。</summary>
    public sealed class UI_RelicSelectPanel : MonoBehaviour
    {
        [SerializeField] private UI_RelicToSelectSlot _slotPrefab;

        private UI_RelicToSelectSlot _slot;
        private UniTaskCompletionSource _claimSource;
        private IObjectResolver _resolver;

        // 槽位走容器外实例化：宿主 PopFactory 必须由容器注入
        [Inject]
        public void Construct(IObjectResolver resolver_) => _resolver = resolver_;

        private void OnDestroy()
        {
            if (_slot) _slot.Clicked -= OnSlotClicked;
        }

        #region 对外：展示候选与点选异步等待

        /// <summary>在面板中心实例化单件槽位并绑定遗物；候选为空仅报错保持空态（点选不会放行）。</summary>
        public void ShowChoice(SO_Relic relic_)
        {
            gameObject.SetActive(true);
            if (relic_ == null)
            {
                Debug.LogError($"[{nameof(UI_RelicSelectPanel)}] 候选遗物为空，无法展示");
                return;
            }

            if (_slot == null)
            {
                GameObject slotObj = Instantiate(_slotPrefab.gameObject, transform);
                _resolver.InjectGameObject(slotObj);
                _slot = slotObj.GetComponent<UI_RelicToSelectSlot>();
                RectTransform rect = (RectTransform)_slot.transform;
                rect.anchorMin = rect.anchorMax = new Vector2(0.5f, 0.5f);
                rect.anchoredPosition = Vector2.zero;
                _slot.Clicked += OnSlotClicked;
            }

            _slot.Bind(relic_);
        }

        /// <summary>等待玩家点选遗物：点选一次即锁定，重复点击不再放行。</summary>
        public UniTask WaitClaimAsync()
        {
            _claimSource = new UniTaskCompletionSource();
            return _claimSource.Task;
        }

        #endregion

        private void OnSlotClicked()
        {
            if (_claimSource == null) return;
            _slot?.Lock();
            UniTaskCompletionSource source = _claimSource;
            _claimSource = null;
            source.TrySetResult();
        }
    }
}
