using System;
using Cysharp.Threading.Tasks;
using TechC.Core.Manager;
using TechC.ODDESEY.Core.Manager;
using TechC.ODDESEY.Util;
using TechC.VBattle.Core.Extensions;
using UnityEngine;

namespace TechC.ODDESEY.Battle
{
    public class BattleController : MonoBehaviour
    {
        public event Action OnBattleWon;
        public event Action OnBattleLost;

        [Header("View")]
        [SerializeField] private BattleView battleView;
        [SerializeField] private PlayZonePresenter playZonePresenter;
        [SerializeField] private BreakZoneView breakZoneView;

        private BattleLogic battleLogic;

        public void Initialize()
        {
            battleLogic = new BattleLogic();
            battleView.Init();

            BattleEventBus.Subscribe<CardBrokenEvent>(OnCardBroken);
            BattleEventBus.Subscribe<LuckGaugeSpendRequestEvent>(OnLuckGaugeSpendRequested);
            BattleEventBus.Subscribe<LuckGaugeRefundEvent>(OnLuckGaugeRefunded);

            RunBattleAsync().Forget();
        }

        private async UniTaskVoid RunBattleAsync()
        {
            battleLogic.StartBattle(MainManager.I?.GameContext);
            bool isFirstTurn = true;

            while (battleLogic.IsBattleActive)
            {
                var turnData = battleLogic.BeginTurn();
                playZonePresenter.SetupTurn(turnData, turnData.Hand);

                if (isFirstTurn)
                {
                    await battleView.PlayBattleStartAsync(
                        turnData, MainManager.I?.GameContext.CurrentEnemy);
                    isFirstTurn = false;
                }
                else
                    await battleView.ShowTurnStartAsync(turnData);

                await battleView.WaitForPlayerConfirmAsync();
                // TurnStart アニメが終わるまで待つ
                await battleView.WaitForTurnStartAnimAsync();
                var resolveResults = battleLogic.ConfirmTurn();
                CustomLogger.Info($"カード解決開始: {resolveResults.Count}枚", LogTagUtil.TagBattle);

                bool battleEnded = false;
                foreach (var result in resolveResults)
                {
                    await battleView.PlayCardResolveAsync(
                        result, battleLogic.PlayerHpMax, battleLogic.EnemyHpMax);

                    if (result.IsBattleEnd)
                    {
                        MainManager.I?.SetLackGaugeValue(battleLogic.LuckGauge);
                        if (result.IsWon)
                        {
                            await battleView.ShowWinEffectAsync();
                            OnBattleWon?.Invoke();
                        }
                        else
                        {
                            await battleView.ShowLoseEffectAsync();
                            OnBattleLost?.Invoke();
                        }
                        battleEnded = true;
                        break;
                    }

                    CustomLogger.Info(
                        $"カード解決完了: damage={result.DamageDealt} isHit={result.IsHit} IsPlayer={result.IsPlayer}",
                        LogTagUtil.TagBattle);

                    // カード間インターバル（固定値でなく BattleView の設定値を使う）
                    await UniTask.Delay(
                        TimeSpan.FromSeconds(battleView.CardResolveInterval),
                        ignoreTimeScale: true);
                }

                if (battleEnded) break;

                await battleView.RemoveUsedCardsAsync(resolveResults);
                CameraManager.I.SwitchTo(CameraState.Default);
                battleLogic.EndTurn();
                await battleView.UpdateLuckGaugeAsync(
                    battleLogic.LuckGauge,
                    battleLogic.LuckGaugeMax,
                    battleLogic.IsHotMode);
            }
        }

        private void OnCardBroken(CardBrokenEvent ev)
        {
            battleLogic.AddLuckGauge(ev.LuckGain);
            PublishLuckGaugeChanged();
            battleView.UpdateLuckGaugeAsync(
                battleLogic.LuckGauge,
                battleLogic.LuckGaugeMax,
                battleLogic.IsHotMode).Forget();
        }

        private void OnLuckGaugeSpendRequested(LuckGaugeSpendRequestEvent ev)
        {
            bool success = battleLogic.TrySpendLuckGauge(ev.Cost);
            ev.OnResult?.Invoke(success);
            if (success) PublishLuckGaugeChanged();
        }

        private void PublishLuckGaugeChanged()
        {
            BattleEventBus.Publish(new LuckGaugeChangedEvent(
                battleLogic.LuckGauge,
                battleLogic.LuckGaugeMax,
                battleLogic.IsHotMode));
        }

        private void OnLuckGaugeRefunded(LuckGaugeRefundEvent ev)
        {
            battleLogic.AddLuckGauge(ev.Amount);
            PublishLuckGaugeChanged();
        }

        private void OnDestroy()
        {
            BattleEventBus.Unsubscribe<CardBrokenEvent>(OnCardBroken);
            BattleEventBus.Unsubscribe<LuckGaugeSpendRequestEvent>(OnLuckGaugeSpendRequested);
            BattleEventBus.Unsubscribe<LuckGaugeRefundEvent>(OnLuckGaugeRefunded);
        }
    }
}