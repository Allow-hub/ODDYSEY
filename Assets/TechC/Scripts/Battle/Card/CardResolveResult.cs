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
        public bool IsBattleEnd;  // このカードで決着がついたか
        public bool IsWon;        // true=プレイヤー勝利, false=プレイヤー敗北
        public int PlayerHpAfter;
        public int EnemyHpAfter;

        /// <summary>捨て身など外れ時にプレイヤーが受けた自傷ダメージ</summary>
        public int SelfDamageDealt;

        /// <summary>クリティカルが発生したか</summary>
        public bool IsCritical;

        /// <summary>防御カードによる軽減率（0〜100%）。0 なら軽減なし</summary>
        public int ReductionRate;

        public List<AppliedStatusInfo> AppliedStatuses = new();
    }
}