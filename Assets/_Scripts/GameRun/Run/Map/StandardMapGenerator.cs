using System;
using System.Collections.Generic;
using System.Text;
using _DreamKeep.ConfigData;
using _DreamKeep.Utility;

namespace _DreamKeep.GameRun
{
    /// <summary>标准章地图生成器（文档 §4 全套）：拓扑 → 类型分配 → 剪枝 → 布局后处理，全程吃同一随机流保证同种子可复现。
    /// 网格 row 0 为起点行，row 1..RoomRows 为房间行，Boss 在网格外虚拟行；节点由起点收口、最后一行连 Boss，构成 DAG。</summary>
    public static class StandardMapGenerator
    {
        private const int PruneMaxIterations = 50; // 剪枝迭代上限：超限视为收敛失败，保留现状（防御性）

        /// <summary>生成一张整章地图；固定语义行（首行怪 / Boss 前休整 / 倒数第 7 行宝藏）与配额合法性都照文档执行。</summary>
        public static MapGraph Generate(SO_MapGenerationConfig config_, Rng rng_)
        {
            if (!config_) throw new ArgumentNullException(nameof(config_));
            if (rng_ == null) throw new ArgumentNullException(nameof(rng_));

            int width = config_.MapWidth;
            int rows = config_.RoomRows + 1; // 网格行数：row 0 起点行 + 房间行

            MapNode[,] grid = new MapNode[width, rows];
            MapNode start = new MapNode(new MapCoord((byte)(width / 2), 0), EMapPointType.Ancient);
            MapNode boss = new MapNode(new MapCoord((byte)(width / 2), (byte)rows), EMapPointType.Boss);
            start.CanBeModified = false;
            boss.CanBeModified = false;
            grid[start.Coord.Col, 0] = start; // 起点入网格：UI 按网格遍历取节点，漏写会画不出起点

            GenerateTopology(grid, start, boss, width, rows, config_.PathCount, rng_);
            AssignPointTypes(grid, width, rows, config_, rng_);

            int iterations = 0;
            while (iterations++ < PruneMaxIterations && PruneOnce(grid, rng_)) { }
            RemoveUnreachable(grid, start, width, rows);
            PostProcessLayout(grid, boss, width, rows);

            return new MapGraph(width, rows, grid, start, boss);
        }

        #region 拓扑生成：多条起点路径单步推进，落点复用汇成 DAG，最后一行收口连 Boss

        private static void GenerateTopology(MapNode[,] grid_, MapNode start_, MapNode boss_, int width_, int rows_, int pathCount_, Rng rng_)
        {
            int firstCol = rng_.Next(width_);
            for (int path = 0; path < pathCount_; path++)
            {
                int col;
                if (path == 0) col = firstCol;
                else if (path == 1)
                {
                    col = rng_.Next(width_ - 1);
                    if (col >= firstCol) col++; // 错位映射跳过 firstCol，保证第二条路径起点不同
                }
                else col = rng_.Next(width_);
                MapNode current = start_;
                for (int row = 1; row < rows_; row++)
                {
                    MapNode node = grid_[col, row] ??= new MapNode(new MapCoord((byte)col, (byte)row));
                    current.AddChildPoint(node);
                    current = node;
                    if (row < rows_ - 1) col = NextColumn(grid_, node, width_, rng_);
                }
            }

            for (int col = 0; col < width_; col++)
                if (grid_[col, rows_ - 1] is MapNode last)
                    last.AddChildPoint(boss_);
        }

        // 单步前进：向上一行，在 {−1,0,+1} 三列候选中洗牌依次尝试；越界与交叉跳过，全失败直行兜底
        private static int NextColumn(MapNode[,] grid_, MapNode node_, int width_, Rng rng_)
        {
            int col = node_.Coord.Col;
            List<int> candidates = new List<int>(3) { col - 1, col, col + 1 };
            rng_.Shuffle(candidates);
            foreach (int candidate in candidates)
            {
                if (candidate < 0 || candidate >= width_) continue;
                if (CrossesExistingEdge(grid_, node_, candidate, width_)) continue;
                return candidate;
            }
            return col;
        }

        // 交叉判定：新边与同一行间任何已存在的边比较列序，方向相反就成 X（(1,r)→(2,r+1) 与 (2,r)→(1,r+1)）。
        // 只看"目标点的子节点"会漏掉这种同层交叉，必须遍历整行的边
        private static bool CrossesExistingEdge(MapNode[,] grid_, MapNode from_, int targetCol_, int width_)
        {
            int row = from_.Coord.Row;
            for (int col = 0; col < width_; col++)
            {
                MapNode parent = grid_[col, row];
                if (parent == null) continue;
                foreach (MapNode child in parent.Children)
                {
                    if (child.Coord.Row != row + 1) continue;
                    int parentSide = parent.Coord.Col - from_.Coord.Col;
                    int childSide = child.Coord.Col - targetCol_;
                    if (parentSide != 0 && childSide != 0 && parentSide * childSide < 0) return true;
                }
            }
            return false;
        }

        #endregion

        #region 类型分配：固定语义行 + 配额随机落点（合法性校验）+ Monster 兜底

        private static void AssignPointTypes(MapNode[,] grid_, int width_, int rows_, SO_MapGenerationConfig config_, Rng rng_)
        {
            FillRow(grid_, width_, 1, EMapPointType.Monster);
            FillRow(grid_, width_, rows_ - 1, EMapPointType.Rest);
            if (rows_ - 7 > 1) FillRow(grid_, width_, rows_ - 7, EMapPointType.Treasure);

            List<EMapPointType> remaining = BuildQuotaList(RollCounts(config_, rng_));
            List<MapNode> open = CollectOpenNodes(grid_, width_, rows_);
            rng_.Shuffle(open);
            foreach (MapNode node in open)
            {
                if (remaining.Count == 0) break;
                rng_.Shuffle(remaining);
                for (int i = remaining.Count - 1; i >= 0; i--)
                {
                    EMapPointType type = remaining[i];
                    if (!IsValidPointType(node, type, config_, rows_)) continue;
                    node.PointType = type;
                    remaining.RemoveAt(i);
                    break;
                }
            }

            for (int col = 0; col < width_; col++)
                for (int row = 0; row < rows_; row++)
                    if (grid_[col, row] is MapNode node && node.PointType == EMapPointType.None)
                        node.PointType = EMapPointType.Monster;
        }

        private static void FillRow(MapNode[,] grid_, int width_, int row_, EMapPointType type_)
        {
            for (int col = 0; col < width_; col++)
                if (grid_[col, row_] is MapNode node)
                {
                    node.PointType = type_;
                    node.CanBeModified = false;
                }
        }

        private static MapPointTypeCounts RollCounts(SO_MapGenerationConfig config_, Rng rng_) => new MapPointTypeCounts(
            config_.NumOfElites,
            config_.NumOfShops,
            GaussianInt(rng_, config_.UnknownMean, config_.UnknownStd, config_.UnknownMin, config_.UnknownMax),
            GaussianInt(rng_, config_.RestMean, config_.RestStd, config_.RestMin, config_.RestMax));

        private static int GaussianInt(Rng rng_, int mean_, int std_, int min_, int max_)
        {
            double u1 = 1.0 - rng_.Next(0f, 1f);
            double u2 = rng_.Next(0f, 1f);
            double gaussian = Math.Sqrt(-2.0 * Math.Log(u1)) * Math.Sin(2.0 * Math.PI * u2);
            return Math.Clamp(mean_ + (int)Math.Round(gaussian * std_), min_, max_);
        }

        private static List<EMapPointType> BuildQuotaList(MapPointTypeCounts counts_)
        {
            List<EMapPointType> list = new List<EMapPointType>(
                counts_.NumOfElites + counts_.NumOfShops + counts_.NumOfUnknowns + counts_.NumOfRests);
            for (int i = 0; i < counts_.NumOfElites; i++) list.Add(EMapPointType.Elite);
            for (int i = 0; i < counts_.NumOfShops; i++) list.Add(EMapPointType.Shop);
            for (int i = 0; i < counts_.NumOfUnknowns; i++) list.Add(EMapPointType.Event);
            for (int i = 0; i < counts_.NumOfRests; i++) list.Add(EMapPointType.Rest);
            return list;
        }

        private static List<MapNode> CollectOpenNodes(MapNode[,] grid_, int width_, int rows_)
        {
            List<MapNode> open = new List<MapNode>();
            for (int col = 0; col < width_; col++)
                for (int row = 0; row < rows_; row++)
                    if (grid_[col, row] is MapNode node && node.PointType == EMapPointType.None && node.CanBeModified)
                        open.Add(node);
            return open;
        }

        // 五类限制：下层禁休息/精英、上层禁休息、父子同类型禁（稀有设施）、兄弟同类型禁（节奏多样）
        // 行合法性：下层限制禁休息/精英，上层限制禁休息——生成期落点与运行期问号揭示共用同一口径
        public static bool IsRowLegal(EMapPointType type_, int row_, int rows_, SO_MapGenerationConfig config_)
        {
            if (row_ < config_.LowerRowLimit && (type_ == EMapPointType.Rest || type_ == EMapPointType.Elite)) return false;
            if (row_ >= rows_ - config_.UpperRestRowLimit && type_ == EMapPointType.Rest) return false;
            return true;
        }

        private static bool IsValidPointType(MapNode node_, EMapPointType type_, SO_MapGenerationConfig config_, int rows_)
        {
            int row = node_.Coord.Row;
            if (!IsRowLegal(type_, row, rows_, config_)) return false;

            if (type_ is EMapPointType.Elite or EMapPointType.Rest or EMapPointType.Treasure or EMapPointType.Shop)
            {
                foreach (MapNode parent in node_.Parents) if (parent.PointType == type_) return false;
                foreach (MapNode child in node_.Children) if (child.PointType == type_) return false;
            }

            if (type_ is EMapPointType.Rest or EMapPointType.Monster or EMapPointType.Event or EMapPointType.Elite or EMapPointType.Shop)
                foreach (MapNode parent in node_.Parents)
                    foreach (MapNode sibling in parent.Children)
                        if (sibling != node_ && sibling.PointType == type_) return false;

            return true;
        }

        #endregion

        #region 剪枝：消除"分叉到汇合、中间坐标与类型序列完全一致"的冗余平行段

        // 对每个分叉头（parents≥2），沿各自孩子的"1父1子"链走到尾，
        // 链的坐标+类型序列与尾坐标完全一致的视为重复平行段：按遍历序保第一条，断掉其余首边（孤岛交由可达性清理收走）
        private static bool PruneOnce(MapNode[,] grid_, Rng rng_)
        {
            List<MapNode> heads = new List<MapNode>();
            for (int col = 0; col < grid_.GetLength(0); col++)
                for (int row = 0; row < grid_.GetLength(1); row++)
                    if (grid_[col, row] is MapNode node && node.Parents.Count >= 2)
                        heads.Add(node);

            bool pruned = false;
            foreach (MapNode head in heads)
            {
                List<(string id, MapNode first)> chains = new List<(string, MapNode)>();
                foreach (MapNode first in head.Children.ToArray())
                {
                    StringBuilder key = new StringBuilder();
                    MapNode cursor = first;
                    while (true)
                    {
                        key.Append(cursor.PointType).Append('|'); // 只比类型序列：平行段坐标本就不同，带坐标永远不重复
                        if (cursor.Parents.Count != 1 || cursor.Children.Count != 1) break;
                        cursor = cursor.Children[0];
                    }
                    chains.Add(($"{cursor.Coord}|{key}", first));
                }

                Dictionary<string, List<MapNode>> groups = new Dictionary<string, List<MapNode>>();
                foreach ((string id, MapNode first) in chains)
                {
                    if (!groups.TryGetValue(id, out List<MapNode> list)) groups[id] = list = new List<MapNode>();
                    list.Add(first);
                }
                foreach (List<MapNode> duplicates in groups.Values)
                {
                    if (duplicates.Count < 2) continue;
                    pruned = true;
                    int keep = rng_.Next(duplicates.Count);
                    for (int i = 0; i < duplicates.Count; i++)
                        if (i != keep) head.RemoveChildPoint(duplicates[i]);
                }
            }
            return pruned;
        }

        #endregion

        #region 可达性清理：剪枝断边可能留下孤岛，从起点沿边不可达的节点一并移出网格并断开边

        private static void RemoveUnreachable(MapNode[,] grid_, MapNode start_, int width_, int rows_)
        {
            HashSet<MapNode> reachable = new HashSet<MapNode>();
            Stack<MapNode> frontier = new Stack<MapNode>();
            frontier.Push(start_);
            while (frontier.Count > 0)
            {
                MapNode node = frontier.Pop();
                if (!reachable.Add(node)) continue;
                foreach (MapNode child in node.Children) frontier.Push(child);
            }

            List<MapNode> dead = new List<MapNode>();
            for (int col = 0; col < width_; col++)
                for (int row = 0; row < rows_; row++)
                    if (grid_[col, row] is MapNode node && !reachable.Contains(node))
                        dead.Add(node);

            foreach (MapNode node in dead)
            {
                foreach (MapNode parent in node.Parents.ToArray()) parent.RemoveChildPoint(node);
                foreach (MapNode child in node.Children.ToArray()) node.RemoveChildPoint(child);
                grid_[node.Coord.Col, node.Coord.Row] = null;
            }
        }

        #endregion

        #region 布局后处理：整图居中 → 逐行散开 → 短链拉直（纯坐标调整，不改类型与连线语义）

        private static void PostProcessLayout(MapNode[,] grid_, MapNode boss_, int width_, int rows_)
        {
            CenterGrid(grid_, boss_, width_, rows_);
            SpreadAdjacentMapPoints(grid_, width_, rows_);
            StraightenPaths(grid_, width_, rows_);
        }

        // 一次性平移到网格内左右对称（整数除法天然把奇数差收敛为容差 1，避免反复 ±1 奇偶震荡死循环）；
        // Boss 在网格外，跟随平移保持观感连接
        private static void CenterGrid(MapNode[,] grid_, MapNode boss_, int width_, int rows_)
        {
            var (minCol, maxCol) = OccupiedColumnBounds(grid_, width_, rows_);
            int delta = Math.Clamp((width_ - 1 - (minCol + maxCol)) / 2, -minCol, width_ - 1 - maxCol);
            if (delta != 0) ShiftColumns(grid_, boss_, width_, rows_, delta);
        }

        private static (int minCol, int maxCol) OccupiedColumnBounds(MapNode[,] grid_, int width_, int rows_)
        {
            int min = int.MaxValue, max = int.MinValue;
            for (int col = 0; col < width_; col++)
                for (int row = 0; row < rows_; row++)
                    if (grid_[col, row] != null)
                    {
                        if (col < min) min = col;
                        if (col > max) max = col;
                    }
            return (min, max);
        }

        private static void ShiftColumns(MapNode[,] grid_, MapNode boss_, int width_, int rows_, int delta_)
        {
            List<MapNode> nodes = new List<MapNode>();
            for (int col = 0; col < width_; col++)
                for (int row = 0; row < rows_; row++)
                    if (grid_[col, row] != null) nodes.Add(grid_[col, row]);

            Array.Clear(grid_, 0, grid_.Length);
            foreach (MapNode node in nodes)
            {
                node.Coord = new MapCoord((byte)(node.Coord.Col + delta_), node.Coord.Row);
                grid_[node.Coord.Col, node.Coord.Row] = node;
            }
            boss_.Coord = new MapCoord((byte)(boss_.Coord.Col + delta_), boss_.Coord.Row);
        }

        // 逐行散开：每节点在"父列±1 ∩ 子列±1"的空位列中，选与同排邻居间隔最大者（连线不被拉断）
        private static void SpreadAdjacentMapPoints(MapNode[,] grid_, int width_, int rows_)
        {
            for (int row = 1; row < rows_; row++)
                for (int col = 0; col < width_; col++)
                {
                    MapNode node = grid_[col, row];
                    if (node == null) continue;

                    int best = col;
                    int bestGap = RowMinGap(grid_, row, col, width_);
                    foreach (int candidate in AllowedColumns(node, width_))
                    {
                        if (candidate == col || grid_[candidate, row] != null) continue;
                        int gap = RowMinGap(grid_, row, candidate, width_);
                        if (gap > bestGap || (gap == bestGap && Math.Abs(candidate - col) < Math.Abs(best - col)))
                        {
                            best = candidate;
                            bestGap = gap;
                        }
                    }
                    if (best != col) MoveNode(grid_, node, best);
                }
        }

        private static HashSet<int> AllowedColumns(MapNode node_, int width_)
        {
            HashSet<int> allowed = null;
            CollectNeighbourColumns(node_.Parents, ref allowed);
            CollectNeighbourColumns(node_.Children, ref allowed);
            if (allowed == null) return new HashSet<int>();
            allowed.RemoveWhere(c_ => c_ < 0 || c_ >= width_);
            return allowed;
        }

        private static void CollectNeighbourColumns(List<MapNode> neighbours_, ref HashSet<int> allowed_)
        {
            foreach (MapNode neighbour in neighbours_)
            {
                HashSet<int> band = new HashSet<int> { neighbour.Coord.Col - 1, neighbour.Coord.Col, neighbour.Coord.Col + 1 };
                if (allowed_ == null) allowed_ = band;
                else allowed_.IntersectWith(band);
            }
        }

        // 与同排其他节点的最小间距；同排没有别的节点时返回 width（视为最大）
        private static int RowMinGap(MapNode[,] grid_, int row_, int col_, int width_)
        {
            int gap = width_;
            for (int c = 0; c < width_; c++)
            {
                if (c == col_ || grid_[c, row_] == null) continue;
                gap = Math.Min(gap, Math.Abs(c - col_));
            }
            return gap;
        }

        // 拉直：恰好 1 父 1 子且父子同列、自身偏向一侧的中间节点，挪到父列（空位才挪）
        private static void StraightenPaths(MapNode[,] grid_, int width_, int rows_)
        {
            for (int row = 1; row < rows_; row++)
                for (int col = 0; col < width_; col++)
                {
                    MapNode node = grid_[col, row];
                    if (node == null || node.Parents.Count != 1 || node.Children.Count != 1) continue;
                    int line = node.Parents[0].Coord.Col;
                    if (line == node.Children[0].Coord.Col && line != col && grid_[line, row] == null)
                        MoveNode(grid_, node, line);
                }
        }

        // 行内平移：清旧格写新格并更新坐标
        private static void MoveNode(MapNode[,] grid_, MapNode node_, int newCol_)
        {
            if (node_.Coord.Col == newCol_) return;
            grid_[node_.Coord.Col, node_.Coord.Row] = null;
            node_.Coord = new MapCoord((byte)newCol_, node_.Coord.Row);
            grid_[newCol_, node_.Coord.Row] = node_;
        }

        #endregion
    }
}
