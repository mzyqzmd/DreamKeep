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

        /// <summary>整局唯一随机流：洗牌、随机目标、遭遇血量浮动都走它，保证同种子可复现。</summary>
        public Rng Rng { get; }

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

        public RunState(SO_Player config_, int seed_)
        {
            Config = config_;
            Rng = new Rng(seed_);
            Gold = config_.StartGold;
            Player = new Player(config_.Name, config_.MaxHp, config_.StartRelics, config_.StartPotions);

            // 配置 → 局内卡（canonical 原型克隆成可变副本，跨战斗保留）
            List<SO_Card> deck = config_.StartDeck;
            for (int i = 0; i < deck.Count; i++)
                Player.Deck.Add(CardModelBaker.Bake(deck[i]).ToMutable());
        }
    }
}
