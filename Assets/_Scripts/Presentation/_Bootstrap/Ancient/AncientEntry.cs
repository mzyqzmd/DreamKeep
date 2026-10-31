using Cysharp.Threading.Tasks;
using UnityEngine;
using VContainer.Unity;
using _DreamKeep.ConfigData;
using _DreamKeep.GameRun;

namespace _DreamKeep.Presentation
{
    /// <summary>先古之民段入口：本段"从哪里开始"的唯一落点——名号横幅 → 对话演绎 → 三选一领取 → 开地图并保留本段。
    /// 领取失败（候选已失效等装配外异常）只报错并重开三选一，不吞不崩。</summary>
    public sealed class AncientEntry : IStartable
    {
        private readonly AncientSession _session;
        private readonly UI_AncientDialoguePanel _panel;
        private readonly UI_AncientOptionGroup _options;
        private readonly UI_AncientPopRoot _popRoot;
        private readonly SegmentSceneManager _segmentManager;

        public AncientEntry(AncientSession session_, UI_AncientDialoguePanel panel_, UI_AncientOptionGroup options_,
            UI_AncientPopRoot popRoot_, SegmentSceneManager segmentManager_)
        {
            _session = session_;
            _panel = panel_;
            _options = options_;
            _popRoot = popRoot_;
            _segmentManager = segmentManager_;
        }

        public void Start() => PlayAsync().Forget();

        private async UniTask PlayAsync()
        {
            SO_Ancient config = _session.Config;
            _panel.BindNameplate(config.AncientName, config.PositionName);
            _popRoot.PlayBanner(config.AncientName, config.PositionName);

            AncientDialoguePage page = _session.CurrentPage;
            if (page != null)
                await _panel.PlayPageAsync(page, config.AncientView, config.PlayerView);

            while (true)
            {
                _options.Show(_session.Choices);
                int index = await _options.WaitChoiceAsync();
                if (_session.Claim(index)) break;
                Debug.LogError($"[{nameof(AncientEntry)}] 遗物入列失败（候选下标 {index}），已重开三选一");
            }

            _options.Hide();
            _segmentManager.CompleteSegment();
        }
    }
}
