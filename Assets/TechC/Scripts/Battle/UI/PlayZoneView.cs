using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace TechC.ODDESEY.Battle
{
    public class PlayZoneView : MonoBehaviour
    {
        [Header("操作エリア")]
        [SerializeField] private Button upPercentageButton;
        [SerializeField] private Button downPercentageButton;
        [SerializeField] private TextMeshProUGUI percentageText;
        [SerializeField] private Button upDamageButton;
        [SerializeField] private Button downDamageButton;
        [SerializeField] private TextMeshProUGUI damageText;

        [Header("限界突破コスト設定（確率）")]
        [Tooltip("カード上限まで：確率+1%あたりのゲージコスト")]
        [SerializeField] private float probCostNormal = 1f;
        [Tooltip("上限突破 +1〜10%：確率+1%あたりのゲージコスト")]
        [SerializeField] private float probCostBreak1 = 2f;
        [Tooltip("上限突破 +11%以上：確率+1%あたりのゲージコスト")]
        [SerializeField] private float probCostBreak2 = 3f;

        [Header("限界突破コスト設定（ダメージ・シールド）")]
        [Tooltip("カード上限まで：+1あたりのゲージコスト")]
        [SerializeField] private float valueCostNormal = 5f;
        [Tooltip("上限突破 +1〜5：+1あたりのゲージコスト")]
        [SerializeField] private float valueCostBreak1 = 10f;
        [Tooltip("上限突破 +6以上：+1あたりのゲージコスト")]
        [SerializeField] private float valueCostBreak2 = 15f;

        [Header("長押し設定")]
        [SerializeField] private float holdDelay = 0.5f;
        [SerializeField] private float holdIntervalMin = 0.04f;
        [SerializeField] private float holdIntervalStart = 0.15f;
        [SerializeField] private float holdAccelTime = 2.0f;

        private int limitBreakCostLevelBonus = 0;
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

        // ─── コスト計算 ──────────────────────────────────────────────────

        /// <summary>
        /// 現在の確率コスト段階を返す。
        /// 0 = カード上限まで, 1 = +1〜10%, 2 = +11%以上
        /// </summary>
        private int GetProbabilityCostLevel(int slotIndex)
        {
            float rolledMax = currentCardInstance.GetBaseProbability(slotIndex);
            float effective = currentCardInstance.GetEffectiveProbability(slotIndex);
            float overshoot = effective - rolledMax;

            if (overshoot <= 0f) return 0;
            if (overshoot < 0.10f) return 1;
            return 2;
        }

        private float GetProbabilityCostByLevel(int level)
        {
            return level switch
            {
                0 => probCostNormal,
                1 => probCostBreak1,
                2 => probCostBreak2,
                3 => probCostBreak2 + 1f,
                4 => probCostBreak2 + 2f,
                _ => probCostBreak2 + (level - 2) * 1f,
            };
        }

        private float CalcProbabilityCost(int slotIndex)
        {
            int level = GetProbabilityCostLevel(slotIndex);
            if (level > 0)
                level += limitBreakCostLevelBonus;

            return GetProbabilityCostByLevel(level);
        }

        /// <summary>
        /// 現在のダメージコスト段階を返す。
        /// 0 = カード上限まで, 1 = +1〜5, 2 = +6以上
        /// </summary>
        private int GetDamageCostLevel(int slotIndex)
        {
            int rolledMax = currentCardInstance.GetBaseValue(slotIndex);
            int effective = currentCardInstance.GetEffectiveValue(slotIndex);
            int overshoot = effective - rolledMax;

            if (overshoot <= 0) return 0;
            if (overshoot <= 5) return 1;
            return 2;
        }

        private float GetDamageCostByLevel(int level)
        {
            return level switch
            {
                0 => valueCostNormal,
                1 => valueCostBreak1,
                2 => valueCostBreak2,
                _ => valueCostBreak2 + (level - 2) * 5f,
            };
        }

        private float CalcDamageCost(int slotIndex)
        {
            int level = GetDamageCostLevel(slotIndex);
            if (level > 0)
                level += limitBreakCostLevelBonus;

            return GetDamageCostByLevel(level);
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

            // Effect のフラグを見て強化できないボタンを無効化
            if (currentCardInstance.OriginalData.Effects.Count > 0)
            {
                var effect = currentCardInstance.OriginalData.Effects[0];
                upPercentageButton.interactable = effect.CanBoostProbability;
                downPercentageButton.interactable = effect.CanBoostProbability;
                upDamageButton.interactable = effect.CanBoostValue;
                downDamageButton.interactable = effect.CanBoostValue;
            }
        }

        // ─── 操作 ────────────────────────────────────────────────────────

        private void AdjustPercentageUp()
        {
            if (currentCardInstance == null) return;
            if (currentCardInstance.GetEffectiveProbability(0) >= 1f) return;

            float cost = CalcProbabilityCost(0); // 上昇前のコスト段階で消費

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

            // 下げる前にコスト段階を確定する（下げた後に計算すると段階がずれる）
            float refundCost = CalcProbabilityCost(0);
            float revertAmount = Mathf.Min(1f / 100f, bonus);

            currentCardInstance.AddBonusProbability(0, -revertAmount);

            BattleEventBus.Publish(new LuckGaugeRefundEvent(
                amount: refundCost * (revertAmount / (1f / 100f))
            ));

            RefreshDisplay();
        }

        private void AdjustDamage(int delta)
        {
            if (currentCardInstance == null) return;

            if (delta > 0)
            {
                float cost = CalcDamageCost(0); // 上昇前のコスト段階で消費

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

                // 下げる前にコスト段階を確定する
                float refundCost = CalcDamageCost(0);
                int revertAmount = Mathf.Min(-delta, bonus);

                currentCardInstance.AddBonusValue(0, -revertAmount);

                BattleEventBus.Publish(new LuckGaugeRefundEvent(
                    amount: refundCost * revertAmount
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

        public void SetLimitBreakCostLevelBonus(int value)
        {
            limitBreakCostLevelBonus = Mathf.Max(0, value);
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