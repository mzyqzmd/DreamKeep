using System.Collections.Generic;
using UnityEngine;

namespace _DreamKeep.ConfigData
{
    /// <summary>先古之民配置：一份资产描述"这位先古之民是谁"（名号、双方说话人外观、分页对话集、专属遗物池），全局唯一真源，运行期只读。
    /// 演绎时由 AncientSession 随机抽一页；按拜访次数定向取页等存档系统落地后再启用。</summary>
    [CreateAssetMenu(fileName = "Ancient", menuName = "DreamKeep/Data/Ancient")]
    public sealed class SO_Ancient : ScriptableObject
    {
        [SerializeField] private string _ancientName;
        [SerializeField] private string _positionName;

        [Tooltip("先古之民一方的气泡外观")] [SerializeField]
        private AncientSpeakerView _ancientView = new();

        [Tooltip("守梦人一方的气泡外观")] [SerializeField]
        private AncientSpeakerView _playerView = new();

        [SerializeField] private List<AncientDialoguePage> _pages = new();

        [Tooltip("本先古之民的专属遗物池：三选一候选从这里掷出")] [SerializeField]
        private SO_RelicPool _relicPool;

        /// <summary>名字：名号牌与横幅的主文本位（如"涅奥"）。</summary>
        public string AncientName => _ancientName;

        /// <summary>职位名：名号牌与横幅的副文本位（如"沉睡者"），空则隐藏副位。</summary>
        public string PositionName => _positionName;

        public AncientSpeakerView AncientView => _ancientView;
        public AncientSpeakerView PlayerView => _playerView;

        public IReadOnlyList<AncientDialoguePage> Pages => _pages;

        /// <summary>专属遗物池：三选一候选的来源，漏接线由装配期守卫暴露。</summary>
        public SO_RelicPool RelicPool => _relicPool;
    }
}