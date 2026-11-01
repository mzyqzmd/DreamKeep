using Cysharp.Threading.Tasks;
using UnityEngine;
using VContainer.Unity;

namespace _DreamKeep.Presentation
{
    /// <summary>宝箱段入口：本段"从哪里开始"的唯一落点——开箱演出 → 中心出遗物 → 领取 → 闪光锁定 → 等继续。
    /// 领取失败（候选已失效等装配外异常）只报错并重开点选，不吞不崩。收段归继续按钮自身（可重复点）。</summary>
    public sealed class ChestEntry : IStartable
    {
        private readonly ChestSession _session;
        private readonly UI_Chest _chest;
        private readonly UI_RelicSelectPanel _panel;
        private readonly UI_ChestProceedButton _proceedButton;

        public ChestEntry(ChestSession session_, UI_Chest chest_, UI_RelicSelectPanel panel_, UI_ChestProceedButton proceedButton_)
        {
            _session = session_;
            _chest = chest_;
            _panel = panel_;
            _proceedButton = proceedButton_;
        }

        public void Start() => PlayAsync().Forget();

        private async UniTask PlayAsync()
        {
            await _chest.WaitOpenedAsync(); // 玩家悬停换肤、点击开箱，播到 50% 放行出遗物

            _panel.ShowChoice(_session.Reward); // 候选为空只报错保持空态
            while (true)
            {
                await _panel.WaitClaimAsync();
                if (_session.Claim()) break;
                Debug.LogError($"[{nameof(ChestEntry)}] 遗物入列失败，已重开点选");
            }

            await _chest.PlayShineFadeAndLockAsync(); // 宝箱闪光并锁定交互
            _proceedButton.Show();
            await _proceedButton.WaitProceedAsync(); // 只等首次点击收束流程，收段开地图由按钮每次点击触发
        }
    }
}
