using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using VContainer.Unity;
using _DreamKeep.CommonPresentation;
using _DreamKeep.GameRun;
using _DreamKeep.RuntimeData;

namespace _DreamKeep.Presentation
{
    /// <summary>休息段表现入口：启动域流程，把弹板与特效请求接上段内视图并把结果交回命令门面，
    /// 再把段完成事实转成整局编排者的收段动作。视图只订阅事实与提交意图，不再一路 await 面板。</summary>
    public sealed class RestEntry : IStartable
    {
        private readonly RestLoop _loop;
        private readonly RestEventDispatcher _events;
        private readonly RestCommandDispatcher _commands;
        private readonly RunState _run;
        private readonly RestVfxRoot _vfxRoot;
        private readonly UI_CardUpgradePanel _panel;
        private readonly UI_CardRemovePanel _removePanel;
        private readonly UI_RestProceedButton _proceedButton;
        private readonly SegmentSceneManager _segmentManager;

        public RestEntry(RestLoop loop_, RestEventDispatcher events_, RestCommandDispatcher commands_, RunState run_,
            RestVfxRoot vfxRoot_, UI_CardUpgradePanel panel_, UI_CardRemovePanel removePanel_,
            UI_RestProceedButton proceedButton_, SegmentSceneManager segmentManager_)
        {
            _loop = loop_;
            _events = events_;
            _commands = commands_;
            _run = run_;
            _vfxRoot = vfxRoot_;
            _panel = panel_;
            _removePanel = removePanel_;
            _proceedButton = proceedButton_;
            _segmentManager = segmentManager_;
        }

        public void Start()
        {
            _events.Register<REE_RestSmokeRequested>(smoke_ => _vfxRoot.PlayRestSmokeAsync());
            _events.Register<REE_UpgradeCardsRequested>(requested_ =>
                SubmitSelectionAsync(_panel, requested_.Count, requested_.Result));
            _events.Register<REE_RemoveCardsRequested>(requested_ =>
                SubmitSelectionAsync(_removePanel, requested_.Count, requested_.Result));
            _events.Register<REE_ProceedOffered>(OnProceedOffered);
            _events.Register<REE_RestCompleted>(completed_ => _segmentManager.CompleteSegment());
            _loop.Start();
        }

        private void OnProceedOffered(REE_ProceedOffered offered_)
        {
            _proceedButton.gameObject.SetActive(true);
            _proceedButton.Clicked += OnProceedButtonClicked;
        }

        private void OnProceedButtonClicked() => _commands.Proceed();

        // 选板结果交回请求事件自带的结果源：null = 关板取消，由域侧短路
        private async UniTask SubmitSelectionAsync(UI_CardSelectPanel panel_, int count_,
            UniTaskCompletionSource<IReadOnlyList<CardModel>> result_, bool cancelable_ = true)
        {
            IReadOnlyList<CardModel> cards = await panel_.ShowAndWaitAsync(_run.Player, count_, cancelable_);
            result_.TrySetResult(cards);
        }
    }
}
