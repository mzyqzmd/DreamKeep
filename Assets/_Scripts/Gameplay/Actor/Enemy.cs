using System.Collections.Generic;
using _DreamKeep.RuntimeData;

namespace _DreamKeep.Gameplay
{
    /// <summary>敌人：占据一个固定阵型槽位，槽位索引即从左到右的站位次序。</summary>
    public class Enemy : Actor
    {
        public int Slot { get; }
        /// <summary>意图词条集合：与玩家卡共用同一条结算管线，仅目标方向相反。</summary>
        public List<CardAffix> Intent { get; }

        public Enemy(int health_, int block_, int slot_, List<CardAffix> intent_) : base(health_, block_)
        {
            Slot = slot_;
            Intent = intent_ ?? new List<CardAffix>();
        }
    }
}
