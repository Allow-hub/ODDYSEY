using System.Collections.Generic;
using TechC.ODDESEY.Util;
using TechC.VBattle.Core.Extensions;
using UnityEngine;

namespace TechC.ODDESEY.Battle
{
    /// <summary>
    /// 蓄積エフェクト。
    ///
    /// 仕様：
    ///   確定発動。次のターン中、AddLuckGauge() で加算されるゲージ量を
    ///   MultiplierMin〜MultiplierMax 倍にする（例: 1.3〜1.5 倍）。
    ///
    /// 実装方針：
    ///   ProbabilityEffect と同じく CardEffectBase + ITurnEffect の複合実装。
    ///   Execute() で倍率を pendingMultiplier に保持して AddTurnEffect(this) を呼ぶ。
    ///   次ターンの OnTurnStart() で BattleLogic.SetLuckGaugeMultiplier() に登録。
    ///   登録後に IsExpired = true にして1ターン限りで失効。
    /// </summary>
    [CreateAssetMenu(menuName = "ODDESEY/CardEffect/GaugeAccumulation")]
    public class GaugeAccumulationEffect : CardEffectBase, ITurnEffect
    {
        [Header("ゲージ倍率（範囲）")]
        [Tooltip("1.3 = 30%増加")]
        public float MultiplierMin = 1.3f;
        public float MultiplierMax = 1.5f;

        public bool IsExpired => isExpired;
        private bool isExpired = false;
        private float pendingMultiplier = 1f; // Execute → OnTurnStart へ倍率を引き継ぐ

        // ─── CardEffectBase ───────────────────────────────────────────────

        public override void RollValue(EffectSlot slot, bool isHotMode)
        {
            // 確定発動なので確率は常に 1
            slot.RolledProbability = 1f;

            // 倍率を int として保持（1.3 → 130、1.5 → 150）
            int min = Mathf.RoundToInt(MultiplierMin * 100);
            int max = Mathf.RoundToInt(MultiplierMax * 100);

            slot.Value = isHotMode
                ? max
                : Random.Range(min, max + 1);

            slot.ValueRange = (min, max);
        }

        public override void Execute(EffectContext context, EffectExecutionState state, int effectIndex)
        {
            isExpired = false;

            int valueInt = context.Source.GetEffectiveValue(effectIndex);
            pendingMultiplier = valueInt / 100f; // 倍率を保持

            context.Logic.AddTurnEffect(this); // 次ターンの OnTurnStart を予約

            context.Result.IsHit = true;

            CustomLogger.Info(
                $"蓄積効果登録: 次ターンのゲージ蓄積量 x{pendingMultiplier:F2}",
                LogTagUtil.TagCard);
        }

        // ─── ITurnEffect ──────────────────────────────────────────────────

        public void OnTurnStart(BattleLogic logic)
        {
            // 次ターン開始時にゲージ倍率を登録して即失効
            logic.SetLuckGaugeMultiplier(pendingMultiplier);
            isExpired = true;

            CustomLogger.Info(
                $"蓄積効果発動: このターンのゲージ蓄積量 x{pendingMultiplier:F2}",
                LogTagUtil.TagCard);
        }

        public void OnBeforeDraw(BattleLogic logic) { }
        public void OnAfterDraw(BattleLogic logic, List<CardInstance> hand) { }
        public void OnCardResolved(BattleLogic logic, CardResolveResult result) { }
    }
}