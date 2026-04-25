using System.Collections.Generic;

namespace TechC.ODDESEY.Battle
{
    /// <summary>
    /// カード1枚の解決結果。BattleController 経由で View に渡す。
    ///
    /// リファクタリング変更点：
    ///   効果固有フィールド（SelfDamageDealt, IsCritical, ReductionRate）を
    ///   Extras ディクショナリに移動し、新効果追加時にクラスを変更しなくてよくした。
    ///   よく使う値は Result 拡張メソッドで糖衣構文を提供する。
    /// </summary>
    public class CardResolveResult
    {
        public int SlotIndex;
        public bool IsPlayer;
        public bool IsHit;
        public int DamageDealt;
        public int CardInstanceId;

        public int EnemyHpAfter;
        public int PlayerHpAfter;

        public bool IsBattleEnd;
        public bool IsWon;

        /// <summary>
        /// 効果固有の追加情報。新効果を追加するたびにクラスを変更しなくてよい。
        /// キー名は定数で管理すること（ResultKeys 参照）。
        /// </summary>
        public Dictionary<string, object> Extras { get; } = new Dictionary<string, object>();

        // ─── 糖衣構文 ────────────────────────────────────────────────────
        public void SetExtra<T>(string key, T value) => Extras[key] = value;

        public T GetExtra<T>(string key, T defaultValue = default)
        {
            if (Extras.TryGetValue(key, out var v) && v is T typed) return typed;
            return defaultValue;
        }
    }

    /// <summary>Extras のキー名定数。タイポ防止と検索性向上のために集約する。</summary>
    public static class ResultKeys
    {
        public const string SelfDamageDealt = "SelfDamageDealt";
        public const string IsCritical = "IsCritical";
        public const string ReductionRate = "ReductionRate";
    }
}