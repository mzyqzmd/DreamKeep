using System.Collections.Generic;
using UnityEngine;

namespace _DreamKeep.Data
{
    [CreateAssetMenu(fileName = "NewCard", menuName = "DreamKeep/Data/Card")]
    public class SO_Card : ScriptableObject
    {
        [SerializeField] private string _name;
        [SerializeField] private int _cost;
        [SerializeField] private Sprite _icon;
        [SerializeField] private ECardType _type;
        [SerializeField] private ECardTargetType _targetType;
        [SerializeReference] private List<AffixData> _affixes = new();
        [SerializeReference] private List<AffixData> _updatedAffixes = new();
        [SerializeField] private bool _canUpdate;

        public string Name => _name;
        public int Cost => _cost;
        public Sprite Icon => _icon;
        public ECardType Type => _type;
        public ECardTargetType TargetType => _targetType;
        public List<AffixData> Affixes => _affixes;
        public List<AffixData> UpdatedAffixes => _updatedAffixes;
        public bool CanUpdate => _canUpdate;
    }
}
