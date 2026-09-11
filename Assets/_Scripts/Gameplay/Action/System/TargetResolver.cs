using System.Collections.Generic;
using _DreamKeep.RuntimeData;

namespace _DreamKeep.Gameplay
{
    /// <summary>落点解析器：把卡的落点规则与阵型解析成确定的目标列表，动作因此保持无状态。</summary>
    public class TargetResolver
    {
        #region 解析一张卡的目标列表：自己 / 全体 / 随机 / 指定（可贯穿）
        public List<Actor> Resolve(BattleState battleState_, CardModel card_, int aimSlot_)
        {
            CardTargetInfo info = CardBaker.GetTargetInfo(card_.CardId);
            List<Actor> targets = new();

            if (info.IsSelf)
            {
                targets.Add(battleState_.Player);
                return targets;
            }

            List<Enemy> alive = battleState_.GetAliveEnemies();
            if (alive.Count == 0) return targets;

            if (info.IsAllEnemies)
            {
                for (int i = 0; i < alive.Count; i++)
                    targets.Add(alive[i]);
                return targets;
            }

            if (info.IsRandom)
            {
                targets.Add(battleState_.Rng.Pick(alive));
                return targets;
            }

            // 默认落点为最前敌人（硬阻挡），需要指定时按槽位定位，未命中则回退最前
            int start = info.NeedsAim ? IndexOfSlot(alive, aimSlot_) : 0;
            return targets;
        }
        #endregion

        #region 查找指定槽位在存活敌人中的下标，不存在返回 -1
        private static int IndexOfSlot(List<Enemy> enemies_, int slot_)
        {
            for (int i = 0; i < enemies_.Count; i++)
                if (enemies_[i].Slot == slot_)
                    return i;
            return -1;
        }
        #endregion
    }
}
