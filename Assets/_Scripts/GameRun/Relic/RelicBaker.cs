using System;
using _DreamKeep.ConfigData;
using _DreamKeep.GameBattle;

namespace _DreamKeep.GameRun
{
    /// <summary>遗物效果装配器：按遗物配置资产类型分派到对应效果类。未登记类型抛错——装配期暴露、防静默失效；
    /// 显式登记为 null 表示该遗物无战斗内效果（纯局外工具），与 PerkBaker 的 null 先例同口径。
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

                // 战斗开场
                SO_Vajra data => new RE_Vajra(data, context_, actions_, dispatcher_, notifier_),
                SO_OddlySmoothStone data => new RE_OddlySmoothStone(data, context_, actions_, dispatcher_, notifier_),
                SO_BagOfMarbles data => new RE_BagOfMarbles(data, context_, actions_, dispatcher_, notifier_),
                SO_RedMask data => new RE_RedMask(data, context_, actions_, dispatcher_, notifier_),
                SO_FestivePopper data => new RE_FestivePopper(data, context_, actions_, dispatcher_, notifier_),
                SO_BloodVial data => new RE_BloodVial(data, context_, actions_, dispatcher_, notifier_),
                SO_BagOfPreparation data => new RE_BagOfPreparation(data, context_, actions_, dispatcher_, notifier_),
                SO_Bellows data => new RE_Bellows(data, context_, actions_, dispatcher_, notifier_),

                // 回合节奏
                SO_Lantern data => new RE_Lantern(data, context_, actions_, dispatcher_, notifier_),
                SO_Candelabra data => new RE_Candelabra(data, context_, actions_, dispatcher_, notifier_),
                SO_Chandelier data => new RE_Chandelier(data, context_, actions_, dispatcher_, notifier_),
                SO_Bread data => new RE_Bread(data, context_, actions_, dispatcher_, notifier_),
                SO_ArtOfWar data => new RE_ArtOfWar(data, context_, actions_, dispatcher_, notifier_),
                SO_PocketWatch data => new RE_PocketWatch(data, context_, actions_, dispatcher_, notifier_),
                SO_HornCleat data => new RE_HornCleat(data, context_, actions_, dispatcher_, notifier_),
                SO_CaptainsWheel data => new RE_CaptainsWheel(data, context_, actions_, dispatcher_, notifier_),
                SO_Lipstick data => new RE_Lipstick(data, context_, actions_, dispatcher_, notifier_),
                SO_MercuryHourglass data => new RE_MercuryHourglass(data, context_, actions_, dispatcher_, notifier_),
                SO_Brimstone data => new RE_Brimstone(data, context_, actions_, dispatcher_, notifier_),
                SO_CloakClasp data => new RE_CloakClasp(data, context_, actions_, dispatcher_, notifier_),
                SO_ParryShield data => new RE_ParryShield(data, context_, actions_, dispatcher_, notifier_),
                SO_RippleBasin data => new RE_RippleBasin(data, context_, actions_, dispatcher_, notifier_),
                SO_StoneCalendar data => new RE_StoneCalendar(data, context_, actions_, dispatcher_, notifier_),

                // 出牌反应
                SO_Kunai data => new RE_Kunai(data, context_, actions_, dispatcher_, notifier_),
                SO_Nunchaku data => new RE_Nunchaku(data, context_, actions_, dispatcher_, notifier_),
                SO_OrnamentalFan data => new RE_OrnamentalFan(data, context_, actions_, dispatcher_, notifier_),
                SO_Kusarigama data => new RE_Kusarigama(data, context_, actions_, dispatcher_, notifier_),
                SO_TuningFork data => new RE_TuningFork(data, context_, actions_, dispatcher_, notifier_),
                SO_LetterOpener data => new RE_LetterOpener(data, context_, actions_, dispatcher_, notifier_),
                SO_ChessPiece data => new RE_ChessPiece(data, context_, actions_, dispatcher_, notifier_),
                SO_FrozenCrystal data => new RE_FrozenCrystal(data, context_, actions_, dispatcher_, notifier_),
                SO_TerrifyingHelmet data => new RE_TerrifyingHelmet(data, context_, actions_, dispatcher_, notifier_),
                SO_RazorTooth data => new RE_RazorTooth(data, context_, actions_, dispatcher_, notifier_),

                // 消耗与弃牌
                SO_GoldPaper data => new RE_GoldPaper(data, context_, actions_, dispatcher_, notifier_),
                SO_CharonsAshes data => new RE_CharonsAshes(data, context_, actions_, dispatcher_, notifier_),
                SO_ForgottenSoul data => new RE_ForgottenSoul(data, context_, actions_, dispatcher_, notifier_),

                // 受伤反应
                SO_DemonTongue data => new RE_DemonTongue(data, context_, actions_, dispatcher_, notifier_),
                SO_HandDrill data => new RE_HandDrill(data, context_, actions_, dispatcher_, notifier_),
                SO_RedSkull data => new RE_RedSkull(data, context_, actions_, dispatcher_, notifier_),

                // 洗牌、手牌状态与战斗结束
                SO_Abacus data => new RE_Abacus(data, context_, actions_, dispatcher_, notifier_),
                SO_UnceasingTop data => new RE_UnceasingTop(data, context_, actions_, dispatcher_, notifier_),
                SO_MeatOnTheBone data => new RE_MeatOnTheBone(data, context_, actions_, dispatcher_, notifier_),

                // 纯局外效果：无战斗内行为（挖掘/烹饪资格由 RestEntry 判定，拾起时效果由先古段结算），显式登记 null 豁免
                SO_Shovel => null,
                SO_MeatCleaver => null,
                SO_GoldenPearl => null,
                SO_NutritiousOyster => null,
                SO_Pomander => null,
                SO_PrecariousShears => null,
                SO_ArcaneScroll => null,
                SO_SmallCapsule => null,

                _ => throw new ArgumentOutOfRangeException(nameof(config_), $"{config_.name} 的效果类型未登记"),
            };

            // 登记"结算前介入"的效果
            if (effect is IDeathGuard guard) context_.DeathGuards.Add(guard);
            return effect;
        }
    }
}
