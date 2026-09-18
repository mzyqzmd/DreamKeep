using _DreamKeep.GameBattle;
using Cysharp.Threading.Tasks;
using TMPro;
using UnityEngine;
using VContainer;

namespace _DreamKeep.Presentation
{
    /// <summary>剩余能量视图：订阅能量变化事件把「当前/上限」写到文本上，回能、消耗与获得能量都即时刷新。
    /// 不轮询、不读战斗态，能量何时变化一律由域侧决定。</summary>
    public class Obj_EnergyRemain : MonoBehaviour
    {
        [Tooltip("剩余能量文本：格式为「当前/上限」，如 3/3")] [SerializeField]
        private TextMeshPro _remainEnergyText;

        #region 接线：能量变化事件驱动文本刷新

        [Inject]
        public void Construct(BattleEventDispatcher eventDispatcher_)
            => eventDispatcher_.Register<BE_EnergyChanged>(OnEnergyChangedAsync);

        #endregion

        private UniTask OnEnergyChangedAsync(BE_EnergyChanged changed_)
        {
            _remainEnergyText.text = $"{changed_.Current}/{changed_.Max}";
            return UniTask.CompletedTask;
        }
    }
}
