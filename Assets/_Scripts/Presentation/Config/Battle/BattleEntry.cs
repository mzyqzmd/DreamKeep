using Cysharp.Threading.Tasks;
using VContainer.Unity;
using _DreamKeep.GameBattle;

namespace _DreamKeep.Presentation
{
    /// <summary>战斗段表现层入口：容器装配完成后启动战斗循环，是"这场战斗从哪里开始"的唯一落点。</summary>
    public class BattleEntry : IStartable
    {
        private readonly BattleLoop _loop;

        public BattleEntry(BattleLoop loop_) => _loop = loop_;

        public void Start() => _loop.Start().Forget();
    }
}
