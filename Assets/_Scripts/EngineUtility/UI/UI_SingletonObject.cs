using UnityEngine;

namespace _DreamKeep.EngineUtility
{
    public sealed class UI_SingletonObject : MonoBehaviour
    {
        private static UI_SingletonObject _instance;

        private void Awake()
        {
            if (_instance && _instance != this)
            {
                Destroy(gameObject);
                return;
            }

            _instance = this;
            DontDestroyOnLoad(gameObject);
        }

        private void OnDestroy()
        {
            if (_instance == this) _instance = null;
        }
    }
}
