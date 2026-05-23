using System;
using System.Collections.Generic;
using UnityEngine;

namespace TechC.ODDESEY.Battle
{
    // ─── 条件種別 ─────────────────────────────────────────────────────────

    public enum EnemyConditionType
    {
        /// <summary>予告フラグが立っている（前ターンに予告カードを使った）</summary>
        AnnouncedAction,
        /// <summary>敵HPが threshold% 以下</summary>
        EnemyHpBelow,
        /// <summary>前ターンに敵が受けたダメージが threshold 以上</summary>
        ReceivedDamageAbove,
        /// <summary>プレイヤーのゲージが threshold% 以上</summary>
        PlayerGaugeAbove,
    }

    // ─── 優先順位 ─────────────────────────────────────────────────────────

    /// <summary>
    /// 条件分岐の優先順位。数値が小さいほど先に評価される。
    /// 仕様通り：予告 > HPフェーズ > 受けたダメージ > プレイヤーゲージ
    /// </summary>
    public enum EnemyConditionPriority
    {
        Announced = 0,  // 予告後の確定行動
        HpPhase = 1,  // HPフェーズ専用行動
        ReceivedDamage = 2,  // 前ターン反応行動
        PlayerGauge = 3,  // 運ゲージ対策行動
    }

    // ─── 評価コンテキスト ─────────────────────────────────────────────────

    /// <summary>
    /// 条件評価に必要な状態スナップショット。
    /// BattleLogic が BeginTurn で生成して渡す。
    /// </summary>
    public struct EnemyActionContext
    {
        /// <summary>敵の現在HP割合（0〜1）</summary>
        public float EnemyHpRatio;
        /// <summary>プレイヤーのゲージ割合（0〜1）</summary>
        public float PlayerGaugeRatio;
        /// <summary>前ターンに敵が受けたダメージ合計</summary>
        public int LastTurnDamageTaken;
        /// <summary>前ターンに予告フラグが立っているか</summary>
        public bool IsAnnounced;
    }

    // ─── 条件分岐1つ ────────────────────────────────────────────────────

    [Serializable]
    public class EnemyConditionEntry
    {
        [Tooltip("条件の種別")]
        public EnemyConditionType conditionType;

        [Tooltip("発動優先順位（小さいほど先）")]
        public EnemyConditionPriority priority;

        [Tooltip("しきい値（HP%・ダメージ量・ゲージ% をそれぞれ 0〜1 または実数値で）")]
        public float threshold;

        [Tooltip("この条件が成立したとき代わりに出すカード（最大2枚）")]
        public List<CardData> overrideCards = new();

        [Tooltip("デバッグ用ラベル（例：HP50%以下で強連撃）")]
        public string label;

        /// <summary>コンテキストを見て条件が成立するか判定する。</summary>
        public bool Evaluate(in EnemyActionContext ctx)
        {
            return conditionType switch
            {
                EnemyConditionType.AnnouncedAction => ctx.IsAnnounced,
                EnemyConditionType.EnemyHpBelow => ctx.EnemyHpRatio <= threshold,
                EnemyConditionType.ReceivedDamageAbove => ctx.LastTurnDamageTaken >= threshold,
                EnemyConditionType.PlayerGaugeAbove => ctx.PlayerGaugeRatio >= threshold,
                _ => false,
            };
        }
    }

    // ─── 1ターン分の行動 ────────────────────────────────────────────────

    [Serializable]
    public class EnemyTurnEntry
    {
        [Tooltip("デバッグ用ラベル（例：通常攻撃、牽制+攻撃）")]
        public string label;

        [Tooltip("基本ローテーションのカード（最大2枚）")]
        public List<CardData> cards = new();

        [Tooltip("条件分岐リスト。Priority の昇順に評価される。")]
        public List<EnemyConditionEntry> conditions = new();

        /// <summary>
        /// コンテキストを評価して使用するカードを解決する。
        /// 優先順位（Priority）の昇順で条件を見て、
        /// 最初に成立したものの overrideCards を返す。
        /// 何も成立しなければ基本の cards を返す。
        /// </summary>
        public List<CardData> ResolveCards(in EnemyActionContext ctx)
        {
            // Priority 昇順でソートしてから評価
            var sorted = new List<EnemyConditionEntry>(conditions);
            sorted.Sort((a, b) => ((int)a.priority).CompareTo((int)b.priority));

            foreach (var cond in sorted)
                if (cond.Evaluate(ctx))
                    return cond.overrideCards;

            return cards;
        }
    }

    // ─── メインの ScriptableObject ───────────────────────────────────────

    /// <summary>
    /// 敵の行動パターンを定義する ScriptableObject。
    /// EnemyData にアサインして使う。
    ///
    /// ローテーション：ループ（1→2→3→1→...）
    /// 条件分岐：Priority 昇順で評価、最初に成立したものを採用
    /// </summary>
    [CreateAssetMenu(fileName = "EnemyActionPattern",
                     menuName = "ODDESEY/EnemyActionPattern")]
    public class EnemyActionPattern : ScriptableObject
    {
        [Tooltip("ターンごとの行動定義（順番通りにループ）")]
        public List<EnemyTurnEntry> turns = new();

        /// <summary>
        /// turnIndex（0始まり）からローテーション位置を求め、
        /// コンテキストを評価して使用するカードを返す。
        /// </summary>
        public List<CardData> ResolveCards(int turnIndex, in EnemyActionContext ctx)
        {
            if (turns == null || turns.Count == 0)
                return new List<CardData>();

            int index = (turnIndex - 1) % turns.Count; // turnIndex は1始まり
            if (index < 0) index = 0;

            return turns[index].ResolveCards(ctx);
        }

        /// <summary>
        /// 現在の基本ローテーションラベルを返す（デバッグ用）。
        /// </summary>
        public string GetLabel(int turnIndex)
        {
            if (turns == null || turns.Count == 0) return "";
            int index = (turnIndex - 1) % turns.Count;
            return turns[index].label;
        }
    }
}