using TMPro;
using UnityEngine;

namespace _DreamKeep.Presentation
{
    public sealed class UI_DevelopLogBar : MonoBehaviour
    {
        [SerializeField] private TextMeshProUGUI _titleText;
        [SerializeField] private TextMeshProUGUI _descriptionText;

        [SerializeField] private string _title; //未显式输入标题就按照设置内容时的输入的内容来
        [SerializeField, TextArea(3, 10)] private string _description;

        public void SetContent(string title_, string description_)
        {
            _titleText.text = string.IsNullOrEmpty(title_) ? title_ : _title;
            _descriptionText.text = description_;
        }
    }
}