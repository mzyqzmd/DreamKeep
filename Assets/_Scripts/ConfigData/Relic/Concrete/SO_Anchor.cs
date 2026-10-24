using UnityEngine;

namespace _DreamKeep.ConfigData
{
    /// <summary>锚：每场战斗开始时获得格挡（落点在首回合「回合开始」事件，清格挡之后）。</summary>
    [CreateAssetMenu(fileName = "Anchor", menuName = "DreamKeep/Data/Relic/锚")]
    public sealed class SO_Anchor : SO_Relic
    {
        [SerializeField] private int _block = 10;

        public int Block => _block;
    }
}
