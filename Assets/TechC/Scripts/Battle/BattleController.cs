using System;
using Cysharp.Threading.Tasks;
using TechC.Core.Manager;
using TechC.ODDESEY.Util;
using TechC.VBattle.Core.Extensions;
using UnityEngine;

namespace TechC.ODDESEY.Battle
{
    /// <summary>
    /// バトル全体の司令塔（MonoBehaviour）。
    ///
    /// 変更点：
    ///   - LuckGaugeSpendRequestEvent を購読。
    ///     PlayZoneView からのゲージ消費要求を BattleLogic に委譲し、
    ///     結果を OnResult コールバックで返す。
    ///   - 消費後に LuckGaugeChangedEvent を発行して LuckGaugeView を即時更新。
    /// </summary>
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
                    await battleView.PlayBattleStartAsync(turnData, MainManager.I?.GameContext.CurrentEnemy);
                    isFirstTurn = false;
                }
                else
                    await battleView.ShowTurnStartAsync(turnData);

                await battleView.WaitForPlayerConfirmAsync();

                var resolveResults = battleLogic.ConfirmTurn();
                CustomLogger.Info($"カード解決開始: {resolveResults.Count}枚", LogTagUtil.TagBattle);

                foreach (var result in resolveResults)
                {
                    await battleView.PlayCardResolveAsync(result);

                    if (result.DamageDealt > 0)
                    {
                        if (result.IsPlayer)
                            await battleView.UpdateEnemyHpAsync(result.EnemyHpAfter, battleLogic.EnemyHpMax);
                        else
                            await battleView.UpdatePlayerHpAsync(result.PlayerHpAfter, battleLogic.PlayerHpMax);
                    }

                    var selfDamage = result.GetExtra<int>(ResultKeys.SelfDamageDealt);
                    if (selfDamage > 0)
                    {
                        if (result.IsPlayer)
                            await battleView.UpdatePlayerHpAsync(result.PlayerHpAfter, battleLogic.PlayerHpMax);
                        else
                            await battleView.UpdateEnemyHpAsync(result.EnemyHpAfter, battleLogic.EnemyHpMax);
                    }

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
                    }

                    CustomLogger.Info($"カード「{result.DamageDealt}」解決完了 (isHit={result.IsHit}, Player={result.IsPlayer})", LogTagUtil.TagBattle);
                    await UniTask.Delay(1000);
                }

                await battleView.RemoveUsedCardsAsync(resolveResults);
                battleLogic.EndTurn();
                await battleView.UpdateLuckGaugeAsync(
                    battleLogic.LuckGauge,
                    battleLogic.LuckGaugeMax,
                    battleLogic.IsHotMode
                );
            }
        }

        /// <summary>
        /// カードが砕かれたとき。ゲージを増やして UI を更新する。
        /// </summary>
        private void OnCardBroken(CardBrokenEvent ev)
        {
            battleLogic.AddLuckGauge(ev.LuckGain);
            PublishLuckGaugeChanged();

            battleView.UpdateLuckGaugeAsync(
                battleLogic.LuckGauge,
                battleLogic.LuckGaugeMax,
                battleLogic.IsHotMode
            ).Forget();
        }

        /// <summary>
        /// PlayZoneView からのゲージ消費要求。
        /// TrySpendLuckGauge() で消費を試みて結果を OnResult で返す。
        /// 成功時は LuckGaugeChangedEvent を発行して UI を即時同期する。
        /// </summary>
        private void OnLuckGaugeSpendRequested(LuckGaugeSpendRequestEvent ev)
        {
            bool success = battleLogic.TrySpendLuckGauge(ev.Cost);
            ev.OnResult?.Invoke(success);

            if (success)
                PublishLuckGaugeChanged();
        }

        /// <summary>
        /// LuckGaugeChangedEvent を発行して全購読者に即時通知する。
        /// </summary>
        private void PublishLuckGaugeChanged()
        {
            BattleEventBus.Publish(new LuckGaugeChangedEvent(
                battleLogic.LuckGauge,
                battleLogic.LuckGaugeMax,
                battleLogic.IsHotMode
            ));
        }

        private void OnDestroy()
        {
            BattleEventBus.Unsubscribe<CardBrokenEvent>(OnCardBroken);
            BattleEventBus.Unsubscribe<LuckGaugeSpendRequestEvent>(OnLuckGaugeSpendRequested);
        }
    }
}