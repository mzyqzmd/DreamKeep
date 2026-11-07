using System;
using _DreamKeep.EngineUtility;
using UnityEngine;
using UnityEngine.UI;

namespace _DreamKeep.CommonPresentation
{
    /// <summary>放弃二次确认面板：关闭与确认都交回整局编排者，本面板不认识存档服务。</summary>
    public sealed class UI_GiveUpPanel : UI_Panel
    {
        [SerializeField] private Button _closeButton;
        [SerializeField] private Button _confirmButton;

        #region 接线：两个按钮只转发意图

        public void Wire(Action onClose_, Action onConfirm_)
        {
            if (_closeButton) _closeButton.onClick.AddListener(() => onClose_?.Invoke());
            if (_confirmButton) _confirmButton.onClick.AddListener(() => onConfirm_?.Invoke());
        }

        #endregion
    }
}
