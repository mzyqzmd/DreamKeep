namespace _DreamKeep.GameBattle
{
    /// <summary>出牌被拒原因：结算结果不变，只供表现层按类别选台词。
    /// NotInHand 属时序竞争（牌已离手），表现层不据此开口。</summary>
    public enum ECardPlayRejectReason
    {
        NotInHand,
        NotEnoughEnergy,
        Unplayable,
        DizzyLimited,
    }
}
