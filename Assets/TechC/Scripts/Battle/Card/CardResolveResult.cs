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
        public CardAnimationType AnimationType { get; set; } = CardAnimationType.Attack;

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

    /// <summary>
    /// CardResolveResult.Extras で使うキー定数。
    /// 新しい Extra 情報を追加するときはここに追記する。
    /// </summary>
    public static class ResultKeys
    {
        public const string SelfDamageDealt = "SelfDamageDealt";
        public const string IsCritical = "IsCritical";
        public const string ReductionRate = "ReductionRate";
        public const string CounterTriggered = "CounterTriggered";

        // ─── シールド ─────────────────────────────────────────────────────
        /// <summary>攻撃前のプレイヤーシールド量</summary>
        public const string PlayerShieldBefore = "PlayerShieldBefore";
        /// <summary>攻撃後のプレイヤーシールド量</summary>
        public const string PlayerShieldAfter = "PlayerShieldAfter";
        /// <summary>攻撃前の敵シールド量</summary>
        public const string EnemyShieldBefore = "EnemyShieldBefore";
        /// <summary>攻撃後の敵シールド量</summary>
        public const string EnemyShieldAfter = "EnemyShieldAfter";

        // ─── シールド付与 ─────────────────────────────────────────────────
        /// <summary>このカードでプレイヤーに付与したシールド量</summary>
        public const string PlayerShieldGained = "PlayerShieldGained";
        /// <summary>このカードで敵に付与したシールド量</summary>
        public const string EnemyShieldGained = "EnemyShieldGained";
        /// <summary>削ったゲージ量。</summary>
        public const string GaugeDrained = "GaugeDrained";
        // ─── 予告 ─────────────────────────────────────────────────────────
        /// <summary>このカードが予告フラグを立てたか。BattleView の演出で参照する。</summary>
        public const string IsAnnounced = "IsAnnounced";
        // ─── デッキ妨害 ───────────────────────────────────────────────────
        /// <summary>このカードで注入したノイズ枚数。</summary>
        public const string NoiseInjected = "NoiseInjected";

        // ─── 反撃バフ ─────────────────────────────────────────────────────
        /// <summary>このカード解決中に反撃バフが発動したか。</summary>
        public const string ReflectTriggered = "ReflectTriggered";
        /// <summary>反撃バフが発動したときの反撃ダメージ量。</summary>
        public const string ReflectDamage = "ReflectDamage";
    }
}