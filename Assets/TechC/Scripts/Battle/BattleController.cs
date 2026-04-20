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
    /// BattleLogic（純粋C#）と BattleView（表示）を所有し橋渡しする。
    /// アニメーション待ちは UniTask で順序を保証する。
    /// </summary>
    public class BattleController : MonoBehaviour
    {
        // -------------------------------------------------------
        // MainManager へ通知するイベント
        // -------------------------------------------------------
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
            breakZoneView.OnCardBroken += OnCardBroken;

            // バトル開始（非同期で回す）
            RunBattleAsync().Forget();
        }

        /// <summary>
        /// バトル全体を非同期で回すメインループ。
        /// ターン開始 → ユーザー入力待ち → カード解決 → を繰り返す。
        /// </summary>
        private async UniTaskVoid RunBattleAsync()
        {
            battleLogic.StartBattle(MainManager.I?.GameContext);
            bool isFirstTurn = true;

            while (battleLogic.IsBattleActive)
            {
                // 1. ターン開始：ドロー・敵カード配置
                var turnData = battleLogic.BeginTurn();
                // プレイゾーンのカード配置を View に反映
                playZonePresenter.SetupTurn(turnData, turnData.Hand);

                // 1ターン目だけバトル開始演出を再生する
                if (isFirstTurn)
                {
                    await battleView.PlayBattleStartAsync(turnData, MainManager.I?.GameContext.CurrentEnemy);
                    isFirstTurn = false;
                }
                else
                    await battleView.ShowTurnStartAsync(turnData);

                // 3. プレイヤーの入力待ち（ターン確定ボタンが押されるまでブロック）
                await battleView.WaitForPlayerConfirmAsync();

                // 4. ターン確定：プレイゾーンのカードを左から順に解決
                var resolveResults = battleLogic.ConfirmTurn();
                CustomLogger.Info($"カード解決開始: {resolveResults.Count}枚", LogTagUtil.TagBattle);

                foreach (var result in resolveResults)
                {
                    // カード1枚ごとにアニメーションを待つ
                    await battleView.PlayCardResolveAsync(result);

                    if (result.DamageDealt > 0)
                    {
                        if (result.IsPlayer)
                            // プレイヤーが攻撃 → 敵のHPが減る
                            await battleView.UpdateEnemyHpAsync(result.EnemyHpAfter, battleLogic.EnemyHpMax);
                        else
                            // 敵が攻撃 → プレイヤーのHPが減る
                            await battleView.UpdatePlayerHpAsync(result.PlayerHpAfter, battleLogic.PlayerHpMax);
                    }

                    // 途中で勝敗が確定したらループを抜ける
                    if (result.IsBattleEnd)
                    {
                        MainManager.I?.SetLackGaugeValue(battleLogic.LuckGauge); // 最終的なゲージ値を MainManager に渡す
                        // 6. 勝敗演出 → MainManager へ通知
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
                    CustomLogger.Info($"カード「{result.CardInstanceId}」解決完了 (isHit={result.IsHit}, Player={result.IsPlayer})", LogTagUtil.TagBattle);
                    await UniTask.Delay(1000); // 解決結果のログが見やすくなるように1フレーム待つ
                }

                // 5. ターン終了処理
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
        /// カードはゲージの更新支持
        /// </summary>
        /// <param name="cardView">砕いたカード</param>
        /// <param name="luckGain">運ゲー時のチャージ量</param>
        private void OnCardBroken(CardView cardView, float luckGain)
        {
            battleLogic.AddLuckGauge(luckGain);

            battleView.UpdateLuckGaugeAsync(
                battleLogic.LuckGauge,
                battleLogic.LuckGaugeMax,
                battleLogic.IsHotMode
            ).Forget();
        }

        private void OnDestroy()
        {
            if (breakZoneView != null)
                breakZoneView.OnCardBroken -= OnCardBroken;
        }
    }
}