using System.Collections.Generic;
using _DreamKeep.RuntimeData;

namespace _DreamKeep.GameRun
{
    /// <summary>选项就位：抉择页、遗物二选一与结果页继续共用；WithProceed 为真时末尾追加继续按钮。</summary>
    public struct EE_OptionsOffered : IEventSegmentEvent
    {
        public IReadOnlyList<EventOptionViewModel> Options { get; }
        public bool WithProceed { get; }

        public EE_OptionsOffered(IReadOnlyList<EventOptionViewModel> options_, bool withProceed_)
        {
            Options = options_;
            WithProceed = withProceed_;
        }
    }
}
