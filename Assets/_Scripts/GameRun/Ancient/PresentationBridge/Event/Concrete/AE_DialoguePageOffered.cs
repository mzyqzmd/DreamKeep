using _DreamKeep.ConfigData;

namespace _DreamKeep.GameRun
{
    /// <summary>对话页就位：视图逐条生成气泡并等待玩家点击推进，演完才放行三选一。</summary>
    public struct AE_DialoguePageOffered : IAncientEvent
    {
        public AncientDialoguePage Page { get; }
        public AncientSpeakerView AncientView { get; }
        public AncientSpeakerView PlayerView { get; }

        public AE_DialoguePageOffered(AncientDialoguePage page_, AncientSpeakerView ancientView_,
            AncientSpeakerView playerView_)
        {
            Page = page_;
            AncientView = ancientView_;
            PlayerView = playerView_;
        }
    }
}
