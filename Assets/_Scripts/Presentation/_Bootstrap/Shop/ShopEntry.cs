using _DreamKeep.CommonPresentation;
using Cysharp.Threading.Tasks;
using VContainer;
using VContainer.Unity;
using _DreamKeep.ConfigData;
using _DreamKeep.GameRun;
using _DreamKeep.RuntimeData;

namespace _DreamKeep.Presentation
{
    /// <summary>商店段表现入口：启动域流程，把删牌请求接上段内弹板并把结果交回命令门面，
    /// 再把段完成事实转成整局编排者的收段动作；成交与支付失败事实在这里经次数台账翻成商人的一句台词。
    /// 视图只订阅事实与提交意图，不再持有会话或直调编排者。</summary>
    public sealed class ShopEntry : IStartable
    {
        private readonly ShopLoop _loop;
        private readonly ShopEventDispatcher _events;
        private readonly ShopCommandDispatcher _commands;
        private readonly RunState _run;
        private readonly UI_ShopRugPanel _rug;
        private readonly UI_ShopMerchant _merchant;
        private readonly UI_CardRemovePanel _removePanel;
        private readonly UI_MerchantSpeech _merchantSpeech;
        private readonly SO_MerchantSpeechLines _merchantSpeechLines;
        private readonly SpeechLineTracker _speechTracker;
        private readonly SegmentSceneManager _segmentManager;
        private readonly IObjectResolver _resolver;

        public ShopEntry(ShopLoop loop_, ShopEventDispatcher events_, ShopCommandDispatcher commands_, RunState run_,
            UI_ShopRugPanel rug_, UI_ShopMerchant merchant_, UI_CardRemovePanel removePanel_,
            UI_MerchantSpeech merchantSpeech_, SO_MerchantSpeechLines merchantSpeechLines_,
            SpeechLineTracker speechTracker_, SegmentSceneManager segmentManager_, IObjectResolver resolver_)
        {
            _loop = loop_;
            _events = events_;
            _commands = commands_;
            _run = run_;
            _rug = rug_;
            _merchant = merchant_;
            _removePanel = removePanel_;
            _merchantSpeech = merchantSpeech_;
            _merchantSpeechLines = merchantSpeechLines_;
            _speechTracker = speechTracker_;
            _segmentManager = segmentManager_;
            _resolver = resolver_;
        }

        public void Start()
        {
            // 地毯子树的悬停气泡要靠 PopFactory：场景件只接了地毯本体，子树在这里补一次注入（Bind 前必须完成）
            _resolver.InjectGameObject(_rug.gameObject);

            _events.Register<SE_RemovalPanelRequested>(requested_ =>
                CardSelectFlow.OpenAndSubmitAsync(_removePanel, _run.Player, requested_.Count,
                    cards_ => _commands.SubmitCardSelection(cards_)).Forget());
            _events.Register<SE_ShopCompleted>(_ => _segmentManager.CompleteSegment());
            _events.Register<SE_PurchaseSucceeded>(_ => SpeakMerchant(_merchantSpeechLines.PurchaseSuccess));
            _events.Register<SE_PurchaseRefused>(refused_ => SpeakMerchant(PickRefusalGroup(refused_.Reason)));
            _events.Register<SE_RemovalRefused>(_ => SpeakMerchant(_merchantSpeechLines.FailureGold));
            _loop.Start();
            _rug.Hide(); // 进店先收摊：玩家点商人再掀开

            _merchant.Clicked += OnMerchantClicked;
            // 删牌弹板随本段场景装配；先摘再挂是幂等守卫——注入若因补注入重跑一次，不累积订阅
            _removePanel.Confirmed -= OnRemovalConfirmed;
            _removePanel.Confirmed += OnRemovalConfirmed;
        }

        private void OnMerchantClicked()
        {
            if (_rug.gameObject.activeSelf) return;
            _commands.OpenShop();
        }

        // 玩家在删牌弹板按下确认的同一刻：删牌服务位播一次翻面并进入不可用态
        private void OnRemovalConfirmed() => _rug.PlayRemovalFlip();

        // 支付失败按原因取分组：金币不足 / 药水栏满；已售出属点击竞态，保持沉默
        private SpeechLineGroup PickRefusalGroup(EShopBuyResult reason_) => reason_ switch
        {
            EShopBuyResult.NotEnoughGold => _merchantSpeechLines.FailureGold,
            EShopBuyResult.PotionSlotsFull => _merchantSpeechLines.FailureSpace,
            _ => null,
        };

        // 取词 → 播放 → 没真正播出就退回本次计数：未播仍耗次数会让商人永久沉默，与战斗侧口径不一致
        private void SpeakMerchant(SpeechLineGroup group_)
        {
            string line = _speechTracker.TryPick(_merchantSpeechLines, group_);
            if (string.IsNullOrEmpty(line)) return;
            if (!_merchantSpeech.Play(line)) _speechTracker.Rollback(_merchantSpeechLines, group_);
        }
    }
}
