using UnityEngine;

namespace TechC.ODDESEY.Event
{
    /// <summary>
    /// イベントの結果タイプ。
    /// </summary>
    public enum EventResultType
    {
        None,       // 何も起きない
        HealHp,     // HPを回復する
        DamageHp,   // HPを減らす
        GainGauge,  // 運ゲージを増やす
        LoseGauge,  // 運ゲージを減らす
        GainCard,   // カードを獲得する
    }

    /// <summary>
    /// マップアイコンの種別（プレイヤーに見せる分類）。
    /// </summary>
    public enum EventMapIconType
    {
        Card,   // カード獲得系
        Heal,   // 回復系
        Risk,   // 高リスク系
    }

    /// <summary>
    /// イベント1件のデータ定義。ScriptableObject として管理する。
    ///
    /// 仕様書 9-1 のデータ項目に対応。
    /// </summary>
    [CreateAssetMenu(fileName = "EventData", menuName = "ODDESEY/EventData")]
    public class EventData : ScriptableObject
    {
        [Header("基本情報")]
        public string EventId;
        public string EventName;
        public EventMapIconType MapIconType;

        public GameObject EventPrefab;  // イベントシーンで表示するPrefab（UIでも3Dオブジェクトでも可）

        [TextArea]
        public string Description;

        [Tooltip("挑戦ボタンに表示するテキスト（例：残骸を解析する）")]
        public string ChallengeText;

        [Header("成功率")]
        [Range(0, 100)]
        [Tooltip("運ゲージ補正前の基礎成功率（%）")]
        public int BaseSuccessRate = 60;

        [Header("成功時")]
        public EventResultType SuccessResultType;
        public int SuccessResultValue;

        [TextArea(1, 3)]
        public string SuccessFlavorText;

        [Header("失敗時")]
        public EventResultType FailureResultType;
        public int FailureResultValue;

        [TextArea]
        public string FailureFlavorText;

        [Header("失敗時の運ゲージ還元量（%）")]
        [Tooltip("運ゲージを1%以上消費した場合のみ還元される")]
        public int FailureGaugeRefund = 10;
    }
}
