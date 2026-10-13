using _DreamKeep.RuntimeData;
using UnityEngine;

namespace _DreamKeep.Presentation.Intent
{
    /// <summary>施加效果意图图标：给自己上状态用强化图，给玩家上状态用负面图；
    /// 数据由 UI_EnemyIntentGroup 生成后经 Bind 写入，悬停说明由基类统一承接。</summary>
    public sealed class UI_ApplyPerkIntent : UI_Intent
    {
        private static readonly int IsSelf = Animator.StringToHash("IsSelf");

        [SerializeField] private Animator _anim;

        public void Bind(EI_ApplyPerk applyPerk_)
        {
            BindIntent(applyPerk_);
            _anim.SetBool(IsSelf, applyPerk_.IsSelf);
        }
    }
}
