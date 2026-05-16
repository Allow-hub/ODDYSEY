using System.Collections.Generic;
using TechC.ODDESEY.Battle;
using UnityEngine;

namespace TechC.ODDESEY.Event
{
    public enum EventResultType
    {
        None,
        HealHp,
        DamageHp,
        GainGauge,
        LoseGauge,
        GainCard,
    }

    public enum EventMapIconType
    {
        Card,
        Heal,
        Risk,
    }

    /// <summary>
    /// 成功・失敗それぞれの結果の1アクション。
    /// 複数持てるため「カード獲得 + ゲージ+20」のような複合結果が表現できる。
    /// </summary>
    [System.Serializable]
    public class EventResultAction
    {
        public EventResultType ResultType;
        public int             ResultValue;

        [Tooltip("ResultType が GainCard のとき、このリストから抽選する")]
        public List<CardData> CardCandidates = new();
    }

    [CreateAssetMenu(fileName = "EventData", menuName = "ODDESEY/EventData")]
    public class EventData : ScriptableObject
    {
        [Header("基本情報")]
        public string           EventId;
        public string           EventName;
        public EventMapIconType MapIconType;
        public GameObject       EventPrefab;

        [TextArea(3, 6)]
        public string Description;

        [Tooltip("挑戦ボタンに表示するテキスト")]
        public string ChallengeText;

        [Header("成功率")]
        [Range(0, 100)]
        public int BaseSuccessRate = 60;

        [Header("成功時（複数設定可）")]
        public List<EventResultAction> SuccessActions = new();

        [TextArea(1, 3)]
        public string SuccessFlavorText;

        [Header("失敗時（複数設定可）")]
        public List<EventResultAction> FailureActions = new();

        [TextArea(1, 3)]
        public string FailureFlavorText;

        [Header("失敗時の運ゲージ還元量（%）")]
        public int FailureGaugeRefund = 10;

        // ─── 後方互換プロパティ（既存コードが壊れないように） ────────────

        /// <summary>旧 SuccessResultType の代替。最初の Action を返す。</summary>
        public EventResultType SuccessResultType
            => SuccessActions.Count > 0 ? SuccessActions[0].ResultType : EventResultType.None;

        /// <summary>旧 SuccessResultValue の代替。</summary>
        public int SuccessResultValue
            => SuccessActions.Count > 0 ? SuccessActions[0].ResultValue : 0;

        /// <summary>旧 FailureResultType の代替。</summary>
        public EventResultType FailureResultType
            => FailureActions.Count > 0 ? FailureActions[0].ResultType : EventResultType.None;

        /// <summary>旧 FailureResultValue の代替。</summary>
        public int FailureResultValue
            => FailureActions.Count > 0 ? FailureActions[0].ResultValue : 0;

        // ─── カード抽選 ───────────────────────────────────────────────────

        /// <summary>指定 Action のカード候補から count 枚抽選する。</summary>
        public List<CardData> DrawCards(EventResultAction action, int count)
            => DrawCardsFromPool(action.CardCandidates, count);

        private static List<CardData> DrawCardsFromPool(List<CardData> pool, int count)
        {
            if (pool == null || pool.Count == 0) return new List<CardData>();

            var copy   = new List<CardData>(pool);
            var result = new List<CardData>();
            int take   = Mathf.Min(count, copy.Count);

            for (int i = 0; i < take; i++)
            {
                int idx = Random.Range(0, copy.Count);
                result.Add(copy[idx]);
                copy.RemoveAt(idx);
            }
            return result;
        }
    }
}