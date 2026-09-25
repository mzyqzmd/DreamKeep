using System;
using UnityEngine;

namespace _DreamKeep.ConfigData
{
    /// <summary>给予词条：打出后为指定张数（Count）的牌授予一个词条模板（GrantedAffix），运行时由 Domain 层动态增删。</summary>
    [Serializable]
    [AddTypeMenu("Affix/给予")]
    public class AD_Grant : AffixData
    {
        [SerializeReference,SubclassSelector] private AffixData _grantedAffix;  //给予的词条
        [SerializeField] private int _count;  //可以给几张牌给予词条

        public AffixData GrantedAffix => _grantedAffix;
        public int Count => _count;
        public override string Name => "给予";
    }
}
