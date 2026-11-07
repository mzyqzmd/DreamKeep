using System.Collections.Generic;
using _DreamKeep.EngineUtility;
using UnityEngine;

namespace _DreamKeep.Presentation
{
    public sealed class UI_DevelopLogPanel : UI_Panel
    {
        [SerializeField] private List<UI_DevelopLogBar> _barContentDict = new();
        
    }
}