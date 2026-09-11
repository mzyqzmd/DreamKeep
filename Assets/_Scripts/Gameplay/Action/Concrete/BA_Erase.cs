using System.Collections.Generic;
using _DreamKeep.RuntimeData;

namespace _DreamKeep.Gameplay
{
    /// <summary>遗忘动作：把自身这张卡从整局 run 永久移除，不进入任何牌堆。</summary>
    public class BA_Erase : BattleAction
    {
        /// <summary>延迟执行期间卡可能已离场，移除失败即视为离场。</summary>
        public CardModel Card { get; }

        public BA_Erase(Actor source_, List<Actor> targets_, CardModel card_) : base(source_, targets_) => Card = card_;

        protected override void OnExecute(BattleState battleState_, BattleEventBuffer events_)
        {
            if (!battleState_.Hand.Remove(Card)) return;

            // 有源卡登记原型 Id 供战斗结束回写卡组，战斗内临时卡查不到映射则跳过
            if (battleState_.TryGetSourceId(Card, out int sourceId))
                battleState_.RegisterErased(sourceId);

            events_.Emit(new IBE_CardErased(Card));
        }
    }
}
