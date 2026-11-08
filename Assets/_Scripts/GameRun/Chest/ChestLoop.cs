using Cysharp.Threading.Tasks;
using _DreamKeep.RuntimeData;

namespace _DreamKeep.GameRun
{
    /// <summary>宝箱段流程：一次性等待点由命令门面放行，玩法事实经宝箱段事件分发器播出；纯 C#，不认识场景与视图。
    /// 候选为空时跳过领取直接收尾，避免槽位无从生成而永久卡段；领取失败重开点选，
    /// 遗物入列即标记节点通过，「继续」每次点击都播一次段完成。</summary>
    public sealed class ChestLoop
    {
        private readonly ChestSession _session;
        private readonly RunState _run;
        private readonly ChestEventDispatcher _events;
        private readonly UniTaskCompletionSource _opening = new();
        private UniTaskCompletionSource _claim = new();
        private UniTaskCompletionSource _proceed = new();

        public ChestLoop(ChestSession session_, RunState run_, ChestEventDispatcher events_)
        {
            _session = session_;
            _run = run_;
            _events = events_;
        }

        public void Start() => RunAsync().Forget();

        public void OpenChest() => _opening.TrySetResult();

        public void ClaimRelic() => _claim.TrySetResult();

        public void Proceed() => _proceed.TrySetResult();

        // 等待点由构造期建好：命令早于 await 到达也不会丢，TrySetResult 会把任务永久留在完成态。
        // 循环型等待点在每轮放行后重置，重置放在 await 之后，不影响本轮已持有的 Task 引用。
        private async UniTask RunAsync()
        {
            await _opening.Task;
            await _events.PlayAsync(new CE_ChestOpening()); // 表现播到开箱动画放行比例才返回
            await _events.PlayAsync(new CE_RelicOffered(_session.Reward));

            // 候选为空：槽位不会生成、领取意图无从到达，直接跳过领取，把收段交给「继续」
            if (_session.Reward != null)
                while (true)
                {
                    await _claim.Task;
                    _claim = new UniTaskCompletionSource();
                    if (_session.Claim()) break;
                    await _events.PlayAsync(new CE_RelicOffered(_session.Reward)); // 入列失败：重开点选
                }

            _run.CompleteMapNode(); // 遗物领到手（或候选为空）即通过，不等闪光与「继续」
            await _events.PlayAsync(new CE_RelicClaimed());

            while (true)
            {
                await _proceed.Task;
                _proceed = new UniTaskCompletionSource();
                await _events.PlayAsync(new CE_ChestCompleted());
            }
        }
    }
}
