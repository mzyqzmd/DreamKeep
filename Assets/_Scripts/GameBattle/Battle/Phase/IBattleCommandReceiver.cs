using Cysharp.Threading.Tasks;

namespace _DreamKeep.GameBattle
{
    public interface IBattleCommandReceiver
    {
        UniTask HandleCommand(BattleCommand command_);
    }
}