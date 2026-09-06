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
        [SerializeReference] private List<AffixData> _affixes = new();

        public string Name => _name;
        public int Cost => _cost;
        public Sprite Icon => _icon;
        public ECardType Type => _type;
        public List<AffixData> Affixes => _affixes;
    }
}
