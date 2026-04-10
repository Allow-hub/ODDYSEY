using System.Collections.Generic;
using UnityEngine;

namespace TechC.ODDESEY.Battle
{
    /// <summary>
    /// バトルの表示層（MonoBehaviour）。
    /// 状態・判定は持たず「見せる」だけに徹する。
    /// BattleController からのみ呼ばれる。
    /// </summary>
    public class BattleView : MonoBehaviour
    {
        // -------------------------------------------------------
        // Inspector 設定
        // -------------------------------------------------------

        // [Header("Hand")]
        // [SerializeField] private Transform handContainer;
        // [SerializeField] private GameObject cardViewPrefab;

        // [Header("Play zone")]
        // [SerializeField] private PlayZoneSlotView[] slotViews;  // インスペクタで3〜5スロット分セット

        // [Header("Luck gauge")]
        // [SerializeField] private LuckGaugeView luckGaugeView;

        // [Header("HP")]
        // [SerializeField] private HpBarView playerHpBar;
        // [SerializeField] private HpBarView enemyHpBar;

        // [Header("Effects")]
        // [SerializeField] private GameObject winEffectObj;
        // [SerializeField] private GameObject loseEffectObj;

        // // -------------------------------------------------------
        // // 初期化（BattleController から呼ばれる）
        // // -------------------------------------------------------

        // public void Initialize()
        // {
        //     winEffectObj?.SetActive(false);
        //     loseEffectObj?.SetActive(false);
        // }

        // // -------------------------------------------------------
        // // 手札表示
        // // -------------------------------------------------------

        // public void ShowHand(List<CardData> hand)
        // {
        //     // 既存カードビューをクリア
        //     foreach (Transform child in handContainer)
        //         Destroy(child.gameObject);

        //     foreach (var cardData in hand)
        //     {
        //         var obj  = Instantiate(cardViewPrefab, handContainer);
        //         var view = obj.GetComponent<CardView>();
        //         view.Setup(cardData);
        //         // TODO: ドラッグ操作でプレイゾーンへ配置できるよう CardView に BattleController の参照を渡す
        //     }
        // }

        // // -------------------------------------------------------
        // // プレイゾーン表示
        // // -------------------------------------------------------

        // public void ShowPlayZone(PlayZoneSlot[] slots)
        // {
        //     for (int i = 0; i < slotViews.Length; i++)
        //     {
        //         if (i < slots.Length)
        //             slotViews[i].Setup(slots[i]);
        //         else
        //             slotViews[i].SetEmpty();
        //     }
        // }

        // // -------------------------------------------------------
        // // カード解決アニメーション
        // // -------------------------------------------------------

        // public void PlayCardAnimation(CardResolveResult result)
        // {
        //     // TODO: result に応じたアニメーション（ヒット / ミス / 破壊）
        //     if (result.WasBroken)
        //     {
        //         // 砕くエフェクト
        //     }
        //     else if (result.IsHit)
        //     {
        //         // ダメージエフェクト
        //     }
        //     else
        //     {
        //         // ミスエフェクト
        //     }
        // }

        // // -------------------------------------------------------
        // // 運ゲージ表示
        // // -------------------------------------------------------

        // public void UpdateLuckGauge(float gauge)
        // {
        //     luckGaugeView?.UpdateGauge(gauge);
        // }

        // // -------------------------------------------------------
        // // 勝敗演出
        // // -------------------------------------------------------

        // public void ShowWinEffect()
        // {
        //     winEffectObj?.SetActive(true);
        // }

        // public void ShowLoseEffect()
        // {
        //     loseEffectObj?.SetActive(true);
        // }

        // // -------------------------------------------------------
        // // HP 更新
        // // -------------------------------------------------------

        // public void UpdatePlayerHp(int current, int max)
        // {
        //     playerHpBar?.UpdateHp(current, max);
        // }

        // public void UpdateEnemyHp(int current, int max)
        // {
        //     enemyHpBar?.UpdateHp(current, max);
        // }
    }
}