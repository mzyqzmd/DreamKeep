using System.Collections.Generic;
using _DreamKeep.CommonPresentation;
using UnityEngine;

namespace _DreamKeep.Presentation
{
    /// <summary>商人说话：预摆气泡列表里纯随机挑一个（允许连续选中同一个），始终只显示一个；
    /// 气泡允许被打断——每次开口先把全部气泡的时间线与激活状态收干净，再重新挑一个重播。
    /// 台词取词与次数上限归段入口，本类只管挑哪个气泡演。</summary>
    public sealed class UI_MerchantSpeech : MonoBehaviour
    {
        [SerializeField] private List<UI_SpeechBubble> _bubbles = new();

        private void Awake() => HideAll();

        #region 对外入口：挑一个气泡播一句

        /// <summary>播一句商人台词：空台词或空列表直接丢弃；返回选中气泡的播放结果。</summary>
        public bool Play(string line_)
        {
            if (string.IsNullOrEmpty(line_) || _bubbles.Count == 0) return false;
            HideAll();
            UI_SpeechBubble picked = _bubbles[Random.Range(0, _bubbles.Count)];
            return picked && picked.Play(line_, true);
        }

        #endregion

        private void HideAll()
        {
            for (int i = 0; i < _bubbles.Count; i++)
            {
                if (!_bubbles[i]) continue;
                _bubbles[i].ResetView();
                _bubbles[i].gameObject.SetActive(false);
            }
        }
    }
}
