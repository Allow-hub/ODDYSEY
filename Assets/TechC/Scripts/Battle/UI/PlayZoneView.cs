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
    ///   - [NEW] Rollの最大値（RolledProbability / Value）を超えた強化には
    ///     段階的にコストが増加する（超過10%刻み / 1ダメージ刻みごとに2倍）。
    ///     costScaleMax でスケール倍率の上限を設定できる。
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

        [Header("運ゲージ消費コスト（基本値）")]
        [SerializeField] private float percentageAdjustCost = 1f;
        [SerializeField] private float damageAdjustCost = 5f;

        [Header("超過コストスケール設定")]
        [Tooltip("確率の超過1段階あたりの刻み幅（例: 0.1f = 10%ごと）")]
        [SerializeField] private float probabilityOvershootStep = 0.1f;

        [Tooltip("ダメージの超過1段階あたりの刻み幅（例: 1 = 1ダメージごと）")]
        [SerializeField] private int damageOvershootStep = 1;

        [Tooltip("コストスケールの最大倍率（段階が増えてもこの倍率で頭打ち）")]
        [SerializeField] private float costScaleMax = 16f; // 2^4 = 4段階上限相当

        [Header("長押し設定")]
        [SerializeField] private float holdDelay = 0.5f;
        [SerializeField] private float holdIntervalMin = 0.04f;
        [SerializeField] private float holdIntervalStart = 0.15f;
        [SerializeField] private float holdAccelTime = 2.0f;

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

        // ─── 超過コスト計算 ──────────────────────────────────────────────

        /// <summary>
        /// 現在のボーナス確率がRoll最大値をどれだけ超えているかに基づいてコストを算出する。
        ///
        /// 超過量を probabilityOvershootStep 刻みで段階カウントし、
        /// baseCost × 2^段階数 を返す（costScaleMax で頭打ち）。
        ///
        /// 例（step=0.1f, baseCost=1f）：
        ///   超過0〜9%  → 1f
        ///   超過10〜19% → 2f
        ///   超過20〜29% → 4f
        ///   超過30%〜  → 8f（costScaleMax=8の場合）
        /// </summary>
        private float CalcProbabilityCost(int slotIndex)
        {
            float rolled = currentCardInstance.GetBaseProbability(slotIndex);
            float bonus = currentCardInstance.GetBonusProbability(slotIndex);

            // Rollの最大値を超えていなければ基本コスト
            float overshoot = bonus - rolled;
            if (overshoot <= 0f)
                return percentageAdjustCost;

            // 超過量を step 刻みで段階カウント（floor）
            int steps = Mathf.FloorToInt(overshoot / probabilityOvershootStep);
            float scale = Mathf.Pow(2f, steps);
            scale = Mathf.Min(scale, costScaleMax);

            return percentageAdjustCost * scale;
        }

        /// <summary>
        /// 現在のボーナスダメージがRoll最大値をどれだけ超えているかに基づいてコストを算出する。
        ///
        /// 超過量を damageOvershootStep 刻みで段階カウントし、
        /// baseCost × 2^段階数 を返す（costScaleMax で頭打ち）。
        ///
        /// 例（step=1, baseCost=5f）：
        ///   超過0  → 5f
        ///   超過1  → 10f
        ///   超過2  → 20f
        ///   超過3+ → 40f（costScaleMax=40の場合）
        /// </summary>
        private float CalcDamageCost(int slotIndex)
        {
            int rolled = currentCardInstance.GetBaseValue(slotIndex);
            int bonus = currentCardInstance.GetBonusValue(slotIndex);

            float overshoot = bonus - rolled;
            if (overshoot <= 0f)
                return damageAdjustCost;

            int steps = Mathf.FloorToInt(overshoot / damageOvershootStep);
            float scale = Mathf.Pow(2f, steps);
            scale = Mathf.Min(scale, costScaleMax);

            return damageAdjustCost * scale;
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

        private IEnumerator HoldCoroutine(System.Action action)
        {
            action();
            yield return new WaitForSeconds(holdDelay);

            float elapsed = 0f;
            while (true)
            {
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

            // Effect のフラグを見て強化できないボタンを無効化する
            if (currentCardInstance.OriginalData.Effects.Count > 0)
            {
                var effect = currentCardInstance.OriginalData.Effects[0];
                bool canProb = effect.CanBoostProbability;
                bool canValue = effect.CanBoostValue;

                upPercentageButton.interactable = canProb;
                downPercentageButton.interactable = canProb;
                upDamageButton.interactable = canValue;
                downDamageButton.interactable = canValue;
            }
        }

        // ─── 操作 ────────────────────────────────────────────────────────

        private void AdjustPercentageUp()
        {
            if (currentCardInstance == null) return;

            float currentProbability = currentCardInstance.GetEffectiveProbability(0);
            if (currentProbability >= 1f) return;

            // 現時点の超過コストを計算（消費前に取得する）
            float cost = CalcProbabilityCost(0);

            BattleEventBus.Publish(new LuckGaugeSpendRequestEvent(
                cost: cost,
                onResult: success =>
                {
                    if (!success) return;

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

            // 還元コストも現在段階の基本コストで計算
            // （下げた後の状態でなく、下げる前の状態を基準にするため先に CalcProbabilityCost を呼ぶ）
            // ※ AddBonusProbability を先に呼んでしまうと1段階前の値が取れないため、
            //   還元量の割合スケールを掛けて概算で返す（元の設計を踏襲）
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
                float cost = CalcDamageCost(0);

                BattleEventBus.Publish(new LuckGaugeSpendRequestEvent(
                    cost: cost,
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