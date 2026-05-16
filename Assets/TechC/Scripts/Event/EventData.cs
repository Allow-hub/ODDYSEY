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

    [CreateAssetMenu(fileName = "EventData", menuName = "ODDESEY/EventData")]
    public class EventData : ScriptableObject
    {
        [Header("基本情報")]
        public string EventId;
        public string EventName;
        public EventMapIconType MapIconType;
        public GameObject EventPrefab;  // イベントシーンで表示するPrefab（UIでも3Dオブジェクトでも可）

        [TextArea(3, 6)]
        public string Description;

        [Tooltip("挑戦ボタンに表示するテキスト")]
        public string ChallengeText;

        [Header("成功率")]
        [Range(0, 100)]
        public int BaseSuccessRate = 60;

        [Header("成功時")]
        public EventResultType SuccessResultType;
        public int SuccessResultValue;

        [Tooltip("SuccessResultType が GainCard のとき、このリストから抽選する")]
        public List<CardData> SuccessCardCandidates = new();

        [TextArea(1, 3)]
        public string SuccessFlavorText;

        [Header("失敗時")]
        public EventResultType FailureResultType;
        public int FailureResultValue;

        [Tooltip("FailureResultType が GainCard のとき、このリストから抽選する")]
        public List<CardData> FailureCardCandidates = new();

        [TextArea(1, 3)]
        public string FailureFlavorText;

        [Header("失敗時の運ゲージ還元量（%）")]
        public int FailureGaugeRefund = 10;

        // ─── ユーティリティ ───────────────────────────────────────────────

        /// <summary>
        /// 成功時のカード候補からランダムに count 枚取り出す。
        /// 候補が足りない場合は候補数を上限とする。
        /// </summary>
        public List<CardData> DrawSuccessCards(int count)
            => DrawCards(SuccessCardCandidates, count);

        /// <summary>
        /// 失敗時のカード候補からランダムに count 枚取り出す。
        /// </summary>
        public List<CardData> DrawFailureCards(int count)
            => DrawCards(FailureCardCandidates, count);

        private static List<CardData> DrawCards(List<CardData> pool, int count)
        {
            if (pool == null || pool.Count == 0) return new List<CardData>();

            var copy = new List<CardData>(pool);
            var result = new List<CardData>();
            int take = Mathf.Min(count, copy.Count);

            for (int i = 0; i < take; i++)
            {
                int idx = Random.Range(0, copy.Count);
                result.Add(copy[idx]);
                copy.RemoveAt(idx); // 重複なし抽選
            }

            return result;
        }
    }
}