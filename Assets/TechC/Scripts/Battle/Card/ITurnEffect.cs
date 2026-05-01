using System.Collections.Generic;

namespace TechC.ODDESEY.Battle
{
    /// <summary>
    /// ターンをまたぐ効果のインターフェース。ターン開始・ドロー前・ドロー後・カード解決後のタイミングで呼ばれる。
    /// </summary>
    public interface ITurnEffect
    {
        void OnTurnStart(BattleLogic logic);
        void OnBeforeDraw(BattleLogic logic);
        void OnAfterDraw(BattleLogic logic, List<CardInstance> hand);
        void OnCardResolved(BattleLogic logic, CardResolveResult result);

        bool IsExpired { get; }
    }
}