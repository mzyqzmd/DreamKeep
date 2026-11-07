using System;
using System.Collections.Generic;
using _DreamKeep.ConfigData;

namespace _DreamKeep.RuntimeData
{
    /// <summary>玩家：全场唯一的我方单位。持有生物数据、遗物、药水槽、局内卡组与战斗态（战斗态随战斗新建与清空，其余跨战斗保留）。
    /// 金币不属于战斗单位，住整局状态 RunState。
    /// 扩展字段 ExtraFields、开局难度等级；遗物抽取袋 RelicGrabBag 随遗物获取流程一起做。</summary>
    public class Player
    {
        /// <summary>药水槽位数：固定三格，空槽在列表里是 null。</summary>
        public const int PotionSlotCount = 3;

        private readonly List<SO_Potion> _potionSlots = new(PotionSlotCount);
        private readonly List<SO_Relic> _relics = new();

        public Creature Creature { get; }

        /// <summary>整局持有的遗物：自持一份列表（构造时从配置拷贝，不反过来污染配置资产），由获取流程动态追加。</summary>
        public IReadOnlyList<SO_Relic> Relics => _relics;

        /// <summary>遗物新增通知：顶栏遗物栏订阅，领到新遗物当场补一格。</summary>
        public event Action<SO_Relic> RelicAdded;

        /// <summary>整局持有的药水：下标即槽位，null 为空槽；使用或丢弃后置空，获取流程动态填补。</summary>
        public IReadOnlyList<SO_Potion> PotionSlots => _potionSlots;

        /// <summary>药水槽位变更通知：参数为槽位下标，顶栏药水栏订阅后只刷新那一格。</summary>
        public event Action<int> PotionSlotChanged;

        /// <summary>局内完整卡组：跨战斗保留，元素为局内卡（战斗卡由它逐张克隆而来）。</summary>
        public CardPile Deck { get; } = new();

        /// <summary>卡牌入组通知：获得卡演出订阅，领到新卡当场播「展示后飞入牌库」。</summary>
        public event Action<CardModel> CardAdded;

        /// <summary>当前战斗态；不在战斗内时为 null。</summary>
        public PlayerCombatState CombatState { get; private set; }

        public Player(string actorName_, int maxHp_, IReadOnlyList<SO_Relic> startRelics_,
            IReadOnlyList<SO_Potion> startPotions_)
        {
            Creature = new Creature(actorName_, maxHp_);
            // 断引用的配置项直接跳过：遗物栏按本表建槽，塞进 null 会当场炸容器
            for (int i = 0; i < startRelics_.Count; i++)
                if (startRelics_[i] != null) _relics.Add(startRelics_[i]);
            // 空槽也占位：槽位下标与视图下标一一对应，视图按同一份下标刷新
            for (int i = 0; i < PotionSlotCount; i++)
                _potionSlots.Add(i < startPotions_.Count ? startPotions_[i] : null);
        }

        #region 取用药水：取走即清空槽位，槽位越界或空槽返回 false

        public bool TryTakePotion(int slotIndex_, out SO_Potion potion_)
        {
            potion_ = null;
            if (slotIndex_ < 0 || slotIndex_ >= _potionSlots.Count) return false;
            potion_ = _potionSlots[slotIndex_];
            if (potion_ == null) return false;
            _potionSlots[slotIndex_] = null;
            PotionSlotChanged?.Invoke(slotIndex_);
            return true;
        }

        #endregion

        #region 获取药水：放进最靠前的空槽，槽位已满返回 false（奖励面板据此把条目解锁让玩家先喝一瓶）

        public bool TryAddPotion(SO_Potion potion_)
        {
            if (potion_ == null) return false;
            for (int i = 0; i < _potionSlots.Count; i++)
            {
                if (_potionSlots[i] != null) continue;
                _potionSlots[i] = potion_;
                PotionSlotChanged?.Invoke(i);
                return true;
            }

            return false;
        }

        #endregion

        #region 获取遗物：不做去重（去重只在抽取层），成功即通知遗物栏补槽

        public bool AddRelic(SO_Relic relic_)
        {
            if (relic_ == null) return false;
            _relics.Add(relic_);
            RelicAdded?.Invoke(relic_);
            return true;
        }

        public bool HasRelic(SO_Relic relic_) => relic_ != null && _relics.Contains(relic_);

        #endregion

        #region 获取卡牌：加入局内卡组——传进来的必须是 ToMutable 之后的局内卡，canonical 原型不进牌组

        public void AddCard(CardModel runCard_)
        {
            if (runCard_ == null) return;
            Deck.Add(runCard_);
            CardAdded?.Invoke(runCard_);
        }

        #endregion

        #region 进战斗：按配置的能量上限新建战斗态

        public void StartCombat(int maxEnergy_) => CombatState = new PlayerCombatState(maxEnergy_);

        #endregion

        #region 战斗结束：清空战斗态与身上状态，局内卡组不受影响

        public void EndCombat()
        {
            Creature.ClearPerks(); // 状态只在本场有效：不清会被带进下一场
            CombatState = null;
        }

        #endregion

        #region 跨节点遗物计数：开心小花/摆动球/金纸这类"每 N 回合或每 N 张"的计数额跨战斗保留，住整局层；战斗内型计数为效果实例私有不住此

        private readonly Dictionary<SO_Relic, int> _relicCounters = new();

        /// <summary>全部跨节点遗物计数：写档拍平用，key 为遗物配置资产。</summary>
        public IReadOnlyDictionary<SO_Relic, int> RelicCounters => _relicCounters;

        /// <summary>读取跨节点遗物当前计数，未记录即 0。</summary>
        public int RelicCounterOf(SO_Relic relic_) => _relicCounters.TryGetValue(relic_, out int count) ? count : 0;

        /// <summary>计数 +1 并写回，返回推进后的新值（key 不存在则从 1 起）。</summary>
        public int AdvanceRelicCounter(SO_Relic relic_)
        {
            _relicCounters.TryGetValue(relic_, out int count);
            _relicCounters[relic_] = ++count;
            return count;
        }

        /// <summary>计数归零。</summary>
        public void ResetRelicCounter(SO_Relic relic_) => _relicCounters[relic_] = 0;

        /// <summary>读档重建：按原下标写回药水槽，空槽传 null；只刷该格，不播获取演出。</summary>
        public void RestorePotionSlot(int slotIndex_, SO_Potion potion_)
        {
            if (slotIndex_ < 0 || slotIndex_ >= _potionSlots.Count) return;
            _potionSlots[slotIndex_] = potion_;
            PotionSlotChanged?.Invoke(slotIndex_);
        }

        /// <summary>读档重建：写回跨节点遗物计数的已知合法值。</summary>
        public void RestoreRelicCounter(SO_Relic relic_, int count_)
        {
            if (relic_ == null) return;
            _relicCounters[relic_] = count_;
        }

        #endregion
    }
}
