namespace _DreamKeep.Gameplay
{
    /// <summary>玩家角色：全场唯一我方单位，敌方攻击的直接承伤目标，也是唯一持有能量的一方。</summary>
    public class Player : Actor
    {
        public int Energy { get; set; }

        public Player(int health_, int block_) : base(health_, block_)
        {
        }
    }
}
