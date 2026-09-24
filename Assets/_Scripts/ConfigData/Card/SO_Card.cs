using System.Collections.Generic;
using NaughtyAttributes;
using UnityEngine;

namespace _DreamKeep.ConfigData
{
    [CreateAssetMenu(fileName = "NewCard", menuName = "DreamKeep/Data/Card")]
    public class SO_Card : ScriptableObject
    {
        [SerializeField] private string _name;
        [SerializeField] private int _cost;
        [SerializeField] private Sprite _icon;
        [SerializeField] private ECardRarity _rarity;
        [SerializeField] private ECardType _type;
        [SerializeField] private ECardTargetType _targetType;
        [SerializeReference] private List<AffixData> _affixes = new();
        [SerializeField] private bool _canUpdate;

        [ShowIf("_canUpdate")]
        [SerializeReference]
        private List<AffixData> _updatedAffixes = new();

        public string Name => _name;
        public int Cost => _cost;
        public Sprite Icon => _icon;
        public ECardRarity Rarity => _rarity;
        public ECardType Type => _type;
        public ECardTargetType TargetType => _targetType;
        public List<AffixData> Affixes => _affixes;
        public List<AffixData> UpdatedAffixes => _updatedAffixes;
        public bool CanUpdate => _canUpdate;
    }
}