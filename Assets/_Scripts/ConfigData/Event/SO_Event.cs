using UnityEngine;

namespace _DreamKeep.ConfigData
{
    public abstract class SO_Event : ScriptableObject
    {
        [SerializeField] private string _eventName;
    }
}