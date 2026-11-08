using Cysharp.Threading.Tasks;
using VContainer.Unity;
using _DreamKeep.CommonPresentation;
using _DreamKeep.GameRun;
using _DreamKeep.Presentation.Root;
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
        private readonly UI_RestVFXRoot _vfxRoot;
        private readonly UI_CardUpgradePanel _panel;
        private readonly UI_CardRemovePanel _removePanel;
        private readonly UI_RestProceedButton _proceedButton;
        private readonly SegmentSceneManager _segmentManager;
        private readonly VfxFactory _vfxFactory;

        public RestEntry(RestLoop loop_, RestEventDispatcher events_, RestCommandDispatcher commands_, RunState run_,
            UI_RestVFXRoot vfxRoot_, UI_CardUpgradePanel panel_, UI_CardRemovePanel removePanel_,
            UI_RestProceedButton proceedButton_, SegmentSceneManager segmentManager_, VfxFactory vfxFactory_)
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
            _vfxFactory = vfxFactory_;
        }

        public void Start()
        {
            _vfxRoot.Initialize(_vfxFactory); // 工厂在父容器、跨场景不可序列化，运行时交底

            _events.Register<REE_RestSmokeRequested>(smoke_ => _vfxRoot.PlayRestSmokeAsync());
            _events.Register<REE_UpgradeCardsRequested>(requested_ =>
                CardSelectFlow.OpenAndSubmitAsync(_panel, _run.Player, requested_.Count,
                    cards_ => _commands.SubmitCardSelection(cards_)).Forget());
            _events.Register<REE_RemoveCardsRequested>(requested_ =>
                CardSelectFlow.OpenAndSubmitAsync(_removePanel, _run.Player, requested_.Count,
                    cards_ => _commands.SubmitCardSelection(cards_)).Forget());
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
    }
}
