using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace TechC.ODDESEY.Battle
{
    /// <summary>
    /// プレイゾーン全体を管理する View クラス。
    ///
    /// 変更点：
    ///   - 確率ダウン（downPercentageButton）は BonusProbability > 0 の分しか戻せない。
    ///     戻した分のゲージを返還する（SpendではなくRefund）。
    ///   - 全ボタンに長押し対応を追加。押し続けると一定間隔で繰り返し発動する。
    /// </summary>
    public class PlayZoneView : MonoBehaviour
    {
        [Header("操作エリア")]
        [SerializeField] private Button upPercentageButton;
        [SerializeField] private Button downPercentageButton;
        [SerializeField] private TextMeshProUGUI percentageText;
        [SerializeField] private Button upDamageButton;
        [SerializeField] private Button downDamageButton;
        [SerializeField] private TextMeshProUGUI damageText;

        [Header("運ゲージ消費コスト")]
        [SerializeField] private float percentageAdjustCost = 1f;
        [SerializeField] private float damageAdjustCost = 5f;

        [Header("長押し設定")]
        [SerializeField] private float holdDelay = 0.5f;      // 長押し認識までの時間（秒）
        [SerializeField] private float holdInterval = 0.1f;   // 繰り返し間隔（秒）

        private CardInstance currentCardInstance;

        private void OnEnable()
        {
            BattleEventBus.Subscribe<CardPlacedClickedEvent>(OnCardPlacedClicked);
        }

        private void OnDisable()
        {
            BattleEventBus.Unsubscribe<CardPlacedClickedEvent>(OnCardPlacedClicked);
        }

        private void Start()
        {
            RegisterHoldButton(upPercentageButton, () => AdjustPercentageUp());
            RegisterHoldButton(downPercentageButton, () => AdjustPercentageDown());
            RegisterHoldButton(upDamageButton, () => AdjustDamage(1));
            RegisterHoldButton(downDamageButton, () => AdjustDamage(-1));

            SetButtonsInteractable(false);
        }

        // ──────────────────────────────────────────────────────────
        // 長押し登録
        // ──────────────────────────────────────────────────────────

        /// <summary>
        /// ボタンにクリック＋長押しの両方を登録する。
        /// EventTrigger で PointerDown / PointerUp を捕まえ、
        /// 長押し中は Coroutine で action を繰り返す。
        /// </summary>
        private void RegisterHoldButton(Button button, System.Action action)
        {
            var trigger = button.gameObject.GetComponent<EventTrigger>()
                       ?? button.gameObject.AddComponent<EventTrigger>();

            // PointerDown → コルーチン開始
            var down = new EventTrigger.Entry { eventID = EventTriggerType.PointerDown };
            down.callback.AddListener(_ =>
            {
                if (button.interactable)
                    StartCoroutine(HoldCoroutine(button, action));
            });
            trigger.triggers.Add(down);

            // PointerUp → コルーチン停止
            var up = new EventTrigger.Entry { eventID = EventTriggerType.PointerUp };
            up.callback.AddListener(_ => StopCoroutine(HoldCoroutine(button, action)));
            trigger.triggers.Add(up);

            // PointerExit でも停止（ドラッグで外れたとき）
            var exit = new EventTrigger.Entry { eventID = EventTriggerType.PointerExit };
            exit.callback.AddListener(_ => StopAllCoroutinesOnButton(button));
            trigger.triggers.Add(exit);
        }

        /// <summary>
        /// 長押しコルーチン。
        /// holdDelay 後に holdInterval ごとに action を繰り返す。
        /// 最初の1回（クリック）はここで行う（onClick は使わない）。
        /// </summary>
        private IEnumerator HoldCoroutine(Button button, System.Action action)
        {
            // 1回目（クリック相当）
            action();

            // holdDelay 待機
            yield return new WaitForSeconds(holdDelay);

            // 長押し繰り返し
            while (button.interactable)
            {
                action();
                yield return new WaitForSeconds(holdInterval);
            }
        }

        /// <summary>
        /// ボタンに紐づいたコルーチンを全停止する。
        /// PointerUp / PointerExit から呼ぶ。
        /// </summary>
        private void StopAllCoroutinesOnButton(Button button)
        {
            // MonoBehaviour の StopAllCoroutines は PlayZoneView 上の全コルーチンを止めるため、
            // タグ付きコルーチン管理に切り替える
            StopAllCoroutines();
        }

        // ──────────────────────────────────────────────────────────
        // イベント受信
        // ──────────────────────────────────────────────────────────

        private void OnCardPlacedClicked(CardPlacedClickedEvent evt)
        {
            currentCardInstance = evt.Card.CardInstance;
            RefreshDisplay();
            SetButtonsInteractable(true);
        }

        // ──────────────────────────────────────────────────────────
        // 操作
        // ──────────────────────────────────────────────────────────

        /// <summary>
        /// 確率を上げる。ゲージを消費する。
        /// </summary>
        private void AdjustPercentageUp()
        {
            if (currentCardInstance == null) return;

            BattleEventBus.Publish(new LuckGaugeSpendRequestEvent(
                cost: percentageAdjustCost,
                onResult: success =>
                {
                    if (!success) return;
                    currentCardInstance.AddBonusProbability(0, 1f / 100f);
                    RefreshDisplay();
                }
            ));
        }

        /// <summary>
        /// 確率を下げる。
        /// 下げられるのは AddBonusProbability で増やした分（BonusProbability > 0）のみ。
        /// 戻した分のゲージを返還する。
        /// </summary>
        private void AdjustPercentageDown()
        {
            if (currentCardInstance == null) return;

            float bonus = currentCardInstance.GetBonusProbability(0);
            if (bonus <= 0f) return; // 増やした分がなければ何もしない

            // 1% 分だけ戻す（bonus が 1% 未満なら bonus 全部を戻す）
            float revertAmount = Mathf.Min(1f / 100f, bonus);

            currentCardInstance.AddBonusProbability(0, -revertAmount);

            // 戻した分のゲージを返還
            BattleEventBus.Publish(new LuckGaugeRefundEvent(
                amount: percentageAdjustCost * (revertAmount / (1f / 100f))
            ));

            RefreshDisplay();
        }

        /// <summary>
        /// ダメージを増減する。増加はゲージ消費、減少は増やした分のみ戻せてゲージ返還。
        /// </summary>
        private void AdjustDamage(int delta)
        {
            if (currentCardInstance == null) return;

            if (delta > 0)
            {
                // 増加：ゲージ消費
                BattleEventBus.Publish(new LuckGaugeSpendRequestEvent(
                    cost: damageAdjustCost,
                    onResult: success =>
                    {
                        if (!success) return;
                        currentCardInstance.AddBonusValue(0, delta);
                        RefreshDisplay();
                    }
                ));
            }
            else
            {
                // 減少：BonusValue > 0 の分だけ戻せる、ゲージ返還
                int bonus = currentCardInstance.GetBonusValue(0);
                if (bonus <= 0) return;

                int revertAmount = Mathf.Min(-delta, bonus); // 戻す量（正値）
                currentCardInstance.AddBonusValue(0, -revertAmount);

                BattleEventBus.Publish(new LuckGaugeRefundEvent(
                    amount: damageAdjustCost * revertAmount
                ));

                RefreshDisplay();
            }
        }

        // ──────────────────────────────────────────────────────────
        // 表示更新
        // ──────────────────────────────────────────────────────────

        private void RefreshDisplay()
        {
            if (currentCardInstance == null) return;
            int probability = (int)(currentCardInstance.GetEffectiveProbability(0) * 100);
            int damage = currentCardInstance.GetEffectiveValue(0);
            percentageText.text = $"{probability}";
            damageText.text = damage.ToString();
        }

        private void SetButtonsInteractable(bool interactable)
        {
            upPercentageButton.interactable = interactable;
            downPercentageButton.interactable = interactable;
            upDamageButton.interactable = interactable;
            downDamageButton.interactable = interactable;
        }
    }
}