using _DreamKeep.RuntimeData;
using TMPro;
using UnityEngine;

namespace _DreamKeep.Presentation
{
    /// <summary>格挡组件：数字文本 + AC_BlockFrame 的 Show 布尔驱动显隐，
    /// 只认 UI_Actor 转发来的域真值，自己不订事件不判规则。</summary>
    public sealed class UI_BlockView : MonoBehaviour
    {
        [Header("格挡")] [SerializeField] private Animator _anim;
        [SerializeField] private TextMeshProUGUI _blockAmount;

        private static readonly int BlockShow = Animator.StringToHash("Show"); // 动画参数名只哈希一次，避免每次写值都重算

        private int _shownBlock = -1;

        // 初始化：装配期调用——按域真值直写格挡初值
        public void Initialize(Creature creature_) => SetBlock(creature_.Block);

        #region 设置格挡

        // 格挡：数值恒写域真值；显隐交给 AC_BlockFrame 的 Show 布尔，值没变就一个字都不写
        public void SetBlock(int block_)
        {
            if (_shownBlock == block_) return;
            _shownBlock = block_;
            _blockAmount.text = block_ > 0 ? block_.ToString() : "";
            _anim.SetBool(BlockShow, block_ > 0);
        }

        #endregion
    }
}
