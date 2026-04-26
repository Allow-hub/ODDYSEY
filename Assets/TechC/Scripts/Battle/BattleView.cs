using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using UnityEngine;
using UnityEngine.UI;
using System;
using TechC.ODDESEY.Util;
using TechC.VBattle.Core.Extensions;
using TechC.ODDESEY.Core.Manager;
using TechC.Core.Manager;
using System.Threading;

namespace TechC.ODDESEY.Battle
{
    /// <summary>
    /// 戦闘の見た目を管理する
    /// </summary>
    public class BattleView : MonoBehaviour
    {
        [SerializeField] private Animator anim;
        [SerializeField] private PlayZonePresenter playZonePresenter;
        [SerializeField] private PlayerView playerView;
        [SerializeField] private HpView playerHpView;
        [SerializeField] private HpView enemyHpView;
        [SerializeField] private LuckGaugeView luckGaugeView;

        [Header("Hand")]
        [SerializeField] private Transform handContainer;
        [SerializeField] private GameObject cardViewPrefab;

        [Header("UI")]
        [SerializeField] private CanvasGroup fadePanel;
        [SerializeField] private GameObject battleStartText;
        [SerializeField] private Transform enemySpawnPoint;
        [SerializeField] private Button confirmButton;
        [SerializeField] private GameObject winEffectObj;
        [SerializeField] private GameObject loseEffectObj;
        [SerializeField] private Button pauseButton;

        [Header("Animation")]
        [SerializeField] private float fadeDuration = 0.4f;
        [SerializeField] private float textFadeDuration = 0.3f;

        [Header("Hand Layout")]
        [SerializeField] private float startX = -400f;
        [SerializeField] private float spacing = 200f;
        [SerializeField] private float y = -300f;
        [SerializeField] private Vector2 deckStartPos = new Vector2(800, -500);
        private CancellationToken destroyToken;

        private EnemyView currentEnemyView;
        private UniTaskCompletionSource confirmTcs;
        private Dictionary<int, CardView> handViews = new();

        public void Init()
        {
            luckGaugeView.Setup(max: 100f);
            luckGaugeView.UpdateGaugeImmediate(MainManager.I?.LuckGaugeValue ?? 0f, 100f, false);
            winEffectObj.SetActive(false);
            loseEffectObj.SetActive(false);
            if (fadePanel != null) fadePanel.alpha = 1f;
            confirmButton?.onClick.AddListener(ConfirmTurn);
            pauseButton?.onClick.AddListener(() => PauseManager.I?.Pause());
            destroyToken = this.GetCancellationTokenOnDestroy();
        }

        // ================================
        // 公開API（バトル進行）
        // ================================

        /// <summary>
        /// バトル開始演出。フェードイン → 敵登場 → テキスト表示 → テキストフェードアウト。
        /// </summary>
        public async UniTask PlayBattleStartAsync(TurnData firstTurnData, EnemyData enemyData)
        {
            battleStartText.SetActive(false);

            playerHpView.Setup(firstTurnData.PlayerHpMax);
            enemyHpView.Setup(firstTurnData.EnemyHpMax);

            await FadeAsync(1f, 0f, fadeDuration);

            if (enemyData?.EnemyPrefab != null)
            {
                var obj = Instantiate(enemyData.EnemyPrefab, enemySpawnPoint);
                currentEnemyView = obj.GetComponent<EnemyView>();
                currentEnemyView.Setup(enemyData);
                await currentEnemyView.PlayEnterAnimationAsync();
            }

            if (battleStartText != null)
            {
                await UniTask.Delay(TimeSpan.FromSeconds(0.5f));
                battleStartText.SetActive(true);
                await UniTask.Delay(TimeSpan.FromSeconds(0.3f));
                await FadeObjectAsync(battleStartText, 1f, 0f, textFadeDuration);
                battleStartText.SetActive(false);
            }

            await UpdateHandAsync(firstTurnData.Hand);
        }

        /// <summary>
        /// ターン開始演出。テキスト表示 → テキストフェードアウト。
        /// </summary>
        public async UniTask ShowTurnStartAsync(TurnData turnData)
        {
            await UpdateHandAsync(turnData.Hand);
        }

        /// <summary>
        /// カード実行演出。カードの効果に応じたアニメーションを再生する。
        /// </summary>
        public async UniTask PlayCardResolveAsync(CardResolveResult result)
        {
            // ② プレイヤー or 敵のアニメーション
            if (result.IsPlayer)
                await playerView.PlayAttackAnimationAsync();
            else
                await currentEnemyView.PlayAttackAnimationAsync();
        }

        public async UniTask UpdatePlayerHpAsync(int current, int max)
            => await playerHpView.UpdateHpAsync(current, max);

        public async UniTask UpdateEnemyHpAsync(int current, int max)
            => await enemyHpView.UpdateHpAsync(current, max);

        public async UniTask UpdateLuckGaugeAsync(float current, float max, bool isHotMode)
            => await luckGaugeView.UpdateGaugeAsync(current, max, isHotMode);

        public void UpdateLuckGaugeImmediate(float current, float max, bool isHotMode)
            => luckGaugeView.UpdateGaugeImmediate(current, max, isHotMode);

        /// <summary>
        /// 使用されたカードを View から削除する。アニメーションを再生してから削除する。    
        /// </summary>
        public async UniTask RemoveUsedCardsAsync(List<CardResolveResult> results)
        {
            var tasks = new List<UniTask>();

            foreach (var r in results)
            {
                if (!r.IsPlayer) continue;

                if (handViews.TryGetValue(r.CardInstanceId, out var view))
                {
                    tasks.Add(RemoveCardAsync(view.InstanceId));
                    handViews.Remove(r.CardInstanceId);
                }
                else
                    CustomLogger.Warning($"削除対象のカードViewが見つからない: InstanceId {r.CardInstanceId}", LogTagUtil.TagCard);
            }
            if (!destroyToken.IsCancellationRequested && anim)
                anim.SetBool("BattleEnd", true);

            await UniTask.WhenAll(tasks);
        }

        /// <summary>
        /// カードを View から削除する。アニメーションを再生してから削除する。
        /// </summary>
        public async UniTask RemoveCardAsync(int instanceId)
        {
            if (!handViews.TryGetValue(instanceId, out var view))
            {
                CustomLogger.Warning($"削除対象のカードViewが見つからない: InstanceId {instanceId}", LogTagUtil.TagCard);
                return;
            }

            await view.PlayBreakAnimationAsync();

            if (view != null)
                Destroy(view.gameObject);

            handViews.Remove(instanceId);
        }

        /// <summary>
        /// プレイヤーの入力待ち。ターン確定ボタンが押されるまで完了しない UniTask を返す。
        /// </summary>
        public UniTask WaitForPlayerConfirmAsync()
        {
            anim?.SetBool("BattleStart", false);
            confirmTcs = new UniTaskCompletionSource();
            return confirmTcs.Task;
        }

        public void ConfirmTurn()
        {
            anim?.SetBool("BattleEnd", false);
            anim?.SetBool("BattleStart", true);
            confirmTcs?.TrySetResult();
        }

        /// <summary>
        /// 勝利時の演出
        /// </summary>
        /// <returns></returns>
        public async UniTask ShowWinEffectAsync()
        {
            winEffectObj.SetActive(true);
            await UniTask.Delay(4000);//仮
        }

        /// <summary>
        /// 敗北時の演出
        /// </summary>
        /// <returns></returns>
        public async UniTask ShowLoseEffectAsync()
        {
            loseEffectObj.SetActive(true);
            await UniTask.Delay(4000);//仮
        }

        /// <summary>
        /// 手札の内容を View に反映する。カードの追加・削除・移動をアニメーション付きで行う。
        /// </summary>
        private async UniTask UpdateHandAsync(List<CardInstance> newHand)
        {
            var tasks = new List<UniTask>();
            var newIds = new HashSet<int>();

            // -----------------------------
            // 生成・更新
            // -----------------------------
            for (int i = 0; i < newHand.Count; i++)
            {
                var instance = newHand[i];
                newIds.Add(instance.InstanceId);

                var targetPos = HandLayoutUtility.GetLinearPosition(i, startX, spacing, y);

                CardView view;

                if (handViews.TryGetValue(instance.InstanceId, out view))
                {
                    var currentPos = view.GetComponent<RectTransform>().anchoredPosition;
                    tasks.Add(view.PlayDealAnimationAsync(currentPos, targetPos));
                }
                else
                {
                    var obj = Instantiate(cardViewPrefab, handContainer);
                    view = obj.GetComponent<CardView>();

                    view.Setup(
                        instance,
                        playZonePresenter.OnCardReturnRequested
                    );

                    handViews[instance.InstanceId] = view;

                    tasks.Add(view.PlayDealAnimationAsync(deckStartPos, targetPos));
                }

                view.transform.SetParent(handContainer);
            }

            // -----------------------------
            // 削除（存在しないもの）
            // -----------------------------
            var removeList = new List<int>();

            foreach (var kv in handViews)
            {
                if (!newIds.Contains(kv.Key))
                    removeList.Add(kv.Key);
            }

            foreach (var id in removeList)
            {
                if (handViews.TryGetValue(id, out var view))
                {
                    Destroy(view.gameObject);
                    handViews.Remove(id);
                }
            }

            await UniTask.WhenAll(tasks);
        }

        /// <summary>
        /// フェードアニメーション。from → to に fadeDuration 秒かけて変化させる。
        /// </summary>
        private async UniTask FadeAsync(float from, float to, float duration)
        {
            if (fadePanel == null) return;

            float t = 0;
            fadePanel.alpha = from;

            while (t < duration)
            {
                t += Time.deltaTime;
                fadePanel.alpha = Mathf.Lerp(from, to, t / duration);
                await UniTask.Yield();
            }

            fadePanel.alpha = to;
        }

        private async UniTask FadeObjectAsync(GameObject obj, float from, float to, float duration)
        {
            var group = obj.GetComponent<CanvasGroup>() ?? obj.AddComponent<CanvasGroup>();

            float t = 0;
            group.alpha = from;

            while (t < duration)
            {
                t += Time.deltaTime;
                group.alpha = Mathf.Lerp(from, to, t / duration);
                await UniTask.Yield();
            }

            group.alpha = to;
        }
    }
}