using System;
using _DreamKeep.EngineUtility;
using UnityEngine;
using UnityEngine.UI;

namespace _DreamKeep.CommonPresentation
{
    public class UI_QuitGamePanel : UI_Panel
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