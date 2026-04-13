namespace TechC.ODDESEY.Battle
{
    /// <summary>
    /// プレイゾーンの1スロット分のデータ。
    /// 敵カード・プレイヤーカードどちらも同じスロットに入る。
    /// BattleLogic が状態を管理し、View は読み取るだけ。
    /// </summary>
    public class PlayZoneSlot
    {
        /// <summary>プレイヤーカード（CardInstance）。敵カードの場合は null</summary>
        public CardInstance PlayerCardInstance { get; set; }

        /// <summary>敵カード（CardData）。プレイヤーカードの場合は null</summary>
        public CardData EnemyCard { get; set; }

        /// <summary>true = 敵カード、false = プレイヤーカード</summary>
        public bool IsEnemyCard { get; set; }

        // -------------------------------------------------------
        // 運ゲージ消費による強化量（プレイヤーカードのみ有効）
        // -------------------------------------------------------

        /// <summary>
        /// 確率への上乗せ量（0〜100%、運ゲージ 1% → 確率 +1%）。
        /// BaseProbability + BonusProbability が実効確率（上限 100%）。
        /// </summary>
        public float BonusProbability { get; set; } = 0f;

        /// <summary>
        /// ダメージへの上乗せ量（運ゲージ 5% → ダメージ +1）。
        /// BaseDamage + BonusDamage が実効ダメージ（上限なし）。
        /// </summary>
        public int BonusDamage { get; set; } = 0;

        // -------------------------------------------------------
        // 実効値（読み取り専用プロパティ）
        // -------------------------------------------------------

        /// <summary>実際に判定で使う確率（基礎値 + ボーナス、上限 100%）</summary>
        public float EffectiveProbability
        {
            get
            {
                if (PlayerCardInstance == null || !IsPlayerCard) return 0f;
                // CardInstance の EffectiveProbability は運ゲージボーナスを含む
                // プレイヤー操作で追加のボーナスを加算
                return System.Math.Min(PlayerCardInstance.GetEffectiveProbability(0) + BonusProbability / 100f, 1f);
            }
        }

        /// <summary>実際に判定で使うダメージ（基礎値 + ボーナス）</summary>
        public int EffectiveDamage
        {
            get
            {
                if (PlayerCardInstance == null || !IsPlayerCard) return 0;
                // CardInstance の EffectiveDamage に UI経由のボーナスを加算
                return PlayerCardInstance.GetEffectiveDamage(0) + BonusDamage;
            }
        }

        public bool IsEmpty     => PlayerCardInstance == null && EnemyCard == null;
        public bool IsPlayerCard => PlayerCardInstance != null && !IsEnemyCard;

        /// <summary>スロットをリセット（ターン終了時に呼ぶ）</summary>
        public void Clear()
        {
            PlayerCardInstance = null;
            EnemyCard          = null;
            IsEnemyCard        = false;
            BonusProbability   = 0f;
            BonusDamage        = 0;
        }
    }
}