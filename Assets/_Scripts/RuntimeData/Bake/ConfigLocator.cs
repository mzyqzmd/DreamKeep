using _DreamKeep.Data;

namespace _DreamKeep.RuntimeData
{
    /// <summary>配置定位器：持有外层注入的卡牌数据库，供烘焙器按 Id 取配置，注入后全程只读。</summary>
    public static class ConfigLocator
    {
        private static SO_CardDatabase _cardDatabase;

        public static void Inject(SO_CardDatabase cardDatabase_) => _cardDatabase = cardDatabase_;

        #region 按 Id 取配置卡，数据库未注入时返回 false 而不是抛空引用
        public static bool TryGetCard(string cardId_, out SO_Card card_)
        {
            if (_cardDatabase == null)
            {
                card_ = null;
                return false;
            }
            return _cardDatabase.TryGetCard(cardId_, out card_);
        }
        #endregion
    }
}
