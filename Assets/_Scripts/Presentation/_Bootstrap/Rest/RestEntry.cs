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
    /// <summary>休息段入口：把配置的选项资产按玩家状态组装成可点选项——休息＝烟雾播完按配置比例回复生命；
    /// 锻造＝先列可升级卡，玩家手动选卡再预览/确认；烹饪＝持有切肉刀时出现，开删牌弹板选 N 张移出牌库并提高最大生命；
    /// 挖掘＝持有铲子时出现，从 Common+Rare 池掷取一件未拥有遗物直接获得并播一次遗物涟漪。
    /// 条件不满足时选项置灰（无悬停缩放与高亮、悬停说明照常、点击无作用），静态说明整句照抄资产、动态说明由这里插值。
    /// 弹板关闭（未选）则回到选项重选。任一路完成后关闭选项与文案、弹出「继续」，点击只开地图并保留本段，
    /// 卸载推迟到选定下一个节点。特效走 UI_RestVFXRoot → VfxFactory。</summary>
    public sealed class RestEntry : IStartable
    {
        private readonly RunState _run;
        private readonly List<SO_RestOption> _restOptions;
        private readonly UI_RestOptionGroup _options;
        private readonly UI_RestVFXRoot _vfxRoot;
        private readonly UI_CardUpgradePanel _panel;
        private readonly PopPanelFactory _popPanelFactory;
        private UI_CardRemovePanel _removePanel; // 工厂单例跨段复用，可能随停泊画布销毁重建，Start 时领取
        private readonly UI_RestProceedButton _proceedButton;
        private readonly SegmentSceneManager _segmentManager;
        private readonly VfxFactory _vfxFactory;
        private readonly RelicTriggerNotifier _notifier;
        private readonly SO_RelicPool _commonPool;
        private readonly SO_RelicPool _rarePool;

        public RestEntry(RunState run_, List<SO_RestOption> restOptions_, UI_RestOptionGroup options_, UI_RestVFXRoot vfxRoot_,
            UI_CardUpgradePanel panel_, PopPanelFactory popPanelFactory_, UI_RestProceedButton proceedButton_,
            SegmentSceneManager segmentManager_, VfxFactory vfxFactory_, RelicTriggerNotifier notifier_,
            [Key("Common")] SO_RelicPool commonPool_, [Key("Rare")] SO_RelicPool rarePool_)
        {
            _run = run_;
            _restOptions = restOptions_;
            _options = options_;
            _vfxRoot = vfxRoot_;
            _panel = panel_;
            _popPanelFactory = popPanelFactory_;
            _proceedButton = proceedButton_;
            _segmentManager = segmentManager_;
            _vfxFactory = vfxFactory_;
            _notifier = notifier_;
            _commonPool = commonPool_;
            _rarePool = rarePool_;
        }

        public void Start()
        {
            _vfxRoot.Initialize(_vfxFactory); // 工厂在父容器、跨场景不可序列化，运行时交底
            // 删牌面板由工厂懒建单例：显式停泊到本段弹层画布容器（同锻造面板的父级），随段卸载销毁、下次进段重建
            _removePanel = _popPanelFactory.AcquireCardRemovePanel((RectTransform)_panel.transform.parent);
            PlayAsync().Forget();
        }

        private async UniTask PlayAsync()
        {
            Creature creature = _run.Player.Creature;
            SO_MeatCleaver cleaver = _run.Player.Relics.OfType<SO_MeatCleaver>().FirstOrDefault();
            List<SO_Relic> digCandidates = CollectDigCandidates();

            while (true)
            {
                List<SO_RestOption> shown = BuildOptions(cleaver, digCandidates, out RestOption[] options);
                _options.Show(options);
                int choice = await _options.WaitChoiceAsync();
                _options.Hide(); // 点选即收：选项按钮与悬停描述立刻消失，画面只留特效与结果

                // 选项序与 BuildOptions 一致（配置列表顺序）；置灰选项吞点击（基类 Enabled 接管），故 choice 只会落在可用项上
                SO_RestOption chosen = shown[choice];
                if (chosen is SO_RestOption_Heal heal)
                {
                    await RestAsync(creature, heal);
                    break;
                }

                if (chosen is SO_RestOption_Forge)
                {
                    if (await ForgeAsync()) break;
                    continue; // 锻造取消：回到选项重选
                }

                if (chosen is SO_RestOption_Cook)
                {
                    if (await CookAsync(cleaver)) break;
                    continue; // 烹饪取消：回到选项重选
                }

                // 挖掘（组合根白名单保证只剩这一种类型）：点进来即说明候选非空；
                // 先领取（同步触发 RelicAdded 补遗物栏槽位）再播涟漪，槽位才找得到
                SO_Relic dug = _run.Rng.TakeShuffled(digCandidates, 1)[0];
                _run.Player.AddRelic(dug);
                _notifier.NotifyTriggered(dug); // 仅播涟漪，不触发遗物效果（效果只在 RelicEffect.PerformAsync 里跑）
                break;
            }

            // 「继续」不是一次性按钮：每点一次都开地图并保留本段（卸载推迟到选定下一个节点）；
            // 玩家把地图关掉后再点继续，还能再次打开。订阅随段销毁一起消失，无需反注册
            _proceedButton.gameObject.SetActive(true);
            _proceedButton.Clicked += OnProceedButtonClicked;
        }

        // 点「继续」：开地图并保留本段；不是一次性，关掉地图后再点可再开
        private void OnProceedButtonClicked() => _segmentManager.CompleteSegment();

        /// <summary>按配置列表组装选项：持有切肉刀/铲子才放行烹饪/挖掘；同时算好挖掘候选
        /// （Common+Rare 合并过滤已持有，照 AncientSession.RollChoices 口径）。
        /// 返回与 options_ 平行的配置列表，点选后按它分派结算。</summary>
        private List<SO_RestOption> BuildOptions(SO_MeatCleaver cleaver_, List<SO_Relic> digCandidates_, out RestOption[] options_)
        {
            List<SO_RestOption> configs = new(_restOptions.Count);
            List<RestOption> opts = new(_restOptions.Count);
            bool hasShovel = _run.Player.Relics.Any(static r => r is SO_Shovel);

            for (int i = 0; i < _restOptions.Count; i++)
            {
                SO_RestOption option = _restOptions[i];
                if (option is SO_RestOption_Cook && cleaver_ == null) continue;
                if (option is SO_RestOption_Dig && !hasShovel) continue;

                bool enabled;
                string dynamicText;
                if (option is SO_RestOption_Heal heal)
                {
                    int amount = Mathf.CeilToInt(_run.Player.Creature.MaxHp * heal.HealMaxHpRatio);
                    enabled = true;
                    dynamicText = $"回复 <color=green>{Mathf.RoundToInt(heal.HealMaxHpRatio * 100f)}</color>% 生命值（{amount}）";
                }
                else if (option is SO_RestOption_Forge)
                {
                    enabled = _run.Player.Deck.Cards.Any(static c => c.CanUpgrade);
                    dynamicText = null; // 说明只有静态文本
                }
                else if (option is SO_RestOption_Cook)
                {
                    enabled = _run.Player.Deck.Count >= cleaver_.RemoveCards;
                    dynamicText = $"删除 <color=yellow>{cleaver_.RemoveCards}</color> 张牌，<br>并提高 <color=green>{cleaver_.HpGain}</color> 点最大生命值";
                }
                else // SO_RestOption_Dig（组合根白名单外无其他类型）
                {
                    enabled = digCandidates_.Count > 0;
                    dynamicText = null;
                }

                // 说明：静态选项整句照抄资产，动态选项用插值文本
                opts.Add(new RestOption(option.OptionName, option.IsStaticDescription ? option.Description : dynamicText, option.Icon, enabled));
                configs.Add(option);
            }

            options_ = opts.ToArray();
            return configs;
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


        // 休息：烟雾播完按配置比例回复生命（向上取整，钳到上限）
        private async UniTask RestAsync(Creature creature_, SO_RestOption_Heal heal_)
        {
            await _vfxRoot.PlayRestSmokeAsync();
            int amount = Mathf.CeilToInt(creature_.MaxHp * heal_.HealMaxHpRatio);
            creature_.CurrentHp = Mathf.Min(creature_.MaxHp, creature_.CurrentHp + amount);
        }

        /// <summary>锻造：休息段一次只升一张，其余（选卡、预览、升级、锤击与关板）全归面板自己管；
        /// 交还 null 即玩家取消，回到选项重选。</summary>
        private async UniTask<bool> ForgeAsync()
        {
            _panel.Show(_run.Player, 1);
            return await _panel.WaitResultAsync() != null;
        }

        /// <summary>烹饪：开删牌弹板等玩家选出要移除的牌；确认时弹板内部已移出牌组并播完删牌演出，
        /// 这里只负责提高最大生命（当前生命同步加，顶栏经 HpChanged 自动刷新）。关闭弹板交还 null 即取消烹饪。</summary>
        private async UniTask<bool> CookAsync(SO_MeatCleaver cleaver_)
        {
            _removePanel.Show(_run.Player, cleaver_.RemoveCards);
            IReadOnlyList<CardModel> removed = await _removePanel.WaitResultAsync();
            if (removed == null) return false; // 关板收起已由面板自管

            _run.Player.Creature.RaiseMaxHp(cleaver_.HpGain);
            return true;
        }
    }
}
