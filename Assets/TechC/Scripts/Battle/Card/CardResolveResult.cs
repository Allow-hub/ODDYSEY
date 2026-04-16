using System.Collections.Generic;

namespace TechC.ODDESEY.Battle
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
        public int CardInstanceId;
        public List<AppliedStatusInfo> AppliedStatuses = new();
    }
}