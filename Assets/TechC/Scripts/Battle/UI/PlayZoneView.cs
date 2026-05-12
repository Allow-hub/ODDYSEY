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
    ///   - 確率上昇時に 100% を超えないよう上限チェックを追加。
    ///   - 長押し時間が長いほど加速するアダプティブ速度を実装。
    ///     holdDelay 経過後、holdAccelInterval ごとに interval を短縮する。
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
        [SerializeField] private float holdDelay = 0.5f;  // 長押し認識までの時間（秒）
        [SerializeField] private float holdIntervalMin = 0.04f; // 加速後の最小間隔（秒）
        [SerializeField] private float holdIntervalStart = 0.15f; // 長押し開始直後の間隔（秒）
        [SerializeField] private float holdAccelTime = 2.0f;  // この秒数かけて最大速度に到達

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

        // ─── 長押し登録 ──────────────────────────────────────────────────

        private void RegisterHoldButton(Button button, System.Action action)
        {
            var trigger = button.gameObject.GetComponent<EventTrigger>()
                       ?? button.gameObject.AddComponent<EventTrigger>();
            trigger.triggers.Clear();

            var down = new EventTrigger.Entry { eventID = EventTriggerType.PointerDown };
            down.callback.AddListener(_ =>
            {
                if (button.interactable)
                    StartCoroutine(HoldCoroutine(action));
            });
            trigger.triggers.Add(down);

            var up = new EventTrigger.Entry { eventID = EventTriggerType.PointerUp };
            up.callback.AddListener(_ => StopAllCoroutines());
            trigger.triggers.Add(up);

            var exit = new EventTrigger.Entry { eventID = EventTriggerType.PointerExit };
            exit.callback.AddListener(_ => StopAllCoroutines());
            trigger.triggers.Add(exit);
        }

        /// <summary>
        /// 長押しコルーチン。長押し時間が長いほど発火間隔を短縮して加速する。
        ///
        /// holdDelay   : 長押し認識までの待機
        /// holdIntervalStart : 長押し開始直後の間隔
        /// holdIntervalMin   : 加速後の最小間隔
        /// holdAccelTime     : holdIntervalStart → holdIntervalMin に到達するまでの時間
        /// </summary>
        private IEnumerator HoldCoroutine(System.Action action)
        {
            // 1回目（クリック相当）
            action();

            // 長押し認識待ち
            yield return new WaitForSeconds(holdDelay);

            float elapsed = 0f;

            while (true)
            {
                // 経過時間に応じて間隔を線形補間で短縮
                float t = Mathf.Clamp01(elapsed / holdAccelTime);
                float interval = Mathf.Lerp(holdIntervalStart, holdIntervalMin, t);

                action();
                yield return new WaitForSeconds(interval);

                elapsed += interval;
            }
        }

        // ─── イベント受信 ────────────────────────────────────────────────

        private void OnCardPlacedClicked(CardPlacedClickedEvent evt)
        {
            currentCardInstance = evt.Card.CardInstance;
            RefreshDisplay();
            SetButtonsInteractable(true);
        }

        // ─── 操作 ────────────────────────────────────────────────────────

        private void AdjustPercentageUp()
        {
            if (currentCardInstance == null) return;

            // 現在の実効確率が 100% 以上なら上げない
            float currentProbability = currentCardInstance.GetEffectiveProbability(0);
            if (currentProbability >= 1f) return;

            BattleEventBus.Publish(new LuckGaugeSpendRequestEvent(
                cost: percentageAdjustCost,
                onResult: success =>
                {
                    if (!success) return;

                    // 上昇後が 100% を超えないようにクランプ
                    float addAmount = 1f / 100f;
                    float afterProb = currentCardInstance.GetEffectiveProbability(0) + addAmount;
                    if (afterProb > 1f)
                        addAmount = 1f - currentCardInstance.GetEffectiveProbability(0);

                    if (addAmount <= 0f) return;

                    currentCardInstance.AddBonusProbability(0, addAmount);
                    RefreshDisplay();
                }
            ));
        }

        private void AdjustPercentageDown()
        {
            if (currentCardInstance == null) return;

            float bonus = currentCardInstance.GetBonusProbability(0);
            if (bonus <= 0f) return;

            float revertAmount = Mathf.Min(1f / 100f, bonus);
            currentCardInstance.AddBonusProbability(0, -revertAmount);

            BattleEventBus.Publish(new LuckGaugeRefundEvent(
                amount: percentageAdjustCost * (revertAmount / (1f / 100f))
            ));

            RefreshDisplay();
        }

        private void AdjustDamage(int delta)
        {
            if (currentCardInstance == null) return;

            if (delta > 0)
            {
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
                int bonus = currentCardInstance.GetBonusValue(0);
                if (bonus <= 0) return;

                int revertAmount = Mathf.Min(-delta, bonus);
                currentCardInstance.AddBonusValue(0, -revertAmount);

                BattleEventBus.Publish(new LuckGaugeRefundEvent(
                    amount: damageAdjustCost * revertAmount
                ));

                RefreshDisplay();
            }
        }

        // ─── 表示更新 ────────────────────────────────────────────────────

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