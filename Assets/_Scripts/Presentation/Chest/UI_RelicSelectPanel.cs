using _DreamKeep.ConfigData;
using _DreamKeep.GameRun;
using UnityEngine;
using VContainer;
using VContainer.Unity; // InjectGameObject 扩展方法住在这里

namespace _DreamKeep.Presentation
{
    /// <summary>宝箱遗物选择面板：订阅 CE_RelicOffered 后在中心生成单件待选遗物，点选只上报领取意图。
    /// 面板与槽位由段 scope 在层级解析，槽位预制体按需实例化到面板中心。</summary>
    public sealed class UI_RelicSelectPanel : MonoBehaviour
    {
        [SerializeField] private UI_RelicToSelectSlot _slotPrefab;

        private UI_RelicToSelectSlot _slot;
        private IObjectResolver _resolver;
        private ChestCommandDispatcher _commands;

        private void OnDestroy()
        {
            if (_slot) _slot.Clicked -= OnSlotClicked;
        }

        [Inject]
        public void Construct(IObjectResolver resolver_, ChestEventDispatcher events_, ChestCommandDispatcher commands_)
        {
            _resolver = resolver_;
            _commands = commands_;
            events_.Register<CE_RelicOffered>(OnRelicOffered);
        }

        // 候选就位：槽位走容器外实例化，宿主 PopFactory 必须由容器注入
        private void OnRelicOffered(CE_RelicOffered offered_)
        {
            gameObject.SetActive(true);
            SO_Relic relic = offered_.Relic;
            if (relic == null)
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
            else _slot.Unlock(); // 领取失败重开点选：Lock 关掉的是交互，Bind 不会自动解锁

            _slot.Bind(relic);
        }

        private void OnSlotClicked()
        {
            _slot?.Lock();
            _commands.ClaimRelic();
        }
    }
}
