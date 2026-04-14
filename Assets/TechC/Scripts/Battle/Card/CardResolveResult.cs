namespace TechC.ODDESEY
{
    /// <summary>
    /// カード1枚の解決結果を表すクラス。
    /// </summary>
    public class CardResolveResult
    {
        public int SlotIndex;
        public bool IsPlayer;
        public bool IsHit;
        public int DamageDealt;

        // 拡張用
        public bool WasBroken;
        public bool LuckGaugeChanged;
    }
}
