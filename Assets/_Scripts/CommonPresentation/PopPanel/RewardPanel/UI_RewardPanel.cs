using System;
using System.Collections.Generic;
using _DreamKeep.GameRun;
using _DreamKeep.RuntimeData;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using VContainer;

namespace _DreamKeep.CommonPresentation
{
    /// <summary>通用奖励面板：外界给标题与一份奖励清单，面板自己决定每条长什么样，并在确认后把"继续"回报给外界。
    /// 本面板认识 <see cref="ERewardKind"/>：图标（金币与卡牌来自本组件字段，药水与遗物用各自资产图标）与文案都由它决定；
    /// 点击条目只上报「第几条」与三选一选中的卡，真正写域由整局命令入口里的领奖服务完成。
    /// 面板仍不认识战斗与地图：条目内容来自奖励清单，"继续之后去哪"由调用方传入。
    /// 条目兑现成功后调 Complete 请求移除、失败则 Cancel 解锁重试；继续按钮只调外界给的"继续"回调，退场一律由调用方 Close()。</summary>
    public sealed class UI_RewardPanel : MonoBehaviour
    {
        [SerializeField] private TextMeshProUGUI _title;
        [SerializeField] private RectTransform _itemContainer;
        [SerializeField] private UI_RewardItem _itemPrefab;
        [SerializeField] private Button _proceedButton;

        [Header("条目图标：药水与遗物直接用各自资产的图标")]
        [SerializeField] private Sprite _goldIcon;

        [SerializeField] private Sprite _cardIcon;

        private readonly List<UI_RewardItem> _items = new(); // 当前展示的条目（兑现一个就摘一个）
        private Action _onProceed;
        private RunCommandDispatcher _commands;
        private PopPanelFactory _popFactory;
        private int _showToken; // 本次陈列的令牌：面板被收起或重开时作废上一轮还挂着的三选一回调

        private void Awake()
        {
            if (_proceedButton) _proceedButton.onClick.AddListener(OnProceedClicked);
        }

        private void OnDestroy()
        {
            if (_proceedButton) _proceedButton.onClick.RemoveListener(OnProceedClicked);
        }

        // 面板由 PopPanelFactory 在容器外实例化后补注入：领取只上报意图给整局命令入口，三选一要借工厂再取一个面板
        [Inject]
        public void Construct(RunCommandDispatcher commands_, PopPanelFactory popFactory_)
        {
            _commands = commands_;
            _popFactory = popFactory_;
        }

        #region 开合与内容：外界给清单，面板自己铺条目并兑现

        /// <summary>用给定标题与奖励清单重新铺满列表；<paramref name="onProceed_"/> 是确认按钮的回调（面板不自行关闭）。
        /// 清单为空或控件没接线时直接放行回调，让调用方照常往下走，不被一张空面板卡住。</summary>
        public void Show(string title_, IReadOnlyList<RunReward> rewards_, Action onProceed_)
        {
            if (rewards_ == null || rewards_.Count == 0)
            {
                onProceed_?.Invoke();
                return;
            }

            if (!_title || !_itemContainer || !_itemPrefab)
            {
                Debug.LogError($"[{nameof(UI_RewardPanel)}] 面板控件未接线，奖励清单已跳过：{rewards_.Count} 条");
                onProceed_?.Invoke();
                return;
            }

            gameObject.SetActive(true);
            transform.SetAsLastSibling(); // 面板由工厂懒建，取出时要压在场景其余 UI 之上
            _showToken++;
            _title.text = title_;
            _onProceed = onProceed_;
            ClearItems();
            for (int i = 0; i < rewards_.Count; i++)
            {
                RunReward reward = rewards_[i];
                UI_RewardItem item = Instantiate(_itemPrefab, _itemContainer);
                item.Bind(ToEntry(reward, i), RemoveItem);
                _items.Add(item);
            }
        }

        /// <summary>收起面板：条目全部销毁，下次 Show 重新生成（面板实例本身留给工厂复用）。</summary>
        public void Close()
        {
            _showToken++; // 本轮作废：还悬着的三选一回调不再发奖
            if (_popFactory) _popFactory.TryCloseCardChoicePanel(); // 只关已建出的实例，不为收场懒建
            ClearItems();
            _onProceed = null;
            gameObject.SetActive(false);
        }

        #endregion

        #region 清单 → 条目：图标与文案只在这里决定一次

        private RewardEntry ToEntry(RunReward reward_, int index_)
        {
            return reward_.Kind switch
            {
                ERewardKind.Gold => new RewardEntry(_goldIcon, $"金币 +{reward_.Gold}",
                    item_ => ClaimGold(index_, item_)),
                ERewardKind.Card => new RewardEntry(_cardIcon, "获得一次卡牌奖励",
                    item_ => ClaimCards(index_, reward_, item_)),
                ERewardKind.Potion => new RewardEntry(
                    reward_.Potion == null ? null : reward_.Potion.Icon,
                    reward_.Potion == null ? "药水奖励（配置缺失）" : $"{reward_.Potion.PotionName}",
                    item_ => ClaimPotion(index_, item_)),
                ERewardKind.Relic => new RewardEntry(
                    reward_.Relic == null ? null : reward_.Relic.Icon,
                    reward_.Relic == null ? "遗物奖励（配置缺失）" : $"{reward_.Relic.RelicName}",
                    item_ => ClaimRelic(index_, item_)),
                _ => throw new ArgumentOutOfRangeException(nameof(reward_),
                    $"{reward_.Kind} 未登记：漏登记会让该条奖励点了没反应"),
            };
        }

        #endregion

        #region 兑现：只上报序号，写域由整局命令入口里的领奖服务完成

        private void ClaimGold(int index_, UI_RewardItem item_) =>
            Settle(_commands.ClaimReward(index_), item_);

        // 点卡牌条目才铺三张候选：选中把卡交给领奖服务克隆入组，条目随之消失；
        // 跳过只是"这次不看了"，条目解锁回原样，玩家可以再点开重选（或一直放着）。
        // 回调里先核对令牌：面板若已被收起或换了一轮陈列，这手牌不再算数
        private void ClaimCards(int index_, RunReward reward_, UI_RewardItem item_)
        {
            int token = _showToken;
            // 三选一跟随奖励面板所在画布：本面板被停泊到哪（如战斗段 Canvas_HUD），特写层就压在同一处
            UI_CardOneInThreePanel panel = _popFactory.AcquireCardChoicePanel((RectTransform)transform.parent);
            panel.Show(reward_.Cards, selected_ =>
            {
                if (token != _showToken) return;
                ERewardClaimResult result = _commands.ClaimReward(index_, selected_);
                panel.Close();
                if (item_) Settle(result, item_);
            }, () =>
            {
                if (token != _showToken) return;
                panel.Close();
                if (item_) item_.Cancel();
            });
        }

        // 槽位已满时领不了：解锁条目让玩家先去喝一瓶再回来点，不在数据侧静默丢弃
        private void ClaimPotion(int index_, UI_RewardItem item_) =>
            Settle(_commands.ClaimReward(index_), item_);

        private void ClaimRelic(int index_, UI_RewardItem item_) =>
            Settle(_commands.ClaimReward(index_), item_);

        // 成功才收起条目；槽满/配置缺失都解锁重试，陈旧回调静默不动
        private static void Settle(ERewardClaimResult result_, UI_RewardItem item_)
        {
            if (result_ == ERewardClaimResult.Stale) return;
            if (result_ == ERewardClaimResult.Success) item_.Complete();
            else item_.Cancel();
        }

        #endregion

        private void OnProceedClicked() => _onProceed?.Invoke();

        // 条目兑现后自己请求退场：先摘台账再失活销毁——销毁要到帧末才生效，先失活才能当场把布局收拢
        private void RemoveItem(UI_RewardItem item_)
        {
            _items.Remove(item_);
            if (!item_) return;
            item_.gameObject.SetActive(false);
            Destroy(item_.gameObject);
        }

        private void ClearItems()
        {
            foreach (UI_RewardItem item in _items)
            {
                if (!item) continue;
                item.gameObject.SetActive(false);
                Destroy(item.gameObject);
            }

            _items.Clear();
        }
    }
}
