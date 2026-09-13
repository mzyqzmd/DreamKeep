using _DreamKeep.RuntimeData;

namespace _DreamKeep.Gameplay
{
    /// <summary>格挡清空：记录被清空的目标，供举盾待机态退回普通待机。</summary>
    public struct BE_BlockCleared : IBattleEvent
    {
        public Creature Target { get; }

        public BE_BlockCleared(Creature target_) => Target = target_;
    }
}
