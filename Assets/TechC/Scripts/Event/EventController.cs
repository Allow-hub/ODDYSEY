using System;
using TechC.Core.Manager;
using TechC.ODDESEY.Battle;
using UnityEngine;

namespace TechC.ODDESEY.Event
{
    /// <summary>
    /// イベントノードの管理。
    ///
    /// 変更点：
    ///   - GainCard 結果を受け取ったとき、MainManager.AddCardToContext() に委譲する。
    ///   - ゲージ同期を GameContext.LuckGauge 経由に統一。
    ///     MainManager.SetLackGaugeValue は使わなくなった。
    /// </summary>
    public class EventController : MonoBehaviour
    {
        public event Action OnEventCompleted;

        [SerializeField] private EventView eventView;
        [SerializeField] private LuckGaugeView luckGaugeView;
        [SerializeField] private GameObject canvasObj;

        [Header("デバッグ用（本番は Initialize(EventData) で渡す）")]
        [SerializeField] private EventData debugEventData;

        private EventLogic logic = new();

        // ─── 初期化 ───────────────────────────────────────────────────────

        public void Initialize() => Initialize(debugEventData);

        public void Initialize(EventData data)
        {
            var context = MainManager.I?.GameContext;
            if (data == null || context == null)
            {
                Debug.LogWarning("[EventController] EventData または GameContext が null です");
                return;
            }

            // GameContext の LuckGauge を MainManager の値で同期
            context.LuckGauge = MainManager.I.LuckGaugeValue;
            luckGaugeView.Setup(MainManager.I.LuckGaugeMax);  
            luckGaugeView.UpdateGaugeImmediate(MainManager.I?.LuckGaugeValue ?? 0f, 100f, false);

            logic.Setup(data, context);

            eventView.Setup(
                data,
                logic.CurrentGauge,
                logic.ReservedGauge,
                logic.FinalSuccessRate,
                onChallenge: OnChallengePressed,
                onCancel: OnCancelPressed,
                onAddGauge: OnAddGaugePressed,
                onRemoveGauge: OnRemoveGaugePressed
            );
            
            Instantiate(data.EventPrefab, canvasObj.transform);

            RefreshView();
        }

        // ─── ボタン入力 ──────────────────────────────────────────────────

        private void OnAddGaugePressed()
        {
            logic.TryAddReserved(1);
            RefreshView();
        }

        private void OnRemoveGaugePressed()
        {
            logic.TryRemoveReserved(1);
            RefreshView();
        }

        private void OnChallengePressed()
        {
            var result = logic.ChallengeAndApply();

            // GainCard は MainManager に委譲
            if (result.ResultType == EventResultType.GainCard)
                MainManager.I?.AddCardToContext(result.ResultValue);

            // GameContext のゲージを MainManager に書き戻す
            SyncGaugeToMainManager();

            eventView.ShowResult(result, OnResultClosed);
        }

        private void OnCancelPressed()
        {
            logic.Cancel();
            CompleteEvent();
        }

        private void OnResultClosed() => CompleteEvent();

        // ─── 内部処理 ────────────────────────────────────────────────────

        private void RefreshView()
        {
            eventView.UpdateGaugeInfo(
                currentGauge: logic.CurrentGauge,
                reservedGauge: logic.ReservedGauge,
                finalSuccessRate: logic.FinalSuccessRate,
                canAdd: logic.CanAddReserved(),
                canRemove: logic.CanRemoveReserved()
            );
        }

        /// <summary>
        /// GameContext に変更されたゲージ値を MainManager に書き戻す。
        /// </summary>
        private void SyncGaugeToMainManager()
        {
            var context = MainManager.I?.GameContext;
            if (context == null) return;
            MainManager.I.SetLackGaugeValue(context.LuckGauge);
            luckGaugeView.UpdateGaugeImmediate(context.LuckGauge, MainManager.I.LuckGaugeMax, true);
        }

        private void CompleteEvent()
        {
            SyncGaugeToMainManager();
            OnEventCompleted?.Invoke();
        }
    }
}