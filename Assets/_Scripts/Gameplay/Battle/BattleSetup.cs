using System.Collections.Generic;
using _DreamKeep.RuntimeData;
using _DreamKeep.Utility;

namespace _DreamKeep.Gameplay
{
    /// <summary>战斗装配器：把局外牌组深拷贝成战斗牌组并装出战斗态，敌人意图复用卡牌烘焙链路。</summary>
    public static class BattleSetup
    {
        #region 装配一场战斗：局外牌组深拷贝为战斗牌组后构造战斗态
        public static BattleState Create(List<InGameCard> deck_, Player player_, List<Enemy> enemies_, Rng rng_, int energyPerTurn_) =>
            new(player_, enemies_, BattleCardBaker.BakeAll(deck_), rng_, energyPerTurn_);
        #endregion

        #region 构造一个敌人：意图取自配置卡的词条集合，敌我共用同一条结算管线
        public static Enemy CreateEnemy(int health_, int block_, int slot_, string intentCardId_) =>
            new(health_, block_, slot_, CardBaker.BakeAffixes(intentCardId_));
        #endregion
    }
}
