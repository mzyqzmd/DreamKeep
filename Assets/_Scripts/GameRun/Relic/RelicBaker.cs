using System;
using _DreamKeep.ConfigData;
using _DreamKeep.GameBattle;

namespace _DreamKeep.GameRun
{
    /// <summary>遗物效果装配器：按遗物配置资产类型分派到对应效果类，未登记的类型直接抛错——装配期暴露、防静默失效。
    /// 需要"结算前介入"的效果（如致命伤拦截）在这里登记进战斗态，战斗域只按接口询问。</summary>
    public static class RelicBaker
    {
        public static RelicEffect Bake(SO_Relic config_, BattleContext context_, ActionSystem actions_,
            BattleEventDispatcher dispatcher_, RelicTriggerNotifier notifier_)
        {
            RelicEffect effect = config_ switch
            {
                SO_BurningBlood data => new RE_BurningBlood(data, context_, actions_, dispatcher_, notifier_),
                SO_Anchor data => new RE_Anchor(data, context_, actions_, dispatcher_, notifier_),
                SO_HappyFlower data => new RE_HappyFlower(data, context_, actions_, dispatcher_, notifier_),
                SO_Shuriken data => new RE_Shuriken(data, context_, actions_, dispatcher_, notifier_),
                SO_Pendulum data => new RE_Pendulum(data, context_, actions_, dispatcher_, notifier_),
                SO_CentennialPuzzle data => new RE_CentennialPuzzle(data, context_, actions_, dispatcher_, notifier_),
                SO_GremlinHorn data => new RE_GremlinHorn(data, context_, actions_, dispatcher_, notifier_),
                SO_Orichalcum data => new RE_Orichalcum(data, context_, actions_, dispatcher_, notifier_),
                SO_LizardTail data => new RE_LizardTail(data, context_, actions_, dispatcher_, notifier_),
                _ => throw new ArgumentOutOfRangeException(nameof(config_), $"{config_.name} 的效果类型未登记"),
            };

            // 登记"结算前介入"的效果
            if (effect is IDeathGuard guard) context_.DeathGuards.Add(guard);
            return effect;
        }
    }
}
