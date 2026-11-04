using UnityEngine;

namespace _DreamKeep.ConfigData
{
    /// <summary>地图生成配置：网格规模、路径数与类型配额参数（高斯参数内联），初值照文档 §4，数值由用户在资产上调。
    /// 纯数据：掷骰由 GameRun 侧生成器完成（本程序集不引 Utility 的 Rng）。</summary>
    [CreateAssetMenu(fileName = "MapGenerationConfig", menuName = "DreamKeep/Data/Map/地图生成配置")]
    public class SO_MapGenerationConfig : ScriptableObject
    {
        [Header("网格")] [Tooltip("网格列数")] [SerializeField]
        private int _mapWidth = 7;

        [Tooltip("房间行数（不含起点与 Boss 两锚点行）")] [SerializeField]
        private int _roomRows = 15;

        [Tooltip("起点路径条数")] [SerializeField] private int _pathCount = 7;

        [Header("类型配额（固定语义行之外）")] [Tooltip("精英目标数量")] [SerializeField]
        private int _numOfElites = 5;

        [Tooltip("商店目标数量")] [SerializeField] private int _numOfShops = 3;

        [Tooltip("未知（?）数量：高斯均值")] [SerializeField]
        private int _unknownMean = 12;

        [Tooltip("未知数量：高斯标准差")] [SerializeField]
        private int _unknownStd = 1;

        [Tooltip("未知数量：下限")] [SerializeField] private int _unknownMin = 10;
        [Tooltip("未知数量：上限")] [SerializeField] private int _unknownMax = 14;

        [Tooltip("休整数量：高斯均值")] [SerializeField]
        private int _restMean = 5;

        [Tooltip("休整数量：高斯标准差")] [SerializeField]
        private int _restStd = 1;

        [Tooltip("休整数量：下限")] [SerializeField] private int _restMin = 3;
        [Tooltip("休整数量：上限")] [SerializeField] private int _restMax = 6;

        [Header("合法性约束（行号从 0 数）")] [Tooltip("此行号之前禁止休息与精英（下层限制）")] [SerializeField]
        private int _lowerRowLimit = 5;

        [Tooltip("距最后一行还剩这些行内禁止休息（上层限制）")] [SerializeField]
        private int _upperRestRowLimit = 3;

        [Header("未知（?）揭示权重")] [Tooltip("揭示为普通战斗的权重")] [SerializeField]
        private int _revealMonster = 45;

        [Tooltip("揭示为精英的权重")] [SerializeField] private int _revealElite = 10;

        [Tooltip("揭示为商店的权重")] [SerializeField] private int _revealShop = 10;

        [Tooltip("揭示为休息的权重")] [SerializeField] private int _revealRest = 10;

        [Tooltip("揭示为宝箱的权重")] [SerializeField] private int _revealTreasure = 10;

        [Tooltip("揭示为事件的权重")] [SerializeField] private int _revealEvent = 15;

        public int MapWidth => _mapWidth;
        public int RoomRows => _roomRows;
        public int PathCount => _pathCount;
        public int NumOfElites => _numOfElites;
        public int NumOfShops => _numOfShops;
        public int UnknownMean => _unknownMean;
        public int UnknownStd => _unknownStd;
        public int UnknownMin => _unknownMin;
        public int UnknownMax => _unknownMax;
        public int RestMean => _restMean;
        public int RestStd => _restStd;
        public int RestMin => _restMin;
        public int RestMax => _restMax;
        public int LowerRowLimit => _lowerRowLimit;
        public int UpperRestRowLimit => _upperRestRowLimit;
        public int RevealMonster => _revealMonster;
        public int RevealElite => _revealElite;
        public int RevealShop => _revealShop;
        public int RevealRest => _revealRest;
        public int RevealTreasure => _revealTreasure;
        public int RevealEvent => _revealEvent;
    }
}