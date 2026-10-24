using System.Collections.Generic;
using _DreamKeep.ConfigData;

namespace _DreamKeep.RuntimeData
{
    /// <summary>玩家：全场唯一的我方单位。持有生物数据、遗物、局内卡组与战斗态（战斗态随战斗新建与清空，卡组与遗物跨战斗保留）。
    /// TODO（系统未落地，先记名不落字段）：金币 Gold（含 GoldChanged 事件）、药水槽 PotionSlots、
    /// 法球槽位数 BaseOrbSlotCount、扩展字段 ExtraFields、开局难度等级；遗物抽取袋 RelicGrabBag 随遗物获取流程一起做。</summary>
    public class Player
    {
        public Creature Creature { get; }

        /// <summary>整局持有的遗物：直接引用配置资产（遗物无局面状态），将来由获取流程动态增删。</summary>
        public IReadOnlyList<SO_Relic> Relics { get; }

        /// <summary>局内完整卡组：跨战斗保留，元素为局内卡（战斗卡由它逐张克隆而来）。</summary>
        public CardPile Deck { get; } = new();

        /// <summary>当前战斗态；不在战斗内时为 null。</summary>
        public PlayerCombatState CombatState { get; private set; }

        public Player(string actorName_, int maxHp_, IReadOnlyList<SO_Relic> startRelics_)
        {
            Creature = new Creature(actorName_, maxHp_);
            Relics = startRelics_;
        }

        #region 进战斗：按配置的能量上限新建战斗态

        public void StartCombat(int maxEnergy_) => CombatState = new PlayerCombatState(maxEnergy_);

        #endregion

        #region 战斗结束：清空战斗态，局内卡组不受影响

        public void EndCombat() => CombatState = null;

        #endregion
    }
}