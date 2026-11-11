using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using _DreamKeep.RuntimeData;

namespace _DreamKeep.GameBattle
{
    /// <summary>手牌选择结果应用器：常驻订阅请求事件，等表现层交回选中列表后把效果动作追加进队列；
    /// 等待落在事件处理器内，故动作系统天然冻结到玩家确认为止。空列表视为跳过效果。</summary>
    public sealed class CardSelectionFlow
    {
        private readonly ActionSystem _actionSystem;
        private readonly BattleEventDispatcher _dispatcher;

        public CardSelectionFlow(ActionSystem actionSystem_, BattleEventDispatcher dispatcher_)
        {
            _actionSystem = actionSystem_;
            _dispatcher = dispatcher_;
            dispatcher_.Register<BE_CardSelectionRequested>(OnRequestedAsync);
        }

        #region 等结果并追加效果动作

        private async UniTask OnRequestedAsync(BE_CardSelectionRequested requested_)
        {
            IReadOnlyList<CardModel> selected = await requested_.Result.Task;
            _dispatcher.PlayAsync(new BE_CardSelectionFinished()).Forget();
            if (selected == null || selected.Count == 0) return;

            _actionSystem.Enqueue(requested_.Mode == ECardSelectMode.Upgrade
                ? new BA_ApplyCardUpgrade(requested_.Source, requested_.Targets, selected)
                : new BA_ApplyCardExhaust(requested_.Source, requested_.Targets, selected));
        }

        #endregion
    }
}
