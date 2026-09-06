using _DreamKeep.Core;

namespace _DreamKeep.Domain
{
    /// <summary>敌人：占据一个固定阵型槽位，槽位索引即从左到右的站位次序。</summary>
    public class Enemy : Actor
    {
        public int Slot { get; }
        public Enemy(int health_, int block_) : base(health_, block_)
        {
        }
    }
}
