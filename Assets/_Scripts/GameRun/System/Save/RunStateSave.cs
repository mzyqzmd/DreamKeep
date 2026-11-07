using System;
using System.Collections.Generic;
using _DreamKeep.ConfigData;
using _DreamKeep.RuntimeData;
using _DreamKeep.Utility;

namespace _DreamKeep.GameRun
{
    /// <summary>整局状态与磁盘 DTO 的显式转换器：写档把运行时对象拍平，读档按 catalog 的 GUID 反查配置重建，
    /// 不直接序列化带事件、Sprite 与双向边引用的运行时模型；配置缺项经 onWarning_ 上报，域层不碰引擎日志。</summary>
    public static class RunStateSave
    {
        #region 拍平整局状态：配置引用一律经 catalog 换成 GUID

        public static SaveRun ToSave(RunState run_, SO_SaveCatalog catalog_, Action<string> onWarning_ = null)
        {
            return new SaveRun
            {
                PlayerConfigGuid = catalog_.GuidOf(run_.Config),
                MapConfigGuid = catalog_.GuidOf(run_.MapConfig),
                FallbackRelicPoolGuid = catalog_.GuidOf(run_.FallbackRelicPool),
                RngState = run_.Rng.State,
                Gold = run_.Gold,
                Floor = run_.Floor,
                CardRemovalsUsed = run_.CardRemovalsUsed,
                ElapsedSeconds = run_.ElapsedSeconds,
                Player = ToPlayerSave(run_.Player, catalog_, onWarning_),
                Map = ToMapSave(run_.Map),
            };
        }

        #endregion

        #region 从磁盘快照重建整局状态：核心配置缺失返回 null，由调用方按无档处理

        public static RunState FromSave(SaveRun dto_, SO_SaveCatalog catalog_, Action<string> onWarning_ = null)
        {
            SO_Player config = catalog_.Find<SO_Player>(dto_.PlayerConfigGuid);
            SO_MapGenerationConfig mapConfig = catalog_.Find<SO_MapGenerationConfig>(dto_.MapConfigGuid);
            SO_RelicPool fallbackPool = catalog_.Find<SO_RelicPool>(dto_.FallbackRelicPoolGuid);
            if (config == null || mapConfig == null)
            {
                onWarning_?.Invoke("[RunStateSave] 角色或地图配置 GUID 在目录里找不到，按无档处理");
                return null;
            }

            Player player = RestorePlayer(dto_.Player, config, catalog_, onWarning_);
            MapGraph map = RestoreMap(dto_.Map);
            Rng rng = new Rng(0);
            rng.State = dto_.RngState;
            return new RunState(config, mapConfig, fallbackPool, map, player, rng, dto_);
        }

        #endregion

        #region 冻结待领奖励：写档存已掷结果，读档不重掷、不重复消耗随机流

        public static List<SaveReward> ToRewardSave(IReadOnlyList<RunReward> rewards_, SO_SaveCatalog catalog_,
            Action<string> onWarning_ = null)
        {
            List<SaveReward> result = new(rewards_.Count);
            for (int i = 0; i < rewards_.Count; i++)
            {
                RunReward reward = rewards_[i];
                SaveReward dto = new() { Kind = reward.Kind, Gold = reward.Gold };
                if (reward.Cards != null)
                    for (int j = 0; j < reward.Cards.Count; j++)
                        dto.Cards.Add(ToCardSave(reward.Cards[j], catalog_, onWarning_));
                dto.PotionGuid = catalog_.GuidOf(reward.Potion);
                dto.RelicGuid = catalog_.GuidOf(reward.Relic);
                result.Add(dto);
            }

            return result;
        }

        public static List<RunReward> FromRewardSave(List<SaveReward> dtos_, SO_SaveCatalog catalog_,
            Action<string> onWarning_ = null)
        {
            List<RunReward> rewards = new();
            if (dtos_ == null) return rewards;
            foreach (SaveReward dto in dtos_)
            {
                switch (dto.Kind)
                {
                    case ERewardKind.Gold:
                        rewards.Add(RunReward.GoldOf(dto.Gold));
                        break;
                    case ERewardKind.Card:
                        List<CardModel> cards = new();
                        if (dto.Cards != null)
                            foreach (SaveCard card in dto.Cards)
                                if (FromCardSave(card, catalog_, onWarning_) is CardModel model) cards.Add(model);
                        if (cards.Count > 0) rewards.Add(RunReward.CardsOf(cards));
                        break;
                    case ERewardKind.Potion:
                        if (catalog_.Find<SO_Potion>(dto.PotionGuid) is SO_Potion potion)
                            rewards.Add(RunReward.PotionOf(potion));
                        else onWarning_?.Invoke("[RunStateSave] 待领药水 GUID 缺失，已跳过该条奖励");
                        break;
                    case ERewardKind.Relic:
                        if (catalog_.Find<SO_Relic>(dto.RelicGuid) is SO_Relic relic)
                            rewards.Add(RunReward.RelicOf(relic));
                        else onWarning_?.Invoke("[RunStateSave] 待领遗物 GUID 缺失，已跳过该条奖励");
                        break;
                }
            }

            return rewards;
        }

        #endregion

        private static SavePlayer ToPlayerSave(Player player_, SO_SaveCatalog catalog_, Action<string> onWarning_)
        {
            SavePlayer dto = new()
            {
                CurrentHp = player_.Creature.CurrentHp,
                MaxHp = player_.Creature.MaxHp,
            };
            foreach (SO_Relic relic in player_.Relics) dto.RelicGuids.Add(catalog_.GuidOf(relic));
            foreach (SO_Potion potion in player_.PotionSlots) dto.PotionSlotGuids.Add(catalog_.GuidOf(potion));
            foreach (CardModel card in player_.Deck.Cards) dto.Deck.Add(ToCardSave(card, catalog_, onWarning_));
            foreach (KeyValuePair<SO_Relic, int> counter in player_.RelicCounters)
            {
                string guid = catalog_.GuidOf(counter.Key);
                if (guid != null) dto.RelicCounters[guid] = counter.Value;
            }

            return dto;
        }

        private static Player RestorePlayer(SavePlayer dto_, SO_Player config_, SO_SaveCatalog catalog_,
            Action<string> onWarning_)
        {
            Player player = new(config_.Name, config_.MaxHp, Array.Empty<SO_Relic>(), Array.Empty<SO_Potion>());
            player.Creature.RestoreVitals(dto_.CurrentHp, dto_.MaxHp);
            if (dto_.RelicGuids != null)
                foreach (string guid in dto_.RelicGuids)
                {
                    if (catalog_.Find<SO_Relic>(guid) is SO_Relic relic) player.AddRelic(relic);
                    else onWarning_?.Invoke("[RunStateSave] 持有遗物 GUID 缺失，已跳过一件遗物");
                }

            if (dto_.PotionSlotGuids != null)
                for (int i = 0; i < dto_.PotionSlotGuids.Count && i < Player.PotionSlotCount; i++)
                    player.RestorePotionSlot(i, catalog_.Find<SO_Potion>(dto_.PotionSlotGuids[i]));

            if (dto_.Deck != null)
                foreach (SaveCard card in dto_.Deck)
                    if (FromCardSave(card, catalog_, onWarning_) is CardModel model) player.AddCard(model);

            if (dto_.RelicCounters != null)
                foreach (KeyValuePair<string, int> counter in dto_.RelicCounters)
                    if (catalog_.Find<SO_Relic>(counter.Key) is SO_Relic relic)
                        player.RestoreRelicCounter(relic, counter.Value);

            return player;
        }

        private static SaveCard ToCardSave(CardModel card_, SO_SaveCatalog catalog_, Action<string> onWarning_)
        {
            string guid = catalog_.GuidOf(card_.Source);
            if (string.IsNullOrEmpty(guid))
                onWarning_?.Invoke($"[RunStateSave] 卡 {card_.Name} 的配置不在目录里，读档会跳过这张卡");
            return new SaveCard { CardGuid = guid, IsUpgraded = card_.IsUpgraded };
        }

        private static CardModel FromCardSave(SaveCard dto_, SO_SaveCatalog catalog_, Action<string> onWarning_)
        {
            if (catalog_.Find<SO_Card>(dto_.CardGuid) is not SO_Card config)
            {
                onWarning_?.Invoke("[RunStateSave] 卡牌 GUID 缺失，已跳过一张卡");
                return null;
            }

            CardModel card = CardModelBaker.Bake(config).ToMutable();
            if (dto_.IsUpgraded) card.Upgrade();
            return card;
        }

        // 节点扁平成表：先按网格顺序收，再补网格外的 Boss；下标即 DTO 里一切引用的依据
        private static SaveMap ToMapSave(MapGraph map_)
        {
            List<MapNode> nodes = new();
            Dictionary<MapNode, int> indexOf = new();
            for (int col = 0; col < map_.Width; col++)
            for (int row = 0; row < map_.Rows; row++)
                if (map_.GetNode(col, row) is MapNode node) Collect(node);
            if (map_.StartNode != null && !indexOf.ContainsKey(map_.StartNode)) Collect(map_.StartNode);
            if (map_.BossNode != null && !indexOf.ContainsKey(map_.BossNode)) Collect(map_.BossNode);

            SaveMap dto = new()
            {
                Width = map_.Width,
                Rows = map_.Rows,
                StartIndex = IndexOf(map_.StartNode),
                BossIndex = IndexOf(map_.BossNode),
                CurrentIndex = IndexOf(map_.CurrentNode),
                SegmentPending = map_.SegmentPending,
            };
            foreach (MapNode node in map_.ClearedNodes) dto.ClearedIndices.Add(IndexOf(node));
            for (int i = 0; i < nodes.Count; i++)
            {
                MapNode node = nodes[i];
                SaveMapNode nodeDto = new()
                {
                    Col = node.Coord.Col,
                    Row = node.Coord.Row,
                    PointType = node.PointType,
                    RevealedType = node.RevealedType,
                    CanBeModified = node.CanBeModified,
                };
                foreach (MapNode child in node.Children)
                    if (indexOf.TryGetValue(child, out int childIndex)) nodeDto.Children.Add(childIndex);
                dto.Nodes.Add(nodeDto);
            }

            return dto;

            void Collect(MapNode node_)
            {
                indexOf[node_] = nodes.Count;
                nodes.Add(node_);
            }

            int IndexOf(MapNode node_) => node_ != null && indexOf.TryGetValue(node_, out int index) ? index : -1;
        }

        private static MapGraph RestoreMap(SaveMap dto_)
        {
            List<MapNode> nodes = new(dto_.Nodes.Count);
            foreach (SaveMapNode nodeDto in dto_.Nodes)
                nodes.Add(new MapNode(new MapCoord((byte)nodeDto.Col, (byte)nodeDto.Row), nodeDto.PointType)
                {
                    CanBeModified = nodeDto.CanBeModified,
                    RevealedType = nodeDto.RevealedType,
                });

            for (int i = 0; i < dto_.Nodes.Count; i++)
            {
                if (dto_.Nodes[i].Children == null) continue;
                foreach (int childIndex in dto_.Nodes[i].Children)
                    if (childIndex >= 0 && childIndex < nodes.Count) nodes[i].AddChildPoint(nodes[childIndex]);
            }

            MapNode[,] grid = new MapNode[dto_.Width, dto_.Rows];
            for (int i = 0; i < nodes.Count; i++)
            {
                MapCoord coord = nodes[i].Coord;
                if (coord.Col < dto_.Width && coord.Row < dto_.Rows) grid[coord.Col, coord.Row] = nodes[i];
            }

            HashSet<MapNode> cleared = new();
            if (dto_.ClearedIndices != null)
                foreach (int index in dto_.ClearedIndices)
                    if (index >= 0 && index < nodes.Count) cleared.Add(nodes[index]);

            return MapGraph.Restore(dto_.Width, dto_.Rows, grid, At(dto_.StartIndex), At(dto_.BossIndex),
                cleared, At(dto_.CurrentIndex), dto_.SegmentPending);

            MapNode At(int index_) => index_ >= 0 && index_ < nodes.Count ? nodes[index_] : null;
        }
    }
}
