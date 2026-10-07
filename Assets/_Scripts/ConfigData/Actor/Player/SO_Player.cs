using System.Collections.Generic;
using UnityEngine;

namespace _DreamKeep.ConfigData
{
    /// <summary>玩家配置：起始能量、每回合抽牌数与起始卡组；表现数据与初始状态同敌人完全同构。</summary>
    [CreateAssetMenu(fileName = "Player", menuName = "DreamKeep/Data/Actor/Player")]
    public class SO_Player : ScriptableObject
    {
        [Header("基础配置")]
        [SerializeField] private string _name;
        [SerializeField] private int _maxHP;
        [SerializeField] private GameObject _viewPrefab;

        [Header("玩家配置")]
        [SerializeField] private int _startGold; //TODO:还没做商店相关内容，先不急
        [SerializeField] private int _startEnergy = 3;
        [SerializeField] private int _drawPerTurn = 5;

        [SerializeField] private List<SO_Card> _startDeck = new();

        [Header("初始状态")]
        [SerializeField] private List<PerkEntry> _startPerks = new();

        //TODO:还有玩家初始遗物没添加
        public string Name => _name;
        public int MaxHp => _maxHP;
        public GameObject ViewPrefab => _viewPrefab;
        public int StartGold => _startGold;
        public int StartEnergy => _startEnergy;
        public int DrawPerTurn => _drawPerTurn;
        public List<SO_Card> StartDeck => _startDeck;
        public List<PerkEntry> StartPerks => _startPerks;
    }
}
