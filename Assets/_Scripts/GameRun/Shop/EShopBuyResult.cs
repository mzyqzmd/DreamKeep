namespace _DreamKeep.GameRun
{
    /// <summary>商店购买结果：成功与否加上失败原因，视图据此决定「成交消失」还是「抖动提示」。
    /// 已售出与不存在同码——对视图来说都是「这一格不该再被点」。</summary>
    public enum EShopBuyResult
    {
        Success,
        NotEnoughGold,
        PotionSlotsFull,
        SoldOut,
    }
}
