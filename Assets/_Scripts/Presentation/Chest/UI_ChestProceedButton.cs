using _DreamKeep.EngineUtility;
using _DreamKeep.GameRun;
using VContainer;

namespace _DreamKeep.Presentation
{
    /// <summary>宝箱段「继续」按钮：场景初始隐藏，遗物领取成功的事实到达后亮出；
    /// 每次点击只提交继续意图，收段开图由宝箱段完成事实的订阅方完成。</summary>
    public sealed class UI_ChestProceedButton : UI_Button
    {
        private ChestCommandDispatcher _commands;

        private void Awake() => Clicked += OnProceedClicked;

        [Inject]
        public void Construct(ChestEventDispatcher events_, ChestCommandDispatcher commands_)
        {
            _commands = commands_;
            events_.Register<CE_RelicClaimed>(OnRelicClaimed);
        }

        private void OnRelicClaimed(CE_RelicClaimed claimed_) => gameObject.SetActive(true);

        private void OnProceedClicked() => _commands.Proceed();
    }
}
