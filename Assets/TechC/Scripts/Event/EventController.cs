using System;
using TechC.Core.Manager;
using TechC.VBattle.Core.Extensions;
using TechC.ODDESEY.Battle;
using UnityEngine;

namespace TechC.ODDESEY.Event
{
    public class EventController : MonoBehaviour
    {
        public event Action OnEventCompleted;

        [SerializeField] private EventView eventView;
        [SerializeField] private LuckGaugeView luckGaugeView;
        [SerializeField] private GameObject canvasObj;
        [SerializeField] private EventData debugEventData;

        private GameObject currentEventPrefab;
        private EventLogic logic = new();

        public void Initialize() => Initialize(debugEventData);

        public void Initialize(EventData data)
        {
            if (data == null)
            {
                CustomLogger.Error("[EventController] EventData が null です。");
                return;
            }
            if (eventView == null)
            {
                CustomLogger.Error("[EventController] EventView が null です。");
                return;
            }

            var context = MainManager.I?.GameContext;
            if (context == null)
            {
                CustomLogger.Error("[EventController] GameContext が null です。");
                return;
            }

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
            currentEventPrefab = Instantiate(data.EventPrefab, canvasObj.transform);
            RefreshView();
        }

        private void OnAddGaugePressed() { logic.TryAddReserved(1); RefreshView(); }
        private void OnRemoveGaugePressed() { logic.TryRemoveReserved(1); RefreshView(); }

        private void OnChallengePressed()
        {
            var result = logic.ChallengeAndApply();

            // GainCard：抽選済みのカードを GameContext に追加
            if (result.ResultType == EventResultType.GainCard)
            {
                foreach (var card in result.DrawnCards)
                    MainManager.I?.GameContext?.AddCard(card);
            }

            SyncGaugeToMainManager();
            eventView.ShowResult(result, OnResultClosed);
            Destroy(currentEventPrefab);
        }

        private void OnCancelPressed() { logic.Cancel(); CompleteEvent(); }
        private void OnResultClosed() => CompleteEvent();

        private void RefreshView()
        {
            eventView.UpdateGaugeInfo(
                logic.CurrentGauge,
                logic.ReservedGauge,
                logic.FinalSuccessRate,
                logic.CanAddReserved(),
                logic.CanRemoveReserved());
        }

        private void SyncGaugeToMainManager()
        {
            var context = MainManager.I?.GameContext;
            if (context == null) return;
            MainManager.I.SetLuckGaugeValue(context.LuckGauge);
            luckGaugeView.UpdateGaugeImmediate(context.LuckGauge, MainManager.I.LuckGaugeMax, true);
        }

        private void CompleteEvent()
        {
            SyncGaugeToMainManager();
            OnEventCompleted?.Invoke();
        }
    }
}