using System.Collections.Generic;
using System.Linq;
using Cysharp.Threading.Tasks;
using UnityEngine;
using VContainer;
using VContainer.Unity;
using _DreamKeep.CommonPresentation;
using _DreamKeep.ConfigData;
using _DreamKeep.GameRun;
using _DreamKeep.Presentation.Root;
using _DreamKeep.RuntimeData;

namespace _DreamKeep.Presentation
{
    /// <summary>休息段入口：按玩家状态动态组装休息/锻造（/挖掘）选项——休息＝烟雾播完回复 25% 最大生命；
    /// 锻造＝先列可升级卡，玩家手动选卡再预览/确认；挖掘＝持有铲子时追加，从 Common+Rare 池掷取一件未拥有遗物
    /// 直接获得并播一次遗物涟漪。锻造/挖掘条件不满足时选项置灰（悬停文案保留、点击无作用）。
    /// 弹板关闭（未选）则回到选项重选。任一路完成后关闭选项与文案、弹出「继续」，点击只开地图并保留本段，
    /// 卸载推迟到选定下一个节点。特效走 UI_RestVFXRoot → VfxFactory。</summary>
    public sealed class RestEntry : IStartable
    {
        /// <summary>休息选项图标三元组：组合根序列化资产后注入，段入口按选项语义取用。</summary>
        public readonly struct OptionIcons
        {
            public readonly Sprite Heal;
            public readonly Sprite Smith;
            public readonly Sprite Dig;

            public OptionIcons(Sprite heal_, Sprite smith_, Sprite dig_)
            {
                Heal = heal_;
                Smith = smith_;
                Dig = dig_;
            }
        }

        private readonly RunState _run;
        private readonly UI_RestOptionGroup _options;
        private readonly UI_RestVFXRoot _vfxRoot;
        private readonly UI_SmithPanel _panel;
        private readonly UI_RestProceed _proceed;
        private readonly SegmentSceneManager _segmentManager;
        private readonly IObjectResolver _resolver;
        private readonly VfxFactory _vfxFactory;
        private readonly RelicTriggerNotifier _notifier;
        private readonly SO_RelicPool _commonPool;
        private readonly SO_RelicPool _rarePool;
        private readonly OptionIcons _icons;

        private const float HealMaxHpRatio = 0.25f;

        public RestEntry(RunState run_, UI_RestOptionGroup options_, UI_RestVFXRoot vfxRoot_, UI_SmithPanel panel_,
            UI_RestProceed proceed_, SegmentSceneManager segmentManager_, IObjectResolver resolver_,
            VfxFactory vfxFactory_, RelicTriggerNotifier notifier_,
            [Key("Common")] SO_RelicPool commonPool_, [Key("Rare")] SO_RelicPool rarePool_, OptionIcons icons_)
        {
            _run = run_;
            _options = options_;
            _vfxRoot = vfxRoot_;
            _panel = panel_;
            _proceed = proceed_;
            _segmentManager = segmentManager_;
            _resolver = resolver_;
            _vfxFactory = vfxFactory_;
            _notifier = notifier_;
            _commonPool = commonPool_;
            _rarePool = rarePool_;
            _icons = icons_;
        }

        public void Start()
        {
            _vfxRoot.Initialize(_vfxFactory); // 工厂在父容器、跨场景不可序列化，运行时交底
            _panel.Initialize(_resolver); // 面板池需要容器注入卡面，显式交底（面板是纯场景件、不走 [Inject]）
            PlayAsync().Forget();
        }

        private async UniTask PlayAsync()
        {
            Creature creature = _run.Player.Creature;
            int heal = Mathf.CeilToInt(creature.MaxHp * HealMaxHpRatio);
            string restDescription = $"回复 25% 生命值（{heal}）";

            while (true)
            {
                List<SO_Relic> digCandidates =
                    BuildOptions(restDescription, out UI_RestOptionGroup.RestOption[] options);
                _options.Show(options);
                int choice = await _options.WaitChoiceAsync();
                _options.Hide(); // 点选即收：选项按钮与悬停描述立刻消失，画面只留特效与结果

                // 选项序与 BuildOptions 一致：0＝休息、1＝锻造、2＝挖掘（仅持有铲子时存在）；
                // 置灰选项吞点击（基类 Enabled 接管），故 choice 只会落在可用项上
                if (choice == 0)
                {
                    await RestAsync(creature, heal);
                    break;
                }

                if (choice == 1)
                {
                    CardModel upgraded = await ForgeAsync();
                    if (upgraded != null) break;
                    continue; // 锻造取消：回到选项重选
                }

                // 挖掘：点进来即说明候选非空；先领取（同步触发 RelicAdded 补遗物栏槽位）再播涟漪，槽位才找得到
                SO_Relic dug = _run.Rng.TakeShuffled(digCandidates, 1)[0];
                _run.Player.AddRelic(dug);
                _notifier.NotifyTriggered(dug); // 仅播涟漪，不触发遗物效果（效果只在 RelicEffect.PerformAsync 里跑）
                break;
            }

            // 「继续」不是一次性按钮：每点一次都开地图并保留本段（卸载推迟到选定下一个节点）；
            // 玩家把地图关掉后再点继续，还能再次打开。订阅随段销毁一起消失，无需反注册
            _proceed.gameObject.SetActive(true);
            _proceed.Clicked += OnProceedClicked;
        }

        // 点「继续」：开地图并保留本段；不是一次性，关掉地图后再点可再开
        private void OnProceedClicked() => _segmentManager.CompleteSegment();

        /// <summary>按玩家状态组装选项列表；同时算好挖掘候选（Common+Rare 合并过滤已持有，照 AncientSession.RollChoices 口径）。
        /// 持有铲子才追加挖掘选项；候选为空则挖掘选项仍出现但置灰（悬停提示「暂无可挖掘的遗物」）。</summary>
        private List<SO_Relic> BuildOptions(string restDescription_, out UI_RestOptionGroup.RestOption[] options_)
        {
            List<UI_RestOptionGroup.RestOption> opts = new();
            opts.Add(new UI_RestOptionGroup.RestOption("美容觉", restDescription_, _icons.Heal, true));

            bool canForge = _run.Player.Deck.Cards.Any(static c => c.CanUpgrade);
            opts.Add(new UI_RestOptionGroup.RestOption("锻造", "选择你牌库中的一张牌进行升级", _icons.Smith, canForge));

            List<SO_Relic> digCandidates = CollectDigCandidates();
            if (_run.Player.Relics.Any(static r => r is SO_Shovel))
            {
                bool hasDig = digCandidates.Count > 0;
                opts.Add(new UI_RestOptionGroup.RestOption(
                    "挖掘", hasDig ? "挖取获得一件未拥有的遗物" : "暂无可挖掘的遗物", _icons.Dig, hasDig));
            }

            options_ = opts.ToArray();
            return digCandidates;
        }

        // 合并两池并跳过已持有与断引用的项；无铲子时也算（结果只影响置灰态，不影响选项出现）
        private List<SO_Relic> CollectDigCandidates()
        {
            List<SO_Relic> candidates = new();
            AddUnowned(_commonPool);
            AddUnowned(_rarePool);
            return candidates;

            void AddUnowned(SO_RelicPool pool_)
            {
                if (pool_ == null) return;
                foreach (SO_Relic relic in pool_.Relics)
                    if (relic != null && !_run.Player.HasRelic(relic))
                        candidates.Add(relic);
            }
        }


        private async UniTask RestAsync(Creature creature_, int heal_)
        {
            await _vfxRoot.PlayRestSmokeAsync();
            creature_.CurrentHp = Mathf.Min(creature_.MaxHp, creature_.CurrentHp + heal_);
        }

        private async UniTask<CardModel> ForgeAsync()
        {
            _panel.Show(_run.Player);
            CardModel card = await _panel.WaitResultAsync();
            if (card == null)
            {
                _panel.Hide();
                return null;
            }

            await _vfxRoot.PlaySmithForgeAsync(); // 确认时面板已自行收起（见 UI_SmithPanel.OnConfirm），这里只补一次幂等隐藏
            _panel.Hide();
            return card;
        }
    }
}