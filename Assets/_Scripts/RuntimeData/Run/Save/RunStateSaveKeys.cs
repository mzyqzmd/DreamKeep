namespace _DreamKeep.RuntimeData
{
    /// <summary>整局存档键的唯一真源：拍平与重建两侧共用同一组常量。
    /// 键名改动只允许发生在这里——写读各写一遍字面量时，拼错一个字符不会报错，读档只会静默回退默认值。</summary>
    internal static class RunStateSaveKeys
    {
        #region 整局标量

        public const string Gold = "gold";
        public const string Floor = "floor";
        public const string Removals = "removals";
        public const string Elapsed = "elapsed";
        public const string Rng = "rng";

        #endregion

        #region 三份配置：资产 GUID

        public const string PlayerConfig = "playerConfig";
        public const string MapConfig = "mapConfig";
        public const string FallbackPool = "fallbackPool";

        #endregion

        #region 地图：节点平行表、子边扁平表、锚点与已通过

        public const string MapWidth = "map.width";
        public const string MapRows = "map.rows";
        public const string MapCols = "map.cols";
        public const string MapRowsData = "map.rowsData";
        public const string MapTypes = "map.types";
        public const string MapRevealed = "map.revealed";
        public const string MapModifiable = "map.modifiable";
        public const string MapEdges = "map.edges";
        public const string MapStart = "start";
        public const string MapBoss = "map.boss";
        public const string MapCurrent = "map.current";
        public const string MapCleared = "cleared";
        public const string MapPending = "map.pending";

        #endregion

        #region 玩家：生命、清单 GUID、卡组与跨节点计数

        public const string PlayerName = "player.name";
        public const string PlayerHp = "player.hp";
        public const string PlayerMaxHp = "player.maxHp";
        public const string PlayerRelics = "player.relics";
        public const string PlayerPotions = "player.potions";
        public const string PlayerDeck = "player.deck";
        public const string PlayerDeckUpgraded = "player.deckUpgraded";
        public const string PlayerCounterKeys = "player.counterKeys";
        public const string PlayerCounterValues = "player.counterValues";

        #endregion

        #region 待领奖励：条目平行表 + 卡牌组扁平表

        public const string RewardKinds = "reward.kinds";
        public const string RewardGold = "reward.gold";
        public const string RewardPotions = "reward.potions";
        public const string RewardRelics = "reward.relics";
        public const string RewardCardGroup = "reward.cardGroup";
        public const string RewardCardSource = "reward.cardSource";
        public const string RewardCardUpgraded = "reward.cardUpgraded";

        #endregion
    }
}
