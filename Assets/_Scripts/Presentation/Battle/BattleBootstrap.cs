using Cysharp.Threading.Tasks;
using VContainer.Unity;
using _DreamKeep.Gameplay;

namespace _DreamKeep.Presentation
{
    /// <summary>战斗启动入口：容器构建完成后启动战斗循环。原先的 BattleEventPump 已删除，此处以最小 EntryPoint 恢复启动。</summary>
    public class BattleBootstrap : IStartable
    {
        private readonly BattleLoop _loop;

        public BattleBootstrap(BattleLoop loop_) => _loop = loop_;

        public void Start() => _loop.Start().Forget();
    }
}
