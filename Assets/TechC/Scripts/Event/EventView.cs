using System;
using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace TechC.ODDESEY.Event
{
    /// <summary>
    /// イベント画面の表示を管理する View クラス。
    ///
    /// 仕様書 3-8 のイベント画面表示項目に対応。
    /// ボタンのコールバックは EventController から注入する。
    ///
    /// 画面構成：
    ///   イベント画面（eventPanel）
    ///     イベント名・説明・成功率・成功/失敗結果・ゲージ情報
    ///     [運ゲージを使う] [運ゲージを戻す] [挑戦する] [やめる]
    ///   結果画面（resultPanel）
    ///     結果（成功/失敗）・フレーバーテキスト・結果内容・還元量
    ///     [次へ進む]
    /// </summary>
    public class EventView : MonoBehaviour
    {
        // ─── イベント画面 ─────────────────────────────────────────────────
        [Header("イベント画面")]
        [SerializeField] private GameObject eventPanel;
        [SerializeField] private TextMeshProUGUI eventNameText;
        [SerializeField] private TextMeshProUGUI descriptionText;
        [SerializeField] private TextMeshProUGUI baseSuccessRateText;
        [SerializeField] private TextMeshProUGUI finalSuccessRateText;
        [SerializeField] private TextMeshProUGUI successResultText;
        [SerializeField] private TextMeshProUGUI failureResultText;
        [SerializeField] private TextMeshProUGUI currentGaugeText;
        [SerializeField] private TextMeshProUGUI reservedGaugeText;

        [Header("ボタン")]
        [SerializeField] private Button addGaugeButton;
        [SerializeField] private Button removeGaugeButton;
        [SerializeField] private Button challengeButton;
        [SerializeField] private Button cancelButton;

        [Header("長押し設定")]
        [SerializeField] private float holdDelay = 0.5f;
        [SerializeField] private float holdInterval = 0.1f;

        // ─── 結果画面 ─────────────────────────────────────────────────────
        [Header("結果画面")]
        [SerializeField] private GameObject resultPanel;
        [SerializeField] private TextMeshProUGUI resultLabelText;     // 「成功」or「失敗」
        [SerializeField] private TextMeshProUGUI flavorText;
        [SerializeField] private TextMeshProUGUI resultContentText;   // 結果内容（HP+12 等）
        [SerializeField] private TextMeshProUGUI refundText;          // 還元量（失敗時のみ）
        [SerializeField] private Button nextButton;

        // ─── コールバック ────────────────────────────────────────────────
        private Action onChallenge;
        private Action onCancel;
        private Action onAddGauge;
        private Action onRemoveGauge;

        // ─── 初期化 ───────────────────────────────────────────────────────

        /// <summary>
        /// EventController から呼ぶ初期化。
        /// イベントの静的情報をセットし、ボタンコールバックを登録する。
        /// </summary>
        public void Setup(
            EventData data,
            int currentGauge,
            int reservedGauge,
            int finalSuccessRate,
            Action onChallenge,
            Action onCancel,
            Action onAddGauge,
            Action onRemoveGauge)
        {
            this.onChallenge = onChallenge;
            this.onCancel = onCancel;
            this.onAddGauge = onAddGauge;
            this.onRemoveGauge = onRemoveGauge;

            // 静的テキスト
            eventNameText.text = data.EventName;
            descriptionText.text = data.Description;
            baseSuccessRateText.text = $"基礎成功率：{data.BaseSuccessRate}%";
            successResultText.text = FormatResult("成功", data.SuccessResultType, data.SuccessResultValue);
            failureResultText.text = FormatResult("失敗", data.FailureResultType, data.FailureResultValue);

            // ボタン登録（長押し対応）
            RegisterHoldButton(addGaugeButton, () => onAddGauge?.Invoke());
            RegisterHoldButton(removeGaugeButton, () => onRemoveGauge?.Invoke());

            challengeButton.onClick.RemoveAllListeners();
            challengeButton.onClick.AddListener(() => onChallenge?.Invoke());

            cancelButton.onClick.RemoveAllListeners();
            cancelButton.onClick.AddListener(() => onCancel?.Invoke());

            // パネル表示
            eventPanel.SetActive(true);
            resultPanel.SetActive(false);

            UpdateGaugeInfo(currentGauge, reservedGauge, finalSuccessRate,
                canAdd: true, canRemove: false);
        }

        // ─── ゲージ情報の更新（動的）────────────────────────────────────

        /// <summary>
        /// 運ゲージ操作のたびに EventController から呼ばれる。
        /// </summary>
        public void UpdateGaugeInfo(
            int currentGauge,
            int reservedGauge,
            int finalSuccessRate,
            bool canAdd,
            bool canRemove)
        {
            currentGaugeText.text = $"現在の運ゲージ：{currentGauge}%";
            reservedGaugeText.text = $"使用予定：{reservedGauge}%";
            finalSuccessRateText.text = $"最終成功率：{finalSuccessRate}%";

            addGaugeButton.interactable = canAdd;
            removeGaugeButton.interactable = canRemove;
        }

        // ─── 結果画面 ─────────────────────────────────────────────────────

        /// <summary>
        /// 挑戦結果を表示する。
        /// </summary>
        public void ShowResult(EventResult result, Action onClose)
        {
            eventPanel.SetActive(false);
            resultPanel.SetActive(true);

            resultLabelText.text = result.IsSuccess ? "【成功】" : "【失敗】";
            flavorText.text = result.FlavorText;
            resultContentText.text = FormatResult(
                result.IsSuccess ? "成功" : "失敗",
                result.ResultType,
                result.ResultValue);

            if (result.RefundedGauge > 0)
            {
                refundText.gameObject.SetActive(true);
                refundText.text = $"運ゲージ +{result.RefundedGauge}%（失敗時還元）";
            }
            else
            {
                refundText.gameObject.SetActive(false);
            }

            nextButton.onClick.RemoveAllListeners();
            nextButton.onClick.AddListener(() => onClose?.Invoke());
        }

        // ─── 長押しボタン登録 ────────────────────────────────────────────

        private void RegisterHoldButton(Button button, Action action)
        {
            var trigger = button.gameObject.GetComponent<EventTrigger>()
                       ?? button.gameObject.AddComponent<EventTrigger>();
            trigger.triggers.Clear();

            var down = new EventTrigger.Entry { eventID = EventTriggerType.PointerDown };
            down.callback.AddListener(_ =>
            {
                if (button.interactable)
                    StartCoroutine(HoldCoroutine(button, action));
            });
            trigger.triggers.Add(down);

            var up = new EventTrigger.Entry { eventID = EventTriggerType.PointerUp };
            up.callback.AddListener(_ => StopAllCoroutines());
            trigger.triggers.Add(up);

            var exit = new EventTrigger.Entry { eventID = EventTriggerType.PointerExit };
            exit.callback.AddListener(_ => StopAllCoroutines());
            trigger.triggers.Add(exit);
        }

        private IEnumerator HoldCoroutine(Button button, Action action)
        {
            action(); // 1回目（クリック相当）
            yield return new WaitForSeconds(holdDelay);
            while (button.interactable)
            {
                action();
                yield return new WaitForSeconds(holdInterval);
            }
        }

        // ─── ユーティリティ ───────────────────────────────────────────────

        private string FormatResult(string label, EventResultType type, int value)
        {
            return type switch
            {
                EventResultType.HealHp => $"HPを{value}回復する",
                EventResultType.DamageHp => $"HPを{value}失う",
                EventResultType.GainGauge => $"運ゲージ+{value}%",
                EventResultType.LoseGauge => $"運ゲージ-{value}%",
                EventResultType.GainCard => $"カードを{value}枚獲得する",
                EventResultType.None => "何も起きない",
                _ => "",
            };
        }
    }
}
