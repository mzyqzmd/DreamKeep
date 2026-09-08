using System.Collections.Generic;
using System.Text;

namespace _DreamKeep.RuntimeData
{
    /// <summary>词条集合通用操作：局外卡与战斗卡共用，避免两处重复有序插入与描述拼接。</summary>
    internal static class CardAffixUtility
    {
        #region 按管线权重稳定插入，权重相同插在既有同权重之后（修饰型恒 -1，必在首位）
        public static void InsertOrdered(List<CardAffix> affixes_, CardAffix affix_)
        {
            int index = affixes_.Count;
            for (int i = 0; i < affixes_.Count; i++)
            {
                if (affixes_[i].PipelineOrder <= affix_.PipelineOrder) continue;
                index = i;
                break;
            }
            affixes_.Insert(index, affix_);
        }
        #endregion

        #region 拼接整张卡的描述文本
        public static string Describe(IReadOnlyList<CardAffix> affixes_)
        {
            StringBuilder description = new();
            for (int i = 0; i < affixes_.Count; i++)
                description.AppendLine(affixes_[i].GetDescription());
            return description.ToString();
        }
        #endregion
    }
}
