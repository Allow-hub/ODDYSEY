using System.Collections.Generic;
using TechC.ODDESEY.Battle;

namespace TechC.ODDESEY.Event
{
    /// <summary>
    /// 1アクションの実行結果。
    /// </summary>
    public class EventActionResult
    {
        public EventResultType ResultType;
        public int             ResultValue;
        public List<CardData>  DrawnCards = new();
    }

    /// <summary>
    /// 挑戦全体の結果。複数アクションの結果を持つ。
    /// </summary>
    public class EventResult
    {
        public bool                    IsSuccess;
        public string                  FlavorText;
        public int                     RefundedGauge;

        /// <summary>実行された全アクションの結果リスト。</summary>
        public List<EventActionResult> ActionResults = new();

        // ─── 後方互換 ─────────────────────────────────────────────────────

        /// <summary>旧 ResultType の代替。最初のアクションを返す。</summary>
        public EventResultType ResultType
            => ActionResults.Count > 0 ? ActionResults[0].ResultType : EventResultType.None;

        /// <summary>旧 ResultValue の代替。</summary>
        public int ResultValue
            => ActionResults.Count > 0 ? ActionResults[0].ResultValue : 0;

        /// <summary>旧 DrawnCards の代替。全アクションのカードをまとめて返す。</summary>
        public List<CardData> DrawnCards
        {
            get
            {
                var all = new List<CardData>();
                foreach (var a in ActionResults)
                    all.AddRange(a.DrawnCards);
                return all;
            }
        }
    }
}