using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using VContainer;
using _DreamKeep.ConfigData;
using _DreamKeep.GameRun;

namespace _DreamKeep.Presentation
{
    /// <summary>先古对话面板编排：订阅名号与对话页事实，写名号（名字与职位两文本）、按行实例化说话人模板气泡逐条堆叠
    /// （打字机＋点击推进：揭示中点击＝补完，揭示后点击＝下一行），页首清空旧气泡、页尾销毁除末条外全部；
    /// 点击热区为面板整面（场景接线透明 Image），选项卷轴的点选不会冒泡到这里。</summary>
    public sealed class UI_AncientDialoguePanel : MonoBehaviour, IPointerClickHandler
    {
        [SerializeField] private GameObject _ancientTemplate; // 先古之民的气泡模板（DialogueItem_L.prefab）
        [SerializeField] private GameObject _playerTemplate; // 守梦人的气泡模板（DialogueItem_R.prefab）

        [Tooltip("气泡挂载点：一般就是本面板，VerticalLayoutGroup 负责堆叠")]
        [SerializeField]
        private RectTransform _bubbleParent;

        [SerializeField] private TMP_Text _ancientName;
        [SerializeField] private TMP_Text _positionName;

        [Tooltip("逐字揭示速度（字/秒）")]
        [SerializeField]
        private float _charsPerSecond = 30f;

        private readonly List<UI_AncientDialogueItem> _bubbles = new();
        private UI_AncientDialogueItem _activeItem;
        private bool _revealing;
        private UniTaskCompletionSource _advanceClick;

        [Inject]
        public void Construct(AncientEventDispatcher events_)
        {
            events_.Register<AE_NameplateBound>(bound_ => BindNameplate(bound_.AncientName, bound_.PositionName));
            events_.Register<AE_DialoguePageOffered>(offered_ =>
                PlayPageAsync(offered_.Page, offered_.AncientView, offered_.PlayerView));
        }

        private void BindNameplate(string ancientName_, string positionName_)
        {
            _ancientName.text = ancientName_;
            _positionName.gameObject.SetActive(!string.IsNullOrEmpty(positionName_));
            _positionName.text = positionName_ ?? "";
        }

        #region 对外演绎：整页台词逐条生成气泡，播完只留末句

        /// <summary>演完一页：逐条生成气泡、揭示与点击推进；页尾销毁除末条外全部气泡，返回即该页演完。</summary>
        public async UniTask PlayPageAsync(AncientDialoguePage page_, AncientSpeakerView ancientView_, AncientSpeakerView playerView_)
        {
            IReadOnlyList<AncientDialogueLine> lines = page_.Lines;
            if (lines.Count == 0) return;

            ClearBubbles();

            for (int i = 0; i < lines.Count; i++)
            {
                AncientDialogueLine line = lines[i];
                bool ancient = line.Speaker == EAncientSpeaker.Ancient;
                _activeItem = Spawn(ancient ? _ancientTemplate : _playerTemplate);
                _bubbles.Add(_activeItem);
                _activeItem.Bind(line.Text, ancient ? ancientView_ : playerView_);

                _revealing = true;
                await _activeItem.PlayRevealAsync(_charsPerSecond, this.GetCancellationTokenOnDestroy());

                // 先备好推进扳机再解除揭示态：揭示刚结束那一瞬的点击也算"翻下一行"
                _advanceClick = new UniTaskCompletionSource();
                _revealing = false;
                if (i < lines.Count - 1) await _advanceClick.Task;
            }

            // 页毕收拢：只保留最后一句所在气泡
            UI_AncientDialogueItem last = _bubbles[^1];
            for (int i = _bubbles.Count - 2; i >= 0; i--) Destroy(_bubbles[i].gameObject);
            _bubbles.Clear();
            _bubbles.Add(last);
        }

        #endregion

        public void OnPointerClick(PointerEventData eventData_)
        {
            if (_revealing) _activeItem.CompleteReveal();
            else _advanceClick?.TrySetResult();
        }

        // 页首清场：把上一页遗留的气泡（含页尾留存的末句）全部销毁
        private void ClearBubbles()
        {
            for (int i = 0; i < _bubbles.Count; i++)
                if (_bubbles[i] != null) Destroy(_bubbles[i].gameObject);
            _bubbles.Clear();
        }

        private UI_AncientDialogueItem Spawn(GameObject template_)
        {
            GameObject bubble = Instantiate(template_, _bubbleParent);
            return bubble.GetComponent<UI_AncientDialogueItem>();
        }
    }
}
