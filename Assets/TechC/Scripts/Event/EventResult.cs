using System.Collections.Generic;
using TechC.ODDESEY.Battle;

namespace TechC.ODDESEY.Event
{
    /// <summary>
    /// 挑戦結果。EventController が EventView に渡す。
    /// DrawnCards に抽選されたカードが入る。
    /// </summary>
    public class EventResult
    {
        public bool IsSuccess;
        public EventResultType ResultType;
        public int ResultValue;
        public string FlavorText;
        public int RefundedGauge;

        /// <summary>
        /// GainCard のとき、抽選されたカードが入る。
        /// EventController がこれを見て GameContext に追加する。
        /// </summary>
        public List<CardData> DrawnCards = new();
    }
}