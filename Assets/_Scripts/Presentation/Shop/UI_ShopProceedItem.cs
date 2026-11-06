using _DreamKeep.EngineUtility;
using _DreamKeep.GameRun;
using VContainer;

namespace _DreamKeep.Presentation
{
    /// <summary>商店「继续」按钮：首次掀开地毯的事实到达后亮出（场景初始是关的），
    /// 每次点击只提交继续意图，收段开图由商店段完成事实的订阅方完成。</summary>
    public sealed class UI_ShopProceedItem : UI_Button
    {
        private ShopCommandDispatcher _commands;

        [Inject]
        public void Construct(ShopEventDispatcher events_, ShopCommandDispatcher commands_)
        {
            _commands = commands_;
            events_.Register<SE_ShopOpened>(opened_ => Show());
        }

        private void Awake() => Clicked += OnClicked;

        /// <summary>亮出按钮：掀摊事实到达时调用，重复亮出无副作用。</summary>
        public void Show() => gameObject.SetActive(true);

        private void OnClicked() => _commands.Proceed();
    }
}
