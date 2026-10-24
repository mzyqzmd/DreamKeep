using _DreamKeep.RuntimeData;

namespace _DreamKeep.GameBattle
{
    /// <summary>致命伤拦截者：在伤害真正写入生命之前被询问，拦下即改写结算后的生命值。
    /// 由域外实现（遗物等），战斗域只认识本接口、不认识具体来源；实现必须同步——此刻处在动作结算内部。</summary>
    public interface IDeathGuard
    {
        /// <summary>致命伤即将生效：返回 true 表示拦下，hpAfter_ 给出拦截后的生命值。</summary>
        bool TryPreventDeath(BattleContext context_, Creature target_, int hpLoss_, out int hpAfter_);
    }
}
