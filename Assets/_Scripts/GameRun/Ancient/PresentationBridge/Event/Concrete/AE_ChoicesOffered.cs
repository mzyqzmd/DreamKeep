using System.Collections.Generic;
using _DreamKeep.ConfigData;

namespace _DreamKeep.GameRun
{
    /// <summary>三选一就位：候选与逐件可用性按序平行带出，置灰项仍占位显示。</summary>
    public struct AE_ChoicesOffered : IAncientEvent
    {
        public IReadOnlyList<SO_Relic> Choices { get; }
        public IReadOnlyList<bool> Claimable { get; }

        public AE_ChoicesOffered(IReadOnlyList<SO_Relic> choices_, IReadOnlyList<bool> claimable_)
        {
            Choices = choices_;
            Claimable = claimable_;
        }
    }
}
