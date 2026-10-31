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
    /// <summary>通用奖励面板：外界给标题与一份奖励清单，面板自己决定每条长什么样、点了给什么，并在确认后把"继续"回报给外界。
    /// 与旧版不同，本面板认识 <see cref="ERewardKind"/>：图标（金币与卡牌来自本组件字段，药水与遗物用各自资产图标）与文案都由它决定，
    /// 领取也由它完成——金币入账、卡牌弹三选一进化牌组、药水入槽（槽满则解锁让玩家先喝一瓶）、遗物入栏。
    /// 面板仍不认识战斗与地图：条目内容来自奖励清单，"继续之后去哪"由调用方传入。
    /// 条目被兑现后自己调 Complete 请求移除，继续按钮只调外界给的"继续"回调，退场一律由调用方 Close()。</summary>
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
        private RunState _run;
        private PopFactory _popFactory;
        private int _showToken; // 本次陈列的令牌：面板被收起或重开时作废上一轮还挂着的三选一回调

        private void Awake()
        {
            if (_proceedButton) _proceedButton.onClick.AddListener(OnProceedClicked);
        }

        private void OnDestroy()
        {
            if (_proceedButton) _proceedButton.onClick.RemoveListener(OnProceedClicked);
        }

        // 面板由 PopFactory 在容器外实例化后补注入：领取要写整局状态，三选一要借工厂再取一个面板
        [Inject]
        public void Construct(RunState run_, PopFactory popFactory_)
        {
            _run = run_;
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
                item.Bind(ToEntry(reward), RemoveItem);
                _items.Add(item);
            }
        }

        /// <summary>收起面板：条目全部销毁，下次 Show 重新生成（面板实例本身留给工厂复用）。</summary>
        public void Close()
        {
            _showToken++; // 本轮作废：还悬着的三选一回调不再发奖
            if (_popFactory) _popFactory.AcquireCardChoicePanel().Close();
            ClearItems();
            _onProceed = null;
            gameObject.SetActive(false);
        }

        #endregion

        #region 清单 → 条目：图标与文案只在这里决定一次

        private RewardEntry ToEntry(RunReward reward_)
        {
            return reward_.Kind switch
            {
                ERewardKind.Gold => new RewardEntry(_goldIcon, $"金币 +{reward_.Gold}",
                    item_ => ClaimGold(reward_, item_)),
                ERewardKind.Card => new RewardEntry(_cardIcon, "获得一次卡牌奖励",
                    item_ => ClaimCards(reward_, item_)),
                ERewardKind.Potion => new RewardEntry(
                    reward_.Potion == null ? null : reward_.Potion.Icon,
                    reward_.Potion == null ? "药水奖励（配置缺失）" : $"{reward_.Potion.PotionName}",
                    item_ => ClaimPotion(reward_, item_)),
                ERewardKind.Relic => new RewardEntry(
                    reward_.Relic == null ? null : reward_.Relic.Icon,
                    reward_.Relic == null ? "遗物奖励（配置缺失）" : $"{reward_.Relic.RelicName}",
                    item_ => ClaimRelic(reward_, item_)),
                _ => throw new ArgumentOutOfRangeException(nameof(reward_),
                    $"{reward_.Kind} 未登记：漏登记会让该条奖励点了没反应"),
            };
        }

        #endregion

        #region 兑现：金币入账 / 卡牌三选一 / 药水入槽 / 遗物入栏

        private void ClaimGold(RunReward reward_, UI_RewardItem item_)
        {
            _run.GainGold(reward_.Gold);
            item_.Complete();
        }

        // 点卡牌条目才铺三张候选：选中即克隆成局内卡进牌组，条目随之消失；
        // 跳过只是"这次不看了"，条目解锁回原样，玩家可以再点开重选（或一直放着）。
        // 回调里先核对令牌：面板若已被收起或换了一轮陈列，这手牌不再算数
        private void ClaimCards(RunReward reward_, UI_RewardItem item_)
        {
            int token = _showToken;
            // 三选一跟随奖励面板所在画布：本面板被停泊到哪（如战斗段 Canvas_HUD），特写层就压在同一处
            UI_CardOneInThreePanel panel = _popFactory.AcquireCardChoicePanel((RectTransform)transform.parent);
            panel.Show(reward_.Cards, selected_ =>
            {
                if (token != _showToken) return;
                _run.Player.AddCard(selected_.ToMutable());
                panel.Close();
                if (item_) item_.Complete();
            }, () =>
            {
                if (token != _showToken) return;
                panel.Close();
                if (item_) item_.Cancel();
            });
        }

        // 槽位已满时领不了：解锁条目让玩家先去喝一瓶再回来点，不在数据侧静默丢弃
        private void ClaimPotion(RunReward reward_, UI_RewardItem item_)
        {
            if (reward_.Potion != null && _run.Player.TryAddPotion(reward_.Potion)) item_.Complete();
            else item_.Cancel();
        }

        private void ClaimRelic(RunReward reward_, UI_RewardItem item_)
        {
            if (reward_.Relic != null) _run.Player.AddRelic(reward_.Relic);
            item_.Complete();
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
