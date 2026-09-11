namespace _DreamKeep.Gameplay
{
    /// <summary>敌人：占据一个固定阵型槽位，槽位索引即从左到右的站位次序，当前只作为受击目标与胜负判定依据。</summary>
    public class Enemy : Actor
    {
        /// <summary>阵型槽位：敌人专属，玩家全场唯一故无需槽位。</summary>
        public int Slot { get; }

        public Enemy(int health_, int block_, int slot_) : base(health_, block_) => Slot = slot_;
    }
}
