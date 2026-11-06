using Cysharp.Threading.Tasks;
using VContainer.Unity;
using _DreamKeep.CommonPresentation;
using _DreamKeep.GameRun;

namespace _DreamKeep.Presentation
{
    /// <summary>事件段表现入口：启动域流程，把自选请求接上段内弹板并把结果交回命令门面，
    /// 再把段完成事实转成整局编排者的收段动作。视图只订阅事实与提交意图，不再一路 await 面板。</summary>
    public sealed class EventEntry : IStartable
    {
        private readonly EventLoop _loop;
        private readonly EventEventDispatcher _events;
        private readonly EventCommandDispatcher _commands;
        private readonly RunState _run;
        private readonly UI_CardRemovePanel _removePanel;
        private readonly UI_CardUpgradePanel _upgradePanel;
        private readonly UI_RewardPanel _rewardPanel;
        private readonly SegmentSceneManager _segmentManager;

        public EventEntry(EventLoop loop_, EventEventDispatcher events_, EventCommandDispatcher commands_,
            RunState run_, UI_CardRemovePanel removePanel_, UI_CardUpgradePanel upgradePanel_,
            UI_RewardPanel rewardPanel_, SegmentSceneManager segmentManager_)
        {
            _loop = loop_;
            _events = events_;
            _commands = commands_;
            _run = run_;
            _removePanel = removePanel_;
            _upgradePanel = upgradePanel_;
            _rewardPanel = rewardPanel_;
            _segmentManager = segmentManager_;
        }

        public void Start()
        {
            _events.Register<EE_EventCompleted>(completed_ => _segmentManager.CompleteSegment());
            _events.Register<EE_RemoveCardsRequested>(requested_ =>
                OpenCardPanelAsync(_removePanel, requested_.Count).Forget());
            _events.Register<EE_UpgradeCardsRequested>(requested_ =>
                OpenCardPanelAsync(_upgradePanel, requested_.Count).Forget());
            _events.Register<EE_RewardPotionsOffered>(OnRewardPotionsOffered);
            _loop.Start();
        }

        // 弹板自管选牌、删除与演出；关闭键交还 null = 取消，由命令门面原样交回域循环
        private async UniTask OpenCardPanelAsync(UI_CardSelectPanel panel_, int count_)
        {
            panel_.Show(_run.Player, count_);
            _commands.SubmitCardSelection(await panel_.WaitResultAsync());
        }

        private void OnRewardPotionsOffered(EE_RewardPotionsOffered offered_) =>
            _rewardPanel.Show(offered_.Title, offered_.Rewards, () =>
            {
                _rewardPanel.Close();
                _commands.AcknowledgeReward();
            });
    }
}
