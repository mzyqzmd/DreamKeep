using _DreamKeep.RuntimeData;
using UnityEngine;
using UnityEngine.UI;

namespace _DreamKeep.Presentation.Intent
{
    /// <summary>施加效果意图图标：给自己上状态用强化图，给玩家上状态用负面图。
    /// 数据由 UI_EnemyIntentGroup 生成后经 Bind 写入。</summary>
    public class UI_ApplyPerkIntent : MonoBehaviour
    {
        [SerializeField] private Image _icon;
        [SerializeField] private Sprite _upIcon;
        [SerializeField] private Sprite _debuffIcon;

        public void Bind(EI_ApplyPerk applyPerk_) => _icon.sprite = applyPerk_.IsSelf ? _upIcon : _debuffIcon;
    }
}
