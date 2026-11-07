using System;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;

namespace _DreamKeep.CommonPresentation
{
    /// <summary>开发日志条（左栏目录项）：标题与正文只认 Inspector 填的两个字段，目录项自身不显示正文。
    /// 整条直接吃点击（不依赖 Button，场景免加组件），点击把本条上报给面板切换右栏标题与正文。</summary>
    public sealed class UI_DevelopLogBar : MonoBehaviour, IPointerClickHandler
    {
        [SerializeField] private TextMeshProUGUI _titleText;
        [SerializeField] private string _title; //目录项标题：右栏标题取这里

        [SerializeField, TextArea(3, 10)] private string _description;

        /// <summary>点击上报：面板据此切换右栏正文。</summary>
        public event Action<UI_DevelopLogBar> Clicked;

        public string Title => string.IsNullOrEmpty(_titleText.text) ? _title : _titleText.text;
        public string Description => _description;

        void IPointerClickHandler.OnPointerClick(PointerEventData eventData_) =>
            Clicked?.Invoke(this);
    }
}