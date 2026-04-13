using System;
using Cysharp.Threading.Tasks;
using TechC.Core.Manager;
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

        private BattleLogic battleLogic;

        public void Initialize()
        {
            battleLogic = new BattleLogic();

            // battleView.Initialize();

            // バトル開始（非同期で回す）
            RunBattleAsync().Forget();
        }

        // -------------------------------------------------------
        // バトルループ
        // -------------------------------------------------------

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
                    break; // とりあえず1ターン目だけでループ抜ける（以降は仮でターン開始アニメだけ再生してる）
                }
                else
                    await battleView.ShowTurnStartAsync(turnData);

                // 2. View に手札・プレイゾーンを表示（アニメーション待ち）
                // await battleView.ShowTurnStartAsync(turnData);

            //     // 3. プレイヤーの入力待ち（ターン確定ボタンが押されるまでブロック）
            //     await battleView.WaitForPlayerConfirmAsync();

            //     // 4. ターン確定：プレイゾーンのカードを左から順に解決
            //     var resolveResults = battleLogic.ConfirmTurn();

            //     foreach (var result in resolveResults)
            //     {
            //         // カード1枚ごとにアニメーションを待つ
            //         await battleView.PlayCardResolveAsync(result);

            //         // 運ゲージ変動があれば更新
            //         if (result.LuckGaugeChanged)
            //             await battleView.UpdateLuckGaugeAsync(battleLogic.LuckGauge);

            //         // HP 変動があれば更新
            //         if (result.DamageDealt > 0)
            //             await battleView.UpdateHpAsync(battleLogic.PlayerHp, battleLogic.EnemyHp);

            //         // 途中で勝敗が確定したらループを抜ける
            //         if (!battleLogic.IsBattleActive) break;
            //     }

            //     // 5. ターン終了処理（激アツゲージ減少など）
            //     battleLogic.EndTurn();

            //     if (battleLogic.IsHotMode)
            //         await battleView.UpdateLuckGaugeAsync(battleLogic.LuckGauge);
            // }

            // // 6. 勝敗演出 → MainManager へ通知
            // if (battleLogic.IsWon)
            // {
            //     await battleView.ShowWinEffectAsync();
            //     OnBattleWon?.Invoke();
            // }
            // else
            // {
            //     await battleView.ShowLoseEffectAsync();
            //     OnBattleLost?.Invoke();
            }
        }

        // -------------------------------------------------------
        // View（UI操作）→ BattleLogic へ橋渡し
        // BattleView の入力コールバックから呼ばれる
        // -------------------------------------------------------

        // public void OnCardSetToUse(int slotIndex, int handIndex)
        //     => battleLogic.SetCardToUse(slotIndex, handIndex);

        // public void OnCardSetToBreak(int slotIndex, int handIndex)
        //     => battleLogic.SetCardToBreak(slotIndex, handIndex);

        // public void OnSpendLuckForProbability(int slotIndex, float amount)
        // {
        //     battleLogic.SpendLuckForProbability(slotIndex, amount);
        //     battleView.UpdateLuckGaugeImmediate(battleLogic.LuckGauge); // 即時反映でOK
        // }

        // public void OnSpendLuckForDamage(int slotIndex, float amount)
        // {
        //     battleLogic.SpendLuckForDamage(slotIndex, amount);
        //     battleView.UpdateLuckGaugeImmediate(battleLogic.LuckGauge);
        // }

        // ターン確定は WaitForPlayerConfirmAsync の完了トリガーなので
        // BattleView 内部で UniTaskCompletionSource を完了させる形にする
        // （BattleView.ConfirmTurn() を BattleView 側のボタンが呼ぶ）

        // -------------------------------------------------------
        // クリーンアップ
        // -------------------------------------------------------

        private void OnDestroy()
        {
            // UniTaskVoid は CancellationToken で止める方が丁寧だが
            // Prefab ごと Destroy される設計なので GameObject 破棄で自然に止まる
        }
    }
}