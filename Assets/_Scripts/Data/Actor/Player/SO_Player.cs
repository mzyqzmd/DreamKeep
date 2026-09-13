using System.Collections.Generic;
using UnityEngine;

namespace _DreamKeep.Data
{
    /// <summary>玩家配置：起始能量、每回合抽牌数与起始卡组；生命上限来自单位配置基类。</summary>
    [CreateAssetMenu(fileName = "Player", menuName = "DreamKeep/Data/Actor/Player")]
    public class SO_Player : SO_Actor
    {
        [SerializeField] private int _startGold;//TODO:还没做商店相关内容，先不急
        [SerializeField] private int _startEnergy = 3;
        [SerializeField] private int _drawPerTurn = 5;
        [SerializeField] private List<SO_Card> _startDeck = new();
        //TODO:还有玩家初始遗物没添加

        public int StartGold => _startGold;
        public int StartEnergy => _startEnergy;
        public int DrawPerTurn => _drawPerTurn;
        public List<SO_Card> StartDeck => _startDeck;
    }
}
