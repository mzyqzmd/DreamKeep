using System;

namespace _DreamKeep.ConfigData
{
    /// <summary>不能被打出词条：标记型。带此词条的卡无法被打出，拖出后由玩法层驳回、卡牌退回手牌原槽位。</summary>
    [Serializable]
    [AddTypeMenu("Affix/不能被打出")]
    public sealed class AD_UnableToPlay : AffixData
    {
        public override string Name => "不能被打出";
        public override bool ShowDescriptionCardView => false;
    }
}
