using System.Collections.Generic;
using _DreamKeep.ConfigData;

namespace _DreamKeep.GameRun
{
    /// <summary>一张已生成的整章地图：7 列网格 + 起点/首领两锚点，节点与边全部来自生成器。
    /// 可达性只有一条规则——节点内容完成（MarkCleared）后才点亮它的子节点；起点开局即视为已通过，
    /// 否则第一行永远无人可选（先古段没有游戏内入口）。</summary>
    public class MapGraph
    {
        public int Width { get; }
        public int Rows { get; }
        public MapNode StartNode { get; }
        public MapNode BossNode { get; }
        public MapNode CurrentNode { get; private set; }

        private readonly MapNode[,] _grid;
        private readonly HashSet<MapNode> _cleared = new HashSet<MapNode>();

        public MapGraph(int width_, int rows_, MapNode[,] grid_, MapNode startNode_, MapNode bossNode_)
        {
            Width = width_;
            Rows = rows_;
            _grid = grid_;
            StartNode = startNode_;
            BossNode = bossNode_;
            _cleared.Add(startNode_); // 起点预置已通过：先古段只有开发面板入口，玩家从它的下一行开始
            RecalculateTravelability(); // 构造完即自洽，不把"首帧状态"挂到外部的 EnterNode 上
        }

        /// <summary>取网格节点：越界或空位返回 null。</summary>
        public MapNode GetNode(int col_, int row_)
        {
            if ((uint)col_ >= (uint)Width || (uint)row_ >= (uint)Rows) return null;
            return _grid[col_, row_];
        }

        #region 进入节点：只记当前位置并熄掉兄弟分支，不放行下一层

        /// <summary>段内容未完成：在内容通过之前整张图不放出任何可走节点（视觉与点击共用这一条真源）。</summary>
        public bool SegmentPending { get; private set; }

        internal void EnterNode(MapNode node_)
        {
            CurrentNode = node_;
            RecalculateTravelability();
        }

        /// <summary>翻转段进行态并重算可达性：值不变时不做无谓重算。</summary>
        internal void SetSegmentPending(bool pending_)
        {
            if (SegmentPending == pending_) return;
            SegmentPending = pending_;
            RecalculateTravelability();
        }

        #endregion

        #region 标记通过：段内容完成的唯一落点，幂等

        internal void MarkCleared(MapNode node_)
        {
            if (node_ == null || !_cleared.Add(node_)) return;
            RecalculateTravelability();
        }

        #endregion

        // 已通过恒 Traveled；当前已进入未通过恒 InProgress；只有当前节点通过后，其未通过的子节点才可走
        private void RecalculateTravelability()
        {
            for (int col = 0; col < Width; col++)
                for (int row = 0; row < Rows; row++)
                    if (_grid[col, row] != null)
                        _grid[col, row].State = StateOf(_grid[col, row]);

            if (BossNode != null) BossNode.State = StateOf(BossNode);

            // 段还没跑完内容就不放行：起点先古是预置已通过，不挡这一下的话进先古段时第一排会一直亮着
            if (SegmentPending || CurrentNode == null || !_cleared.Contains(CurrentNode)) return;
            foreach (MapNode child in CurrentNode.Children)
                if (!_cleared.Contains(child))
                    child.State = EMapNodeState.Travelable;
        }

        private EMapNodeState StateOf(MapNode node_) =>
            _cleared.Contains(node_) ? EMapNodeState.Traveled :
            node_ == CurrentNode ? EMapNodeState.InProgress : EMapNodeState.Untravelable;
    }
}
