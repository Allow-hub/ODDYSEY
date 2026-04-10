using System;
using System.Collections.Generic;

namespace TechC.ODDESEY.Battle
{
    /// <summary>
    /// バトルのロジック層。MonoBehaviour に依存しない純粋 C#。
    /// 状態管理・判定・計算を担当し、結果を Action で BattleController に通知する。
    /// </summary>
    public class BattleLogic
    {
        // // -------------------------------------------------------
        // // BattleController へ通知するイベント
        // // -------------------------------------------------------

        // public event Action<TurnData>          OnTurnStarted;
        // public event Action<CardResolveResult> OnCardResolved;
        // public event Action<float>             OnLuckGaugeChanged;
        // public event Action                    OnBattleWon;
        // public event Action                    OnBattleLost;

        // // -------------------------------------------------------
        // // 状態
        // // -------------------------------------------------------

        // private LuckGaugeModel luckGauge;
        // private List<CardData> deck;
        // private List<CardData> hand;
        // private List<CardData> discardPile;
        // private PlayZoneSlot[] playZone;

        // private int  playerHp;
        // private int  enemyHp;
        // private bool isBattleActive;

        // // -------------------------------------------------------
        // // 定数（暫定値：デバッグで調整）
        // // -------------------------------------------------------

        // private const int HandLimit    = 5;
        // private const int PlayZoneSize = 4;
        // private const int PlayerInitHp = 30;

        // // -------------------------------------------------------
        // // 初期化・開始
        // // -------------------------------------------------------

        // public void StartBattle()
        // {
        //     // playerHp      = PlayerInitHp;
        //     // luckGauge     = new LuckGaugeModel();
        //     // deck          = new List<CardData>();
        //     // hand          = new List<CardData>();
        //     // discardPile   = new List<CardData>();
        //     // playZone      = new PlayZoneSlot[PlayZoneSize];
        //     isBattleActive = true;

        //     // TODO: 敵データをセット

        //     StartTurn();
        // }

        // // -------------------------------------------------------
        // // ターン開始
        // // -------------------------------------------------------

        // private void StartTurn()
        // {
        //     DrawToFull();
        //     PlaceEnemyCards();

        //     var turnData = new TurnData
        //     {
        //         Hand     = new List<CardData>(hand),
        //         PlayZone = playZone,
        //     };

        //     OnTurnStarted?.Invoke(turnData);
        // }

        // // -------------------------------------------------------
        // // カード状態セット
        // // -------------------------------------------------------

        // public void SetCardToUse(int slotIndex, int handIndex)
        // {
        //     // TODO
        // }

        // public void SetCardToBreak(int slotIndex, int handIndex)
        // {
        //     // TODO
        // }

        // // -------------------------------------------------------
        // // 運ゲージ消費
        // // -------------------------------------------------------

        // public void SpendLuckForProbability(int slotIndex, float amount)
        // {
        //     if (!luckGauge.TrySpend(amount)) return;

        //     // TODO
        //     OnLuckGaugeChanged?.Invoke(luckGauge.Current);
        // }

        // public void SpendLuckForDamage(int slotIndex, float amount)
        // {
        //     if (!luckGauge.TrySpend(amount)) return;

        //     // TODO
        //     OnLuckGaugeChanged?.Invoke(luckGauge.Current);
        // }

        // // -------------------------------------------------------
        // // ターン確定・カード解決
        // // -------------------------------------------------------

        // public void ConfirmTurn()
        // {
        //     for (int i = 0; i < playZone.Length; i++)
        //     {
        //         var slot = playZone[i];
        //         if (slot == null || slot.Card == null) continue;

        //         var result = ResolveSlot(slot);
        //         OnCardResolved?.Invoke(result);

        //         if (!isBattleActive) return;
        //     }

        //     EndTurn();
        // }

        // private CardResolveResult ResolveSlot(PlayZoneSlot slot)
        // {
        //     var result = new CardResolveResult { Slot = slot };

        //     if (slot.IsEnemyCard)
        //     {
        //         // TODO
        //     }
        //     else if (slot.State == CardState.Use)
        //     {
        //         // TODO
        //     }
        //     else if (slot.State == CardState.Break)
        //     {
        //         float gain = slot.Card.LuckConversionRate;
        //         luckGauge.Add(gain);
        //         OnLuckGaugeChanged?.Invoke(luckGauge.Current);
        //         result.WasBroken = true;
        //     }

        //     return result;
        // }

        // // -------------------------------------------------------
        // // ターン終了処理
        // // -------------------------------------------------------

        // private void EndTurn()
        // {
        //     if (luckGauge.IsHotMode)
        //     {
        //         luckGauge.TickDown();
        //         OnLuckGaugeChanged?.Invoke(luckGauge.Current);
        //     }

        //     discardPile.AddRange(hand);
        //     hand.Clear();

        //     playZone = new PlayZoneSlot[PlayZoneSize];

        //     CheckBattleEnd();

        //     if (isBattleActive)
        //         StartTurn();
        // }

        // private void CheckBattleEnd()
        // {
        //     if (enemyHp <= 0)
        //     {
        //         isBattleActive = false;
        //         OnBattleWon?.Invoke();
        //     }
        //     else if (playerHp <= 0)
        //     {
        //         isBattleActive = false;
        //         OnBattleLost?.Invoke();
        //     }
        // }

        // // -------------------------------------------------------
        // // ドロー
        // // -------------------------------------------------------

        // private void DrawToFull()
        // {
        //     while (hand.Count < HandLimit && (deck.Count > 0 || discardPile.Count > 0))
        //     {
        //         if (deck.Count == 0)
        //             ShuffleDiscardToDeck();

        //         if (deck.Count == 0) break;

        //         var card = deck[0];
        //         deck.RemoveAt(0);

        //         card.RollBaseValues();
        //         hand.Add(card);
        //     }
        // }

        // private void ShuffleDiscardToDeck()
        // {
        //     deck.AddRange(discardPile);
        //     discardPile.Clear();
        //     // TODO: シャッフル
        // }

        // private void PlaceEnemyCards()
        // {
        //     // TODO
        // }
    }
}