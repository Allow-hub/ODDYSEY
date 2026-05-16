using System.Collections.Generic;
using TechC.ODDESEY.Battle;
using UnityEngine;

namespace TechC.ODDESEY
{
    /// <summary>
    /// ゲーム全体のコンテキスト。純粋C#クラス。
    /// ステージをまたいで永続するデータのみ持つ。
    ///
    /// 変更点：
    ///   - HP・ゲージ操作のヘルパーメソッドを追加。
    ///     EventLogic / BattleLogic がここを経由して状態を変更する。
    ///   - AddCard() を追加。MainManager がカード獲得時に呼ぶ。
    /// </summary>
    public class GameContext
    {
        public int PlayerHp    { get; set; }
        public int PlayerHpMax { get; set; }
        public float LuckGauge { get; set; }  // 0〜100
        public Dictionary<CardData, int> Deck;
        public List<CardData> RewardCandidates;
        public EnemyData CurrentEnemy;

        // ─── HP 操作 ──────────────────────────────────────────────────────

        public void HealHp(int amount)
            => PlayerHp = Mathf.Min(PlayerHp + amount, PlayerHpMax);

        public void DamageHp(int amount)
            => PlayerHp = Mathf.Max(0, PlayerHp - amount);

        // ─── ゲージ操作 ───────────────────────────────────────────────────

        public void AddGauge(float amount)
            => LuckGauge = Mathf.Clamp(LuckGauge + amount, 0f, 100f);

        public void SpendGauge(float amount)
            => LuckGauge = Mathf.Clamp(LuckGauge - amount, 0f, 100f);

        // ─── カード操作 ───────────────────────────────────────────────────

        /// <summary>
        /// デッキにカードを1枚追加する。
        /// EventController の GainCard 結果、または RewardController から呼ぶ。
        /// </summary>
        public void AddCard(CardData card)
        {
            if (card == null) return;
            Deck ??= new Dictionary<CardData, int>();

            if (Deck.ContainsKey(card))
                Deck[card]++;
            else
                Deck[card] = 1;
        }
    }

    /// <summary>
    /// Inspector から GameContext の初期値を設定するデバッグ用データ。
    /// </summary>
    [System.Serializable]
    public class DebugGameContext
    {
        [Header("プレイヤー")]
        public int PlayerHp    = 30;
        public int PlayerHpMax = 30;
        public float LuckGauge = 0f;

        [Header("初期デッキ")]
        public CardDeckSO InitialDeck;

        [Header("デバッグ用敵")]
        public EnemyData DebugEnemy;

        public GameContext ToGameContext() => new()
        {
            PlayerHp    = PlayerHp,
            PlayerHpMax = PlayerHpMax,
            LuckGauge   = LuckGauge,
            Deck        = InitialDeck != null ? InitialDeck.ToDictionary() : new(),
            CurrentEnemy = DebugEnemy,
        };
    }
}