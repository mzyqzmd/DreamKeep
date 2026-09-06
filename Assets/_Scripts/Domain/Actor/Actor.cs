namespace _DreamKeep.Core
{
    public class Actor
    {
        public int Health { get; set; }
        public int Block { get; set; }
        public Actor(int health_, int block_)
        {
            Health = health_;
            Block = block_;
        }
    }
}
