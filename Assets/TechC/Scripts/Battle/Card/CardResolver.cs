using System.Collections.Generic;
using TechC.ODDESEY.Util;
using TechC.VBattle.Core.Extensions;

namespace TechC.ODDESEY.Battle
{
    /// <summary>
    /// プレイゾーン上のカードを解決し、CardResolveResult のリストを返す。
    ///
    /// 責務：
    ///   1. スロットを左から順に走査する
    ///   2. EvaluateResolveValues() → EffectExecutionState 生成 → ExecuteAll() の順で呼ぶ
    ///   3. 解決後に State から Result へ値を転写する
    ///   4. バトル終了フラグを検知したらループを抜ける
    ///
    /// BattleLogic.ConfirmTurn() はこのクラスに解決を委譲するだけでよい。
    /// 「新しい解決前後フック（チェーン割り込みなど）」はここに追加する。
    ///
    /// 設計メモ：
    ///   BattleLogic から分離することで、将来「同時解決モード」や
    ///   「カード間連携トリガー」を追加する際の変更範囲を CardResolver 内に限定できる。
    /// </summary>
    public class CardResolver
    {
        private readonly BattleLogic logic;

        public CardResolver(BattleLogic logic)
        {
            this.logic = logic;
        }

        /// <summary>
        /// プレイゾーン全スロットを解決し、結果リストを返す。
        /// </summary>
        /// <param name="playZone">プレイゾーンのスロット配列</param>
        /// <param name="hand">現在の手札（手札連動ダメージの計算に使う）</param>
        /// <param name="isHotMode">激アツモードフラグ</param>
        /// <param name="discardCallback">プレイヤーカードを解決後に捨て札へ移す処理</param>
        public List<CardResolveResult> ResolveAll(
            PlayZoneSlot[] playZone,
            List<CardInstance> hand,
            bool isHotMode,
            System.Action<CardInstance> discardCallback)
        {
            var results = new List<CardResolveResult>();

            for (int slotIndex = 0; slotIndex < playZone.Length; slotIndex++)
            {
                var slot = playZone[slotIndex];
                if (slot == null || slot.IsEmpty) continue;

                var instance = slot.IsEnemyCard ? slot.EnemyCardInstance : slot.PlayerCardInstance;
                // プレイヤーカードの ProbabilityDownEffect が登録した値を
                // 敵カードの BonusProbability に反映する（負値で確率を下げる）
                if (slot.IsEnemyCard)
                {
                    int reductionRate = logic.EnemyProbabilityReductionRate;
                    if (reductionRate > 0)
                    {
                        float reductionAmount = reductionRate / 100f;
                        for (int ei = 0; ei < instance.OriginalData.Effects.Count; ei++)
                            instance.AddBonusProbability(ei, -reductionAmount);
                    }
                }
                // ── 1. 遅延評価を確定 ────────────────────────────────────
                instance.EvaluateResolveValues(hand.Count, isHotMode);

                // ── 2. Context / State / Result を生成 ──────────────────
                var result = new CardResolveResult
                {
                    SlotIndex = slotIndex,
                    IsPlayer = !slot.IsEnemyCard,
                    IsHit = false,
                    DamageDealt = 0,
                    CardInstanceId = instance.InstanceId,
                    AnimationType = instance.OriginalData.AnimationType,
                };

                var context = new EffectContext
                {
                    Logic = logic,
                    Source = instance,
                    IsEnemy = slot.IsEnemyCard,
                    SlotIndex = slotIndex,
                    CurrentHandCount = hand.Count,
                    Result = result,
                };

                // EffectExecutionState はスロットごとに新規生成（Effect間通信の初期化）
                var state = new EffectExecutionState();

                // ── 3. 全 Effect を実行 ──────────────────────────────────
                instance.ExecuteAll(context, state);

                // ── 4. State → Result に転写 ─────────────────────────────
                FlushStateToResult(state, result);

                results.Add(result);

                // ── 5. プレイヤーカードを捨て札へ ─────────────────────────
                if (!slot.IsEnemyCard)
                    discardCallback?.Invoke(instance);

                if (slot.IsEnemyCard && result.IsHit)
                {
                    bool countered = logic.TryCounter(result);
                    if (countered)
                    {
                        result.SetExtra(ResultKeys.CounterTriggered, true);

                        CustomLogger.Info(
                            $"カウンター反撃: Slot {slotIndex}",
                            LogTagUtil.TagBattle);
                    }
                }

                // ── 6. バトル終了チェック ────────────────────────────────
                if (result.IsBattleEnd) break;
            }

            return results;
        }

        /// <summary>
        /// EffectExecutionState の累積値を CardResolveResult.Extras に転写する。
        /// Result の具体的なフィールドを増やさずに済む。
        /// </summary>
        private static void FlushStateToResult(EffectExecutionState state, CardResolveResult result)
        {
            if (state.TotalSelfDamage > 0)
                result.SetExtra(ResultKeys.SelfDamageDealt, state.TotalSelfDamage);

            if (state.IsCritical)
                result.SetExtra(ResultKeys.IsCritical, true);

            if (state.DamageReductionRate > 0)
                result.SetExtra(ResultKeys.ReductionRate, state.DamageReductionRate);
        }
    }
}