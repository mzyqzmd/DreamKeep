using System.Collections.Generic;
using _DreamKeep.RuntimeData;

namespace _DreamKeep.GameRun
{
    /// <summary>休息选项就位：视图按本列表重建选项条与悬停说明。</summary>
    public struct REE_OptionsOffered : IRestEvent
    {
        public IReadOnlyList<RestOption> Options { get; }

        public REE_OptionsOffered(IReadOnlyList<RestOption> options_) => Options = options_;
    }
}
