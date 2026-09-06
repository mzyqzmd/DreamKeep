using System;

namespace _DreamKeep.Data
{
    /// <summary>状态「易伤」：可施加数据，具体效果由 Domain 层结算。</summary>
    [Serializable]
    public class ED_Vulnerable : EffectData
    {
        public override string Name => "易伤";

        public override string Description => "受到的伤害提高 50%";
    }
}
