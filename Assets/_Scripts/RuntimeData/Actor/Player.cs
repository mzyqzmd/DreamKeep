namespace _DreamKeep.RuntimeData
{
    /// <summary>玩家：全场唯一的我方单位。持有生物数据、局内卡组与战斗态（战斗态随战斗新建与清空，卡组跨战斗保留）。
    /// TODO（系统未落地，先记名不落字段）：金币 Gold（含 GoldChanged 事件）、遗物 Relics、药水槽 PotionSlots、
    /// 法球槽位数 BaseOrbSlotCount、扩展字段 ExtraFields、开局难度等级；遗物抽取袋 RelicGrabBag 随遗物一起做。</summary>
    public class Player
    {
        public Creature Creature { get; }

        /// <summary>局内完整卡组：跨战斗保留，元素为局内卡（战斗卡由它逐张克隆而来）。</summary>
        public CardPile Deck { get; } = new();

        /// <summary>当前战斗态；不在战斗内时为 null。</summary>
        public PlayerCombatState CombatState { get; private set; }

        public Player(int maxHp_) => Creature = new Creature(maxHp_);

        #region 进战斗：按配置的能量上限新建战斗态
        public void StartCombat(int maxEnergy_) => CombatState = new PlayerCombatState(maxEnergy_);
        #endregion

        #region 战斗结束：清空战斗态，局内卡组不受影响
        public void EndCombat() => CombatState = null;
        #endregion
    }
}
