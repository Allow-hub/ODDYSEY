using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System;
using TechC.ODDESEY.Util;
using TechC.VBattle.Core.Extensions;

namespace TechC.ODDESEY.Battle
{
    public class BattleView : MonoBehaviour
    {
        [SerializeField] private PlayZonePresenter playZonePresenter;
        [Header("Hand")]
        [SerializeField] private Transform handContainer;
        [SerializeField] private GameObject cardViewPrefab;

        [Header("Play zone")]
        [SerializeField] private Transform[] slotTransforms;

        [Header("HP")]
        [SerializeField] private Slider playerHpSlider;
        [SerializeField] private Slider enemyHpSlider;

        [Header("Luck gauge")]
        [SerializeField] private Slider luckGaugeSlider;
        [SerializeField] private GameObject hotModeEffect;

        [Header("バトル開始演出")]
        [SerializeField] private CanvasGroup fadePanel;
        [SerializeField] private GameObject battleStartText;
        [SerializeField] private Transform enemySpawnPoint;  // 敵を生成する位置
        [SerializeField] private Button confirmButton;  // ターン確定ボタン

        [Header("Effects")]
        [SerializeField] private GameObject winEffectObj;
        [SerializeField] private GameObject loseEffectObj;

        [Header("アニメ設定")]
        [SerializeField] private float sliderDuration = 0.3f;
        [SerializeField] private float effectDuration = 1.5f;
        [SerializeField] private float fadeDuration = 0.4f;
        [SerializeField] private float textFadeDuration = 0.3f;

        [Header("手札レイアウト")]
        [SerializeField] private float startX = -400f;
        [SerializeField] private float spacing = 200f;
        [SerializeField] private float y = -300f;
        [SerializeField] private Vector2 deckStartPos = new Vector2(800, -500);

        private EnemyView currentEnemyView;
        private UniTaskCompletionSource confirmTcs;
        private List<CardData> previousHand = new();  // 差分検出用

        public void Init()
        {
            // winEffectObj?.SetActive(false);
            // loseEffectObj?.SetActive(false);
            // hotModeEffect?.SetActive(false);
            // battleStartText?.SetActive(false);
            // enemyObject?.SetActive(false);

            if (fadePanel != null) fadePanel.alpha = 1f;
            if (confirmButton != null)
                confirmButton.onClick.AddListener(ConfirmTurn);
        }

        /// <summary>
        /// バトル開始演出。暗転 → 敵登場 → テキスト表示 → 手札ドロー の順で再生する。
        /// </summary>
        public async UniTask PlayBattleStartAsync(TurnData firstTurnData, EnemyData enemyData)
        {
            battleStartText.SetActive(false);
            // 1. 暗転状態から明転
            await FadeAsync(1f, 0f, fadeDuration);

            // 2. 敵を登場させる
            if (enemyData != null && enemyData.EnemyPrefab != null)
            {
                var obj = Instantiate(enemyData.EnemyPrefab, enemySpawnPoint);
                currentEnemyView = obj.GetComponent<EnemyView>();
                currentEnemyView.Setup(enemyData);
                await currentEnemyView.PlayEnterAnimationAsync();
            }

            // 3. "Battle Start" テキストを表示して消す
            if (battleStartText != null)
            {
                float activeWaitDuration = 0.5f;
                await UniTask.Delay(TimeSpan.FromSeconds(activeWaitDuration));
                battleStartText.SetActive(true);
                float activeTextDuration = 0.3f;
                await UniTask.Delay(TimeSpan.FromSeconds(activeTextDuration));
                await FadeObjectAsync(battleStartText, 1f, 0f, textFadeDuration);
                battleStartText.SetActive(false);
            }

            // 4. HP・ゲージを初期値にセットしてから手札をドロー
            // UpdateHpImmediate(firstTurnData.PlayerHp, firstTurnData.PlayerHpMax,
            //                   firstTurnData.EnemyHp,  firstTurnData.EnemyHpMax);
            // UpdateLuckGaugeImmediate(firstTurnData.LuckGauge, firstTurnData.IsHotMode);

            await UpdateHandAsync(firstTurnData.Hand);
            // ShowPlayZone(firstTurnData.PlayZone);
        }

        /// <summary>
        /// ターン開始時の表示更新。2ターン目以降はこちらを使う。
        /// </summary>
        public async UniTask ShowTurnStartAsync(TurnData turnData)
        {
            // UpdateHpImmediate(turnData.PlayerHp, turnData.PlayerHpMax,
            //                   turnData.EnemyHp, turnData.EnemyHpMax);
            // UpdateLuckGaugeImmediate(turnData.LuckGauge, turnData.IsHotMode);

            await UpdateHandAsync(turnData.Hand);
            // ShowPlayZone(turnData.PlayZone);
        }

        /// <summary>
        /// 手札を更新。新しく追加されたカードだけドロー演出する（差分検出版）。
        /// previousHand を保持して、増減したカードのみ処理。
        /// </summary>
        private async UniTask UpdateHandAsync(List<CardInstance> newHand)
        {
            // -----------------------------
            // 減った分削除
            // -----------------------------
            for (int i = newHand.Count; i < previousHand.Count; i++)
            {
                if (i < handContainer.childCount)
                    Destroy(handContainer.GetChild(i).gameObject);
            }

            var tasks = new List<UniTask>();

            // -----------------------------
            // 既存カードの位置更新
            // -----------------------------
            for (int i = 0; i < Mathf.Min(previousHand.Count, newHand.Count); i++)
            {
                var view = handContainer.GetChild(i).GetComponent<CardView>();
                var targetPos = HandLayoutUtility.GetLinearPosition(i, startX, spacing, y);

                tasks.Add(view.PlayDealAnimationAsync(
                    view.GetComponent<RectTransform>().anchoredPosition,
                    targetPos
                ));
            }

            // -----------------------------
            // 新規カード追加
            // -----------------------------
            for (int i = previousHand.Count; i < newHand.Count; i++)
            {
                var cardInstance = newHand[i];

                var obj = Instantiate(cardViewPrefab, handContainer);
                var view = obj.GetComponent<CardView>();

                view.Setup(cardInstance.OriginalData, cardInstance.InstanceId, playZonePresenter.OnCardReturnRequested);
                var targetPos = HandLayoutUtility.GetLinearPosition(i, startX, spacing, y);

                tasks.Add(view.PlayDealAnimationAsync(deckStartPos, targetPos));
            }

            await UniTask.WhenAll(tasks);

            previousHand = new List<CardData>(newHand.ConvertAll(ci => ci.OriginalData));
            CustomLogger.Info($"手札を更新: {string.Join(", ", newHand.ConvertAll(ci => ci.OriginalData.CardName))}", LogTagUtil.TagBattle);
        }

        // private void ShowPlayZone(PlayZoneSlot[] slots)
        // {
        //     for (int i = 0; i < slotTransforms.Length; i++)
        //     {
        //         if (i >= slots.Length || slots[i].IsEmpty) continue;
        //         // TODO: スロットの CardView を生成して slotTransforms[i] に配置する
        //     }
        // }

        public UniTask WaitForPlayerConfirmAsync()
        {
            confirmTcs = new UniTaskCompletionSource();
            return confirmTcs.Task;
        }

        /// <summary>ターン確定ボタンの OnClick から呼ぶ</summary>
        public void ConfirmTurn()
        {
            confirmTcs?.TrySetResult();
        }

        // public async UniTask PlayCardResolveAsync(CardResolveResult result)
        // {
        //     if (result.WasBroken)
        //     {
        //         // TODO: 対応する CardView を取得して PlayBreakAnimationAsync() を呼ぶ
        //         await UniTask.Delay(300);
        //     }
        //     else if (result.IsHit)
        //     {
        //         // TODO: ヒットエフェクト
        //         await UniTask.Delay(400);
        //     }
        //     else
        //     {
        //         // TODO: ミスエフェクト
        //         await UniTask.Delay(200);
        //     }
        // }

        public async UniTask UpdateLuckGaugeAsync(float gauge, bool isHotMode)
        {
            hotModeEffect?.SetActive(isHotMode);
            if (luckGaugeSlider != null)
                await LerpSliderAsync(luckGaugeSlider, gauge / 100f, sliderDuration);
        }

        public void UpdateLuckGaugeImmediate(float gauge, bool isHotMode)
        {
            if (luckGaugeSlider != null) luckGaugeSlider.value = gauge / 100f;
            hotModeEffect?.SetActive(isHotMode);
        }

        public async UniTask UpdateHpAsync(int playerHp, int playerHpMax, int enemyHp, int enemyHpMax)
        {
            await UniTask.WhenAll(
                playerHpSlider != null
                    ? LerpSliderAsync(playerHpSlider, (float)playerHp / playerHpMax, sliderDuration)
                    : UniTask.CompletedTask,
                enemyHpSlider != null
                    ? LerpSliderAsync(enemyHpSlider, (float)enemyHp / enemyHpMax, sliderDuration)
                    : UniTask.CompletedTask
            );
        }

        public void UpdateHpImmediate(int playerHp, int playerHpMax, int enemyHp, int enemyHpMax)
        {
            if (playerHpSlider != null) playerHpSlider.value = (float)playerHp / playerHpMax;
            if (enemyHpSlider != null) enemyHpSlider.value = (float)enemyHp / enemyHpMax;
        }

        public async UniTask ShowWinEffectAsync()
        {
            winEffectObj?.SetActive(true);
            await UniTask.Delay((int)(effectDuration * 1000));
        }

        public async UniTask ShowLoseEffectAsync()
        {
            loseEffectObj?.SetActive(true);
            await UniTask.Delay((int)(effectDuration * 1000));
        }

        private async UniTask FadeAsync(float from, float to, float duration)
        {
            if (fadePanel == null) return;

            float elapsed = 0f;
            fadePanel.alpha = from;

            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                float t = Mathf.Clamp01(elapsed / duration);
                fadePanel.alpha = Mathf.Lerp(from, to, t);
                await UniTask.Yield();
            }

            fadePanel.alpha = to;
        }

        private async UniTask FadeObjectAsync(GameObject obj, float from, float to, float duration)
        {
            var group = obj.GetComponent<CanvasGroup>();
            if (group == null) group = obj.AddComponent<CanvasGroup>();

            float elapsed = 0f;
            group.alpha = from;

            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                float t = Mathf.Clamp01(elapsed / duration);
                group.alpha = Mathf.Lerp(from, to, t);
                await UniTask.Yield();
            }

            group.alpha = to;
        }

        private async UniTask LerpSliderAsync(Slider slider, float targetValue, float duration)
        {
            float elapsed = 0f;
            float startValue = slider.value;

            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                float t = Mathf.Clamp01(elapsed / duration);
                float ease = 1f - Mathf.Pow(1f - t, 3f);
                slider.value = Mathf.Lerp(startValue, targetValue, ease);
                await UniTask.Yield();
            }

            slider.value = targetValue;
        }
    }
}