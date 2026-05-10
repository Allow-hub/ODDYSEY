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
        [Header("ダメージポップアップ")]
        [SerializeField] private DamagePopupManager damagePopupManager;
        [SerializeField] private Transform playerPopupAnchor;
        [SerializeField] private Transform enemyPopupAnchor;

        [Header("Animation")]
        [SerializeField] private float fadeDuration = 0.4f;
        [SerializeField] private float textFadeDuration = 0.3f;
        [Tooltip("BattleStartアニメ待機時間（秒）")]
        [SerializeField] private float battleStartAnimDuration = 1.0f;

        [Header("Hand Layout")]
        [SerializeField] private float startX = -400f;
        [SerializeField] private float spacing = 200f;
        [SerializeField] private float y = -300f;
        [SerializeField] private Vector2 deckStartPos = new Vector2(800, -500);

        [Header("バトルテンポ")]
        [Tooltip("カード1枚の演出が終わってから次のカードへ進むまでの待機時間（秒）")]
        [SerializeField] private float cardResolveInterval = 0.3f;

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

        public async UniTask ShowTurnStartAsync(TurnData turnData)
        {
            await UpdateHandAsync(turnData.Hand);
        }

        /// <summary>
        /// カード1枚の解決演出を再生する。
        ///
        /// 改善点：
        ///   - Phase2 で HP更新と攻撃アニメ残り部分を並列実行。
        ///     「HPが減るタイミング」と「アニメが終わるタイミング」が揃い
        ///     テンポよく見える。
        ///   - 被ダメアニメは引き続き Forget()（待たない）。
        /// </summary>
        public async UniTask PlayCardResolveAsync(
            CardResolveResult result, int playerHpMax, int enemyHpMax)
        {
            var animType = result.AnimationType;

            if (result.IsPlayer)
            {
                // Phase1: ヒット判定フレームまで待つ
                await playerView.BeginAttackAnimationAsync(animType);

                // Phase2: 被ダメアニメ・HP更新・攻撃アニメ残りを並列実行
                currentEnemyView.PlayDamageAnimationAsync(result.IsHit).Forget();
                // ▼ ヒット・ミス両方でポップアップを出す
                damagePopupManager.Show(
                    result.DamageDealt,
                    isHit: result.IsHit,
                    isPlayerDamage: false,
                    isCritical: result.GetExtra<bool>(ResultKeys.IsCritical),
                    worldPos: enemyPopupAnchor != null
                                        ? enemyPopupAnchor.position
                                        : currentEnemyView.transform.position);

                await UniTask.WhenAll(
                    currentEnemyView.PlayDamageAnimationAsync(result.IsHit),
                    result.DamageDealt > 0
                        ? enemyHpView.UpdateHpAsync(result.EnemyHpAfter, enemyHpMax)
                        : UniTask.CompletedTask,
                    playerView.WaitAttackFinishedAsync(animType)
                );
            }
            else
            {
                // Phase1: ヒット判定フレームまで待つ
                await currentEnemyView.BeginAttackAnimationAsync(animType);

                // Phase2: 被ダメアニメ・HP更新・攻撃アニメ残りを並列実行
                playerView.PlayDamageAnimationAsync(result.IsHit).Forget();
                // ▼ ヒット・ミス両方でポップアップを出す
                damagePopupManager.Show(
                    result.DamageDealt,
                    isHit: result.IsHit,
                    isPlayerDamage: true,
                    isCritical: false,
                    worldPos: playerPopupAnchor != null
                    ? playerPopupAnchor.position
                    : playerView.transform.position);

                await UniTask.WhenAll(
                    playerView.PlayDamageAnimationAsync(result.IsHit),
                    result.DamageDealt > 0
                        ? playerHpView.UpdateHpAsync(result.PlayerHpAfter, playerHpMax)
                        : UniTask.CompletedTask,
                    currentEnemyView.WaitAttackFinishedAsync(animType)
                );
            }

            // 自傷ダメージ
            var selfDamage = result.GetExtra<int>(ResultKeys.SelfDamageDealt);
            if (selfDamage > 0)
            {
                if (result.IsPlayer)
                {
                    // プレイヤー自傷
                    damagePopupManager.Show(selfDamage, isHit: true, isPlayerDamage: true, isCritical: false,
                        playerPopupAnchor != null ? playerPopupAnchor.position : playerView.transform.position);
                    // playerView.PlayDamageAnimationAsync(isHit: true).Forget();
                    await playerHpView.UpdateHpAsync(result.PlayerHpAfter, playerHpMax);
                }
                else
                {
                    damagePopupManager.Show(selfDamage, isHit: true, isPlayerDamage: false, isCritical: false,
                        enemyPopupAnchor != null ? enemyPopupAnchor.position : currentEnemyView.transform.position);
                    // currentEnemyView.PlayDamageAnimationAsync(isHit: true).Forget();
                    await enemyHpView.UpdateHpAsync(result.EnemyHpAfter, enemyHpMax);
                }
            }

            // カウンター
            bool counterTriggered = result.GetExtra<bool>(ResultKeys.CounterTriggered);
            if (counterTriggered)
            {
                currentEnemyView.PlayDamageAnimationAsync(isHit: true).Forget();
                await enemyHpView.UpdateHpAsync(result.EnemyHpAfter, enemyHpMax);
            }
        }

        public async UniTask UpdatePlayerHpAsync(int current, int max)
            => await playerHpView.UpdateHpAsync(current, max);

        public async UniTask UpdateEnemyHpAsync(int current, int max)
            => await enemyHpView.UpdateHpAsync(current, max);

        public async UniTask UpdateLuckGaugeAsync(float current, float max, bool isHotMode)
            => await luckGaugeView.UpdateGaugeAsync(current, max, isHotMode);

        public void UpdateLuckGaugeImmediate(float current, float max, bool isHotMode)
            => luckGaugeView.UpdateGaugeImmediate(current, max, isHotMode);

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
                    CustomLogger.Warning(
                        $"削除対象のカードViewが見つからない: InstanceId {r.CardInstanceId}",
                        LogTagUtil.TagCard);
            }

            if (!destroyToken.IsCancellationRequested && anim)
                anim.SetBool("BattleEnd", true);

            await UniTask.WhenAll(tasks);
        }

        public async UniTask RemoveCardAsync(int instanceId)
        {
            if (!handViews.TryGetValue(instanceId, out var view))
            {
                CustomLogger.Warning(
                    $"削除対象のカードViewが見つからない: InstanceId {instanceId}",
                    LogTagUtil.TagCard);
                return;
            }

            await view.PlayBreakAnimationAsync();

            if (view != null) Destroy(view.gameObject);
            handViews.Remove(instanceId);
        }

        /// <summary>
        /// ConfirmTurn 後に BattleStart アニメが終わるまで待つ。
        /// BattleController で WaitForPlayerConfirmAsync の直後に呼ぶ。
        /// </summary>
        public async UniTask WaitForBattleStartAnimAsync()
        {
            await UniTask.Delay(
                TimeSpan.FromSeconds(battleStartAnimDuration),
                ignoreTimeScale: true);
        }

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

        public async UniTask ShowWinEffectAsync()
        {
            winEffectObj.SetActive(true);
            await UniTask.Delay(4000);
        }

        public async UniTask ShowLoseEffectAsync()
        {
            loseEffectObj.SetActive(true);
            await UniTask.Delay(4000);
        }

        /// <summary>カード間インターバルを返す（BattleController が使う）</summary>
        public float CardResolveInterval => cardResolveInterval;

        // ================================
        // 内部処理
        // ================================

        private async UniTask UpdateHandAsync(List<CardInstance> newHand)
        {
            var tasks = new List<UniTask>();
            var newIds = new HashSet<int>();

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
                    view.Setup(instance);
                    handViews[instance.InstanceId] = view;
                    tasks.Add(view.PlayDealAnimationAsync(deckStartPos, targetPos));
                }

                view.transform.SetParent(handContainer);
            }

            var removeList = new List<int>();
            foreach (var kv in handViews)
                if (!newIds.Contains(kv.Key))
                    removeList.Add(kv.Key);

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