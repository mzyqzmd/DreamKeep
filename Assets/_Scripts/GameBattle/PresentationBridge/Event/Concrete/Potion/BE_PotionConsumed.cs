using System.Collections.Generic;
using _DreamKeep.ConfigData;
using _DreamKeep.RuntimeData;

namespace _DreamKeep.GameBattle
{
    /// <summary>药水离槽：使用或丢弃后广播，槽位下标与那瓶药水一起给出，视图据此清空槽位并复位悬停文案；
    /// 目标带上玩法层已解析的落点——非空即"被投出"，表现层据它演投掷飞行，丢弃为 null（只离槽、不飞）。</summary>
    public struct BE_PotionConsumed : IBattleEvent
    {
        public int SlotIndex { get; }
        public SO_Potion Potion { get; }

        /// <summary>这瓶药水要落在谁身上；丢弃时为 null。</summary>
        public IReadOnlyList<Creature> Targets { get; }

        public BE_PotionConsumed(int slotIndex_, SO_Potion potion_, IReadOnlyList<Creature> targets_ = null)
        {
            SlotIndex = slotIndex_;
            Potion = potion_;
            Targets = targets_;
        }
    }
}
