using System;
using System.Collections.Generic;
using _DreamKeep.ConfigData;
using _DreamKeep.Utility;

namespace _DreamKeep.RuntimeData
{
    /// <summary>整局抽取台账：战斗遭遇与事件按洗牌袋去重——抽过的不再出现，直到候选全轮过一遍后清空重置全池随机。
    /// 弱/强/精英 14 种遭遇资产互不相交，共用一集合按池清交集重置与分档独立集合行为等价；
    /// 纯 C#，随机流走整局 Rng（每档每次抽取消耗一格，与旧实现相同），同种子可复现；
    /// 已用记录由 RunStateRestore 拍平进存档，读档不丢去重窗口。</summary>
    public sealed class RunDrawLedger
    {
        private readonly HashSet<SO_Encounter> _encounters = new();
        private readonly HashSet<SO_Event> _events = new();

        #region 抽遭遇：滤掉已用后随机取一件并登记，候选全用过则清交集重置全池

        public SO_Encounter DrawEncounter(Rng rng_, IReadOnlyList<SO_Encounter> pool_) => Draw(rng_, pool_, _encounters);

        #endregion

        #region 抽事件：滤掉已用后随机取一则并登记，候选全用过则清交集重置全池

        public SO_Event DrawEvent(Rng rng_, IReadOnlyList<SO_Event> pool_) => Draw(rng_, pool_, _events);

        #endregion

        // 洗牌袋核心：空池返回默认值不登记（池空校验由调用方负责，对齐旧 Rng.Pick 行为）；
        // 重置只清本次候选池内的记录——共用集合里其它档位的已用记录不动
        private T Draw<T>(Rng rng_, IReadOnlyList<T> pool_, HashSet<T> used_)
        {
            if (pool_.Count == 0) return default;
            List<T> remaining = new(pool_.Count);
            for (int i = 0; i < pool_.Count; i++)
                if (!used_.Contains(pool_[i])) remaining.Add(pool_[i]);
            if (remaining.Count == 0)
            {
                for (int i = 0; i < pool_.Count; i++) used_.Remove(pool_[i]);
                remaining.AddRange(pool_);
            }

            T drawn = rng_.Pick(remaining);
            used_.Add(drawn);
            return drawn;
        }

        #region 存档数据口：拍平侧枚举已用、读档侧整批灌回（空引用跳过，目录查不到的资产静默丢弃）

        internal IEnumerable<SO_Encounter> UsedEncounters => _encounters;
        internal IEnumerable<SO_Event> UsedEvents => _events;

        internal void RestoreEncounters(IReadOnlyList<SO_Encounter> used_) => Restore(_encounters, used_);
        internal void RestoreEvents(IReadOnlyList<SO_Event> used_) => Restore(_events, used_);

        private static void Restore<T>(HashSet<T> used_, IReadOnlyList<T> items_)
        {
            for (int i = 0; i < items_.Count; i++)
                if (items_[i] != null) used_.Add(items_[i]);
        }

        #endregion
    }
}
