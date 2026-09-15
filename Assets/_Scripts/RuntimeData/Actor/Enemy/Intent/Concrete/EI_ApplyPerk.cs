using _DreamKeep.ConfigData;

namespace _DreamKeep.RuntimeData
{
    /// <summary>施加状态意图：给自己或玩家挂载一份状态（层数已由装配期写进实例）。</summary>
    public class EI_ApplyPerk : EnemyIntent
    {
        public BattlePerk Perk { get; }
        public bool IsSelf { get; }

        public EI_ApplyPerk(BattlePerk perk_, bool isSelf_)
        {
            Perk = perk_;
            IsSelf = isSelf_;
        }
    }
}
