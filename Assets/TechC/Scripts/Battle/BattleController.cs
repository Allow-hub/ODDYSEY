using System;
using UnityEngine;

namespace TechC.ODDESEY.Battle
{
    /// <summary>
    /// バトル全体の司令塔（MonoBehaviour）。
    /// BattleLogic（純粋C#）と BattleView（表示）を所有し、橋渡しする。
    /// </summary>
    public class BattleController : MonoBehaviour
    {
        // -------------------------------------------------------
        // MainManager へ通知するイベント
        // -------------------------------------------------------

        public event Action OnBattleWon;
        public event Action OnBattleLost;

        // -------------------------------------------------------
        // Inspector 設定
        // -------------------------------------------------------

        [Header("View")]
        [SerializeField] private BattleView battleView;

        // -------------------------------------------------------
        // 内部：ロジック
        // -------------------------------------------------------

        private BattleLogic _battleLogic;

        // -------------------------------------------------------
        // 初期化（MainManager から呼ばれる）
        // -------------------------------------------------------

        public void Initialize()
        {
            // _battleLogic = new BattleLogic();

            // // BattleLogic → BattleController へのコールバック購読
            // _battleLogic.OnTurnStarted       += HandleTurnStarted;
            // _battleLogic.OnCardResolved      += HandleCardResolved;
            // _battleLogic.OnLuckGaugeChanged  += HandleLuckGaugeChanged;
            // _battleLogic.OnBattleWon         += HandleBattleWon;
            // _battleLogic.OnBattleLost        += HandleBattleLost;

            // battleView.Initialize();

            // _battleLogic.StartBattle();
        }

        // -------------------------------------------------------
        // BattleLogic コールバックハンドラ → View に橋渡し
        // -------------------------------------------------------

        // private void HandleTurnStarted(TurnData turnData)
        // {
        //     battleView.ShowHand(turnData.Hand);
        //     battleView.ShowPlayZone(turnData.PlayZone);
        // }

        // private void HandleCardResolved(CardResolveResult result)
        // {
        //     battleView.PlayCardAnimation(result);
        // }

        // private void HandleLuckGaugeChanged(float gauge)
        // {
        //     battleView.UpdateLuckGauge(gauge);
        // }

        // private void HandleBattleWon()
        // {
        //     battleView.ShowWinEffect();
        //     OnBattleWon?.Invoke();
        // }

        // private void HandleBattleLost()
        // {
        //     battleView.ShowLoseEffect();
        //     OnBattleLost?.Invoke();
        // }

        // // -------------------------------------------------------
        // // View（UI操作）→ BattleLogic へ橋渡し
        // // -------------------------------------------------------

        // /// <summary>カードを「使用」状態にセット（UIから呼ぶ）</summary>
        // public void OnCardSetToUse(int slotIndex, int handIndex)
        // {
        //     _battleLogic.SetCardToUse(slotIndex, handIndex);
        // }

        // /// <summary>カードを「破壊」状態にセット（UIから呼ぶ）</summary>
        // public void OnCardSetToBreak(int slotIndex, int handIndex)
        // {
        //     _battleLogic.SetCardToBreak(slotIndex, handIndex);
        // }

        // /// <summary>運ゲージを確率強化に消費（UIから呼ぶ）</summary>
        // public void OnSpendLuckForProbability(int slotIndex, float amount)
        // {
        //     _battleLogic.SpendLuckForProbability(slotIndex, amount);
        // }

        // /// <summary>運ゲージをダメージ強化に消費（UIから呼ぶ）</summary>
        // public void OnSpendLuckForDamage(int slotIndex, float amount)
        // {
        //     _battleLogic.SpendLuckForDamage(slotIndex, amount);
        // }

        // /// <summary>ターン確定ボタン（UIから呼ぶ）</summary>
        // public void OnConfirmTurn()
        // {
        //     _battleLogic.ConfirmTurn();
        // }

        // // -------------------------------------------------------
        // // クリーンアップ
        // // -------------------------------------------------------

        // private void OnDestroy()
        // {
        //     if (_battleLogic == null) return;

        //     _battleLogic.OnTurnStarted      -= HandleTurnStarted;
        //     _battleLogic.OnCardResolved     -= HandleCardResolved;
        //     _battleLogic.OnLuckGaugeChanged -= HandleLuckGaugeChanged;
        //     _battleLogic.OnBattleWon        -= HandleBattleWon;
        //     _battleLogic.OnBattleLost       -= HandleBattleLost;
        // }
    }
}