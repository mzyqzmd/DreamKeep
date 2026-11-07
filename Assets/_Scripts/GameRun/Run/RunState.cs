using System;
using System.Collections.Generic;
using _DreamKeep.ConfigData;
using _DreamKeep.RuntimeData;
using _DreamKeep.Utility;

namespace _DreamKeep.GameRun
{
    /// <summary>整局状态：一局游戏里跨战斗延续的全部数据，战斗段只读它。
    /// 卡组在构造时从角色配置烘焙一次，之后升级、增删都作用在这份局内卡组上，不再被战斗重置。
    /// 纯 C# 类，不认识容器与引擎——组合根负责创建与持有。</summary>
    public class RunState
    {
        public SO_Player Config { get; }
        public Player Player { get; }

        /// <summary>整章地图：构造时从生成配置一次性生成，之后只随 EnterMapNode 演进。</summary>
        public MapGraph Map { get; }

        /// <summary>地图生成配置：问号揭示的权重与行限制都从这读。</summary>
        public SO_MapGenerationConfig MapConfig { get; }

        /// <summary>整局唯一随机流：洗牌、随机目标、遭遇血量浮动都走它，保证同种子可复现。</summary>
        public Rng Rng { get; }

        /// <summary>兜底遗物池：常规遗物池抽空时的降级来源（池内只放头环），随整局构建传入，RelicRoller 各入口经此取用。</summary>
        public SO_RelicPool FallbackRelicPool { get; }

        private int _gold;

        /// <summary>金币：set 内抛 GoldChanged，供整局 HUD（顶栏）订阅刷新，避免每帧轮询。</summary>
        public int Gold
        {
            get => _gold;
            set
            {
                if (_gold == value) return;
                _gold = value;
                GoldChanged?.Invoke(_gold);
            }
        }

        /// <summary>金币变更通知：整局层（顶栏）订阅。</summary>
        public event Action<int> GoldChanged;

        #region 加金币：奖励与事件统一走这里，取整避免误传 0 空刷一次事件

        public void GainGold(int amount_)
        {
            if (amount_ <= 0) return;
            Gold += amount_;
        }

        #endregion

        #region 支出金币：商店购买与事件统一走这里，钳到不为负；非正数忽略（误填负数由"反向加钱"变"忽略"）

        public void SpendGold(int amount_)
        {
            if (amount_ <= 0) return;
            Gold = Math.Max(0, Gold - amount_);
        }

        #endregion

        #region 回滚金币快照：取消自选面板这类事务回滚用，写回已知合法值，不做规则钳制

        public void RestoreGoldTo(int value_) => Gold = value_;

        #endregion

        #region 段级玩家生命写入：支出保底留 1 绝不致死（战斗内伤害允许杀人，故收在 GameRun 不放 Creature）、回复钳上限

        public void SpendPlayerHp(int amount_)
        {
            if (amount_ <= 0) return;
            Player.Creature.CurrentHp = Math.Max(1, Player.Creature.CurrentHp - amount_);
        }

        public void RestorePlayerHp(int amount_)
        {
            if (amount_ <= 0) return;
            Player.Creature.CurrentHp = Math.Min(Player.Creature.CurrentHp + amount_, Player.Creature.MaxHp);
        }

        #endregion

        #region 回滚生命快照：取消自选面板这类事务回滚用，写回已知合法值，双向可加可扣；不算回血（不播回血特效）

        public void RestorePlayerHpTo(int value_) => Player.Creature.RestoreHpTo(value_);

        #endregion

        private int _cardRemovalsUsed;

        /// <summary>整局在商店买过的删牌次数：商店删牌服务按它涨价，用得越多越贵。</summary>
        public int CardRemovalsUsed => _cardRemovalsUsed;

        #region 记一次商店删牌：成交后调用一次，价格由商店段自己按本值重算

        public void RecordCardRemoval() => _cardRemovalsUsed++;

        #endregion

        private int _floor = 1;

        /// <summary>当前层数，从 1 开始；由跑图流程推进。set 内抛 FloorChanged。</summary>
        public int Floor
        {
            get => _floor;
            set
            {
                if (_floor == value) return;
                _floor = value;
                FloorChanged?.Invoke(_floor);
            }
        }

        /// <summary>层数变更通知：整局层（顶栏 / 层栏）订阅。</summary>
        public event Action<int> FloorChanged;

        #region 进入地图节点：只记当前位置并推进层数（层数 = 行号 + 1，起点行即第 1 层）

        public void EnterMapNode(MapNode node_)
        {
            if (node_.PointType == EMapPointType.Unknown && node_.RevealedType == EMapPointType.None)
                node_.RevealedType = RollUnknownReveal(node_.Coord.Row); // 进入即揭示：切段、遭遇与图标都按真实内容走
            Map.EnterNode(node_);
            Floor = node_.Coord.Row + 1;
        }

        #endregion

        #region 整局计时：外部每帧喂增量累计已用时长（保持纯 C# 不直读引擎时钟；停表钩子待存档系统落地）

        private double _elapsedSeconds;

        /// <summary>整局已用秒数：顶栏读它展示，将来存档随 RunState 序列化。</summary>
        public double ElapsedSeconds => _elapsedSeconds;

        public void TickTime(double seconds_) => _elapsedSeconds += seconds_;

        #endregion

        #region 段进行态与通过判定：进段挂起整图可走态，内容完成当帧解除并标记通过（幂等）

        /// <summary>段进行态：进段时挂起，内容完成或段收尾时解除；挂起期间地图没有任何可走节点。</summary>
        public void SetSegmentPending(bool pending_) => Map.SetSegmentPending(pending_);

        public void CompleteMapNode()
        {
            Map.SetSegmentPending(false); // 内容完成当帧就解除挂起：下一层立刻可点，不等段收尾
            Map.MarkCleared(Map.CurrentNode);
        }

        #endregion

        #region 问号揭示：进入时按配置权重掷出真实内容，遵守与生成期相同的行限制

        // 加权取样走整局随机流，同种子可复现；行限制全滤空时兜底普通战斗
        private EMapPointType RollUnknownReveal(int row_)
        {
            List<EMapPointType> pool = new();
            AddToPool(EMapPointType.Monster, MapConfig.RevealMonster);
            AddToPool(EMapPointType.Elite, MapConfig.RevealElite);
            AddToPool(EMapPointType.Shop, MapConfig.RevealShop);
            AddToPool(EMapPointType.Chest, MapConfig.RevealTreasure);
            AddToPool(EMapPointType.Unknown, MapConfig.RevealEvent);
            return pool.Count == 0 ? EMapPointType.Monster : Rng.Pick(pool);

            void AddToPool(EMapPointType type_, int weight_)
            {
                if (weight_ <= 0 || !StandardMapGenerator.IsRowLegal(type_, row_, Map.Rows, MapConfig)) return;
                for (int i = 0; i < weight_; i++) pool.Add(type_);
            }
        }

        #endregion

        public RunState(SO_Player config_, int seed_, SO_MapGenerationConfig mapConfig_, SO_RelicPool fallbackRelicPool_)
        {
            Config = config_;
            MapConfig = mapConfig_;
            FallbackRelicPool = fallbackRelicPool_;
            Rng = new Rng(seed_);

            // 地图：整局一张，从主流派生子种子生成（消耗主流一格，同种子整局仍可复现）
            Map = StandardMapGenerator.Generate(mapConfig_, new Rng(Rng.Next(int.MaxValue)));
            EnterMapNode(Map.StartNode);

            Gold = config_.StartGold;
            Player = new Player(config_.Name, config_.MaxHp, config_.StartRelics, config_.StartPotions);

            // 配置 → 局内卡（canonical 原型克隆成可变副本，跨战斗保留）
            List<SO_Card> deck = config_.StartDeck;
            for (int i = 0; i < deck.Count; i++)
                Player.Deck.Add(CardModelBaker.Bake(deck[i]).ToMutable());
        }
    }
}