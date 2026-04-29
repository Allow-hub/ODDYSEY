using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace TechC.ODDESEY.Battle
{
    /// <summary>
    /// プレイゾーン全体を管理する View クラス。
    /// View だが CardInstance の値を直接操作する「軽いロジック」も持つ。
    ///
    /// 運ゲージ消費が必要な操作は LuckGaugeSpendRequestEvent を発行し、
    /// BattleController が BattleLogic に委譲する。消費結果はコールバックで受け取る。
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
        [SerializeField] private float percentageAdjustCost = 1;  // %変更1回あたりのゲージ消費量
        [SerializeField] private float damageAdjustCost = 5;      // ダメージ変更1回あたりのゲージ消費量

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
            upPercentageButton.onClick.AddListener(() => AdjustPercentage(1));
            downPercentageButton.onClick.AddListener(() => AdjustPercentage(-5));
            upDamageButton.onClick.AddListener(() => AdjustDamage(1));
            downDamageButton.onClick.AddListener(() => AdjustDamage(-1));

            SetButtonsInteractable(false); // カード未選択状態ではボタンを無効化
        }

        // ──────────────────────────────────────────────────────────
        // イベント受信
        // ──────────────────────────────────────────────────────────

        /// <summary>
        /// 配置済みカードがクリックされたとき、そのカードを操作対象にする。
        /// </summary>
        private void OnCardPlacedClicked(CardPlacedClickedEvent evt)
        {
            currentCardInstance = evt.Card.CardInstance;
            RefreshDisplay();
            SetButtonsInteractable(true);
        }

        // ──────────────────────────────────────────────────────────
        // 操作（運ゲージ消費あり）
        // ──────────────────────────────────────────────────────────

        /// <summary>
        /// 命中率を増減する。消費コストを払えた場合のみ値を変更する。
        /// </summary>
        private void AdjustPercentage(int delta)
        {
            if (currentCardInstance == null) return;

            BattleEventBus.Publish(new LuckGaugeSpendRequestEvent(
                cost: percentageAdjustCost,
                onResult: success =>
                {
                    if (!success) return; // ゲージ不足 → 何もしない

                    int current = (int)(currentCardInstance.GetEffectiveProbability(0) * 100);
                    int newValue = Mathf.Clamp(current + delta, 0, 100);
                    // ボーナス値として delta 分だけ加算（絶対値ではなく差分）
                    currentCardInstance.AddBonusProbability(0, delta / 100f);
                    RefreshDisplay();
                }
            ));
        }

        /// <summary>
        /// ダメージ値を増減する。消費コストを払えた場合のみ値を変更する。
        /// </summary>
        private void AdjustDamage(int delta)
        {
            if (currentCardInstance == null) return;

            BattleEventBus.Publish(new LuckGaugeSpendRequestEvent(
                cost: damageAdjustCost,
                onResult: success =>
                {
                    if (!success) return; // ゲージ不足 → 何もしない

                    currentCardInstance.AddBonusValue(0, delta);
                    RefreshDisplay();
                }
            ));
        }

        // ──────────────────────────────────────────────────────────
        // 表示更新
        // ──────────────────────────────────────────────────────────

        private void RefreshDisplay()
        {
            if (currentCardInstance == null) return;
            int probability = (int)(currentCardInstance.GetEffectiveProbability(0) * 100);
            int damage = currentCardInstance.GetEffectiveValue(0);
            percentageText.text = $"{probability}%";
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