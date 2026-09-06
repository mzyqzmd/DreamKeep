using _DreamKeep.Core;
using System;
using UnityEngine;

namespace _DreamKeep.Data
{
    [Serializable]
    public abstract class AffixData_Base
    {
        [SerializeField] protected string _name;
        [SerializeField] protected string _description;
        [SerializeField] protected bool _showDescription;

        public string Name => _name;
        public string Description => _description;
        public bool ShowDescription => _showDescription;

        public abstract void Execute(IActor source_, IActor target_);
    }
}
