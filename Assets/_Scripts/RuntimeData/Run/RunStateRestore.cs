using System;
using System.Collections.Generic;
using _DreamKeep.ConfigData;
using _DreamKeep.EngineUtility; // 同 RunState：存档接口 + 资产 GUID 目录，属唯一允许的域层反引
using _DreamKeep.Utility;

namespace _DreamKeep.RuntimeData
{
    /// <summary>整局状态读档重建转换器：把读写袋里的平行表还原成 MapGraph、Player 与 Rng，
    /// 再经 internal 读档构造器造出 RunState。卡组只按「配置 GUID + 是否升级」重建，词条与数值补丁由烘焙期决定。</summary>
    public static class RunStateRestore
    {
        private const string KeyGold = "gold";
        private const string KeyFloor = "floor";
        private const string KeyRemovals = "removals";
        private const string KeyElapsed = "elapsed";
        private const string KeyRng = "rng";
        private const string KeyStart = "start";
        private const string KeyCleared = "cleared";

        #region 拍平整局：标量、三份配置、地图平行表与玩家清单

        public static void Capture(RunState run_, ISaveWriter writer_)
        {
            writer_.Write(KeyGold, run_.Gold);
            writer_.Write(KeyFloor, run_.Floor);
            writer_.Write(KeyRemovals, run_.CardRemovalsUsed);
            writer_.Write(KeyElapsed, run_.ElapsedSeconds);
            writer_.Write(KeyRng, run_.Rng.State);
            writer_.Write("playerConfig", GuidOf(run_.Config));
            writer_.Write("mapConfig", GuidOf(run_.MapConfig));
            writer_.Write("fallbackPool", GuidOf(run_.FallbackRelicPool));
            CaptureMap(run_.Map, writer_);
            CapturePlayer(run_.Player, writer_);
            CapturePendingRewards(run_.PendingRewards, writer_);
        }

        #endregion

        #region 标量回写：读档构造分支与统一 Restore 共用，幂等

        public static void ApplyScalars(RunState run_, ISaveReader reader_)
        {
            run_.RestoreScalars(reader_.Read(KeyGold, 0), reader_.Read(KeyFloor, 1),
                reader_.Read(KeyRemovals, 0), reader_.Read(KeyElapsed, 0d),
                reader_.Read<uint>(KeyRng, 0u));
            run_.SetPendingRewards(BuildPendingRewards(reader_, SaveService.Catalog));
        }

        #endregion

        #region 重建整局：先建地图与玩家，再经读档构造器接住，最后补标量

        public static RunState Build(ISaveReader reader_, SO_Player playerConfig_, SO_MapGenerationConfig mapConfig_,
            SO_RelicPool fallbackPool_)
        {
            SO_SaveCatalog catalog = SaveService.Catalog;
            SO_Player config = Resolve(reader_.Read<string>("playerConfig", null), catalog, playerConfig_);
            SO_MapGenerationConfig mapConfig = Resolve(reader_.Read<string>("mapConfig", null), catalog, mapConfig_);
            SO_RelicPool fallback = Resolve(reader_.Read<string>("fallbackPool", null), catalog, fallbackPool_);

            MapGraph map = BuildMap(reader_);
            Player player = BuildPlayer(reader_, catalog);
            Rng rng = new(1);
            RunState run = new(config, mapConfig, fallback, map, player, rng);
            ApplyScalars(run, reader_);
            return run;
        }

        #endregion

        #region 地图拍平：节点平行表 + 子边扁平表 + 锚点与已通过索引

        private static void CaptureMap(MapGraph graph_, ISaveWriter writer_)
        {
            List<MapNode> nodes = new();
            Dictionary<MapNode, int> index = new();
            for (int col = 0; col < graph_.Width; col++)
            for (int row = 0; row < graph_.Rows; row++)
                AddNode(graph_.GetNode(col, row));
            AddNode(graph_.StartNode);
            AddNode(graph_.BossNode);

            int[] cols = new int[nodes.Count];
            int[] rows = new int[nodes.Count];
            int[] types = new int[nodes.Count];
            int[] revealed = new int[nodes.Count];
            bool[] modifiable = new bool[nodes.Count];
            List<int> edges = new();
            for (int i = 0; i < nodes.Count; i++)
            {
                MapNode node = nodes[i];
                cols[i] = node.Coord.Col;
                rows[i] = node.Coord.Row;
                types[i] = (int)node.PointType;
                revealed[i] = (int)node.RevealedType;
                modifiable[i] = node.CanBeModified;
                foreach (MapNode child in node.Children)
                {
                    edges.Add(i);
                    edges.Add(index[child]);
                }
            }

            List<int> cleared = new();
            foreach (MapNode node in graph_.ClearedNodes)
                if (index.TryGetValue(node, out int at)) cleared.Add(at);

            writer_.Write("map.width", graph_.Width);
            writer_.Write("map.rows", graph_.Rows);
            writer_.Write("map.cols", cols);
            writer_.Write("map.rowsData", rows);
            writer_.Write("map.types", types);
            writer_.Write("map.revealed", revealed);
            writer_.Write("map.modifiable", modifiable);
            writer_.Write("map.edges", edges.ToArray());
            writer_.Write(KeyStart, IndexOf(index, graph_.StartNode));
            writer_.Write("map.boss", IndexOf(index, graph_.BossNode));
            writer_.Write("map.current", IndexOf(index, graph_.CurrentNode));
            writer_.Write(KeyCleared, cleared.ToArray());
            writer_.Write("map.pending", graph_.SegmentPending);

            void AddNode(MapNode node_)
            {
                if (node_ == null || index.ContainsKey(node_)) return;
                index.Add(node_, nodes.Count);
                nodes.Add(node_);
            }
        }

        #endregion

        #region 地图重建：按坐标造节点、补双向边，再交给 MapGraph.Restore 重算可达性

        private static MapGraph BuildMap(ISaveReader reader_)
        {
            int width = reader_.Read("map.width", 0);
            int rows = reader_.Read("map.rows", 0);
            int[] cols = reader_.Read<int[]>("map.cols", Array.Empty<int>());
            int[] nodeRows = reader_.Read<int[]>("map.rowsData", Array.Empty<int>());
            int[] types = reader_.Read<int[]>("map.types", Array.Empty<int>());
            int[] revealed = reader_.Read<int[]>("map.revealed", Array.Empty<int>());
            bool[] modifiable = reader_.Read<bool[]>("map.modifiable", Array.Empty<bool>());
            int[] edges = reader_.Read<int[]>("map.edges", Array.Empty<int>());
            int startIndex = reader_.Read(KeyStart, -1);
            int bossIndex = reader_.Read("map.boss", -1);
            int currentIndex = reader_.Read("map.current", -1);
            int[] clearedIndexes = reader_.Read<int[]>(KeyCleared, Array.Empty<int>());
            bool pending = reader_.Read("map.pending", false);
            if (width <= 0 || rows <= 0 || cols.Length == 0 || startIndex < 0 || bossIndex < 0)
                throw new InvalidOperationException("[RunStateRestore] 地图存档数据不完整，无法重建");

            MapNode[] nodes = new MapNode[cols.Length];
            MapNode[,] grid = new MapNode[width, rows];
            for (int i = 0; i < cols.Length; i++)
            {
                MapNode node = new(new MapCoord((byte)cols[i], (byte)nodeRows[i]), (EMapPointType)types[i]);
                node.CanBeModified = i < modifiable.Length && modifiable[i];
                node.RevealedType = (EMapPointType)revealed[i];
                nodes[i] = node;
                if (nodeRows[i] < rows && cols[i] < width) grid[cols[i], nodeRows[i]] = node;
            }

            for (int e = 0; e + 1 < edges.Length; e += 2)
                if (edges[e] >= 0 && edges[e] < nodes.Length && edges[e + 1] >= 0 && edges[e + 1] < nodes.Length)
                    nodes[edges[e]].AddChildPoint(nodes[edges[e + 1]]);

            List<MapNode> cleared = new();
            foreach (int at in clearedIndexes)
                if (at >= 0 && at < nodes.Length) cleared.Add(nodes[at]);
            MapNode current = currentIndex >= 0 && currentIndex < nodes.Length ? nodes[currentIndex] : nodes[startIndex];
            return MapGraph.Restore(width, rows, grid, nodes[startIndex], nodes[bossIndex], cleared, current, pending);
        }

        #endregion

        #region 玩家拍平：生命、遗物 GUID、药水槽 GUID、卡组 GUID + 升级标记、跨节点计数

        private static void CapturePlayer(Player player_, ISaveWriter writer_)
        {
            writer_.Write("player.name", player_.Creature.ActorName);
            writer_.Write("player.hp", player_.Creature.CurrentHp);
            writer_.Write("player.maxHp", player_.Creature.MaxHp);
            writer_.Write("player.relics", GuidsOf(player_.Relics));
            writer_.Write("player.potions", GuidsOf(player_.PotionSlots));

            List<string> deck = new(player_.Deck.Count);
            List<bool> upgraded = new(player_.Deck.Count);
            foreach (CardModel card in player_.Deck.Cards)
            {
                deck.Add(GuidOf(card.Source));
                upgraded.Add(card.IsUpgraded);
            }

            writer_.Write("player.deck", deck.ToArray());
            writer_.Write("player.deckUpgraded", upgraded.ToArray());
            List<string> keys = new();
            List<int> values = new();
            foreach (KeyValuePair<SO_Relic, int> pair in player_.RelicCounters)
            {
                string guid = GuidOf(pair.Key);
                if (guid == null) continue;
                keys.Add(guid);
                values.Add(pair.Value);
            }

            writer_.Write("player.counterKeys", keys.ToArray());
            writer_.Write("player.counterValues", values.ToArray());
        }

        #endregion

        #region 玩家重建：空槽起步后按存档逐项灌入，写回上限与当前生命不播回血演出

        private static Player BuildPlayer(ISaveReader reader_, SO_SaveCatalog catalog_)
        {
            string name = reader_.Read("player.name", string.Empty);
            int maxHp = reader_.Read("player.maxHp", 1);
            Player player = new(name, maxHp, Array.Empty<SO_Relic>(), Array.Empty<SO_Potion>());
            player.Creature.RestoreVitals(reader_.Read("player.hp", maxHp), maxHp);

            foreach (string guid in reader_.Read<string[]>("player.relics", Array.Empty<string>()))
            {
                SO_Relic relic = catalog_?.Find<SO_Relic>(guid);
                if (relic != null) player.AddRelic(relic);
            }

            string[] potions = reader_.Read<string[]>("player.potions", Array.Empty<string>());
            for (int i = 0; i < potions.Length; i++)
                player.RestorePotionSlot(i, catalog_?.Find<SO_Potion>(potions[i]));

            string[] deck = reader_.Read<string[]>("player.deck", Array.Empty<string>());
            bool[] upgraded = reader_.Read<bool[]>("player.deckUpgraded", Array.Empty<bool>());
            for (int i = 0; i < deck.Length; i++)
            {
                SO_Card source = catalog_?.Find<SO_Card>(deck[i]);
                if (source == null) continue;
                CardModel card = CardModelBaker.Bake(source).ToMutable();
                if (i < upgraded.Length && upgraded[i]) card.Upgrade();
                player.AddCard(card);
            }

            string[] counterKeys = reader_.Read<string[]>("player.counterKeys", Array.Empty<string>());
            int[] counterValues = reader_.Read<int[]>("player.counterValues", Array.Empty<int>());
            for (int i = 0; i < counterKeys.Length && i < counterValues.Length; i++)
            {
                SO_Relic relic = catalog_?.Find<SO_Relic>(counterKeys[i]);
                if (relic != null) player.RestoreRelicCounter(relic, counterValues[i]);
            }

            return player;
        }

        #endregion

        #region 待领奖励拍平：按条目平行表 + 卡牌组扁平表，卡牌只存配置 GUID 与升级标记

        private static void CapturePendingRewards(IReadOnlyList<RunReward> rewards_, ISaveWriter writer_)
        {
            int count = rewards_?.Count ?? 0;
            int[] kinds = new int[count];
            int[] gold = new int[count];
            string[] potions = new string[count];
            string[] relics = new string[count];
            List<int> cardGroups = new();
            List<string> cardSources = new();
            List<bool> cardUpgraded = new();
            for (int i = 0; i < count; i++)
            {
                RunReward reward = rewards_[i];
                kinds[i] = (int)reward.Kind;
                gold[i] = reward.Gold;
                potions[i] = GuidOf(reward.Potion);
                relics[i] = GuidOf(reward.Relic);
                IReadOnlyList<CardModel> cards = reward.Cards;
                if (cards == null) continue;
                for (int c = 0; c < cards.Count; c++)
                {
                    cardGroups.Add(i);
                    cardSources.Add(GuidOf(cards[c].Source));
                    cardUpgraded.Add(cards[c].IsUpgraded);
                }
            }

            writer_.Write("reward.kinds", kinds);
            writer_.Write("reward.gold", gold);
            writer_.Write("reward.potions", potions);
            writer_.Write("reward.relics", relics);
            writer_.Write("reward.cardGroup", cardGroups.ToArray());
            writer_.Write("reward.cardSource", cardSources.ToArray());
            writer_.Write("reward.cardUpgraded", cardUpgraded.ToArray());
        }

        #endregion

        #region 待领奖励重建：解析失败的条目跳过，全空视为不在领奖阶段

        private static IReadOnlyList<RunReward> BuildPendingRewards(ISaveReader reader_, SO_SaveCatalog catalog_)
        {
            int[] kinds = reader_.Read<int[]>("reward.kinds", Array.Empty<int>());
            if (kinds.Length == 0) return null;
            int[] gold = reader_.Read<int[]>("reward.gold", Array.Empty<int>());
            string[] potions = reader_.Read<string[]>("reward.potions", Array.Empty<string>());
            string[] relics = reader_.Read<string[]>("reward.relics", Array.Empty<string>());
            int[] cardGroups = reader_.Read<int[]>("reward.cardGroup", Array.Empty<int>());
            string[] cardSources = reader_.Read<string[]>("reward.cardSource", Array.Empty<string>());
            bool[] cardUpgraded = reader_.Read<bool[]>("reward.cardUpgraded", Array.Empty<bool>());

            List<RunReward> rewards = new(kinds.Length);
            for (int i = 0; i < kinds.Length; i++)
            {
                switch ((ERewardKind)kinds[i])
                {
                    case ERewardKind.Gold:
                        int amount = i < gold.Length ? gold[i] : 0;
                        if (amount > 0) rewards.Add(RunReward.GoldOf(amount));
                        break;
                    case ERewardKind.Card:
                        List<CardModel> cards = new();
                        for (int c = 0; c < cardGroups.Length; c++)
                        {
                            if (cardGroups[c] != i) continue;
                            SO_Card source = c < cardSources.Length ? catalog_?.Find<SO_Card>(cardSources[c]) : null;
                            if (source == null) continue;
                            CardModel card = CardModelBaker.Bake(source).ToMutable();
                            if (c < cardUpgraded.Length && cardUpgraded[c]) card.Upgrade();
                            cards.Add(card);
                        }

                        if (cards.Count > 0) rewards.Add(RunReward.CardsOf(cards));
                        break;
                    case ERewardKind.Potion:
                        SO_Potion potion = i < potions.Length ? catalog_?.Find<SO_Potion>(potions[i]) : null;
                        if (potion != null) rewards.Add(RunReward.PotionOf(potion));
                        break;
                    case ERewardKind.Relic:
                        SO_Relic relic = i < relics.Length ? catalog_?.Find<SO_Relic>(relics[i]) : null;
                        if (relic != null) rewards.Add(RunReward.RelicOf(relic));
                        break;
                }
            }

            return rewards.Count == 0 ? null : rewards;
        }

        #endregion

        private static string GuidOf(UnityEngine.ScriptableObject asset_) =>
            asset_ == null ? null : SaveService.Catalog?.GuidOf(asset_);

        private static string[] GuidsOf<T>(IReadOnlyList<T> assets_) where T : UnityEngine.ScriptableObject
        {
            string[] guids = new string[assets_.Count];
            for (int i = 0; i < assets_.Count; i++) guids[i] = GuidOf(assets_[i]);
            return guids;
        }

        private static T Resolve<T>(string guid_, SO_SaveCatalog catalog_, T fallback_) where T : UnityEngine.ScriptableObject
            => catalog_?.Find<T>(guid_) ?? fallback_;

        private static int IndexOf(Dictionary<MapNode, int> index_, MapNode node_) =>
            node_ != null && index_.TryGetValue(node_, out int at) ? at : -1;
    }
}
