using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using TechC.ODDESEY.Core.Manager;
using TechC.ODDESEY.Core.Util;
using TechC.ODDESEY.Util;
using TechC.VBattle.Core.Extensions;
using UnityEngine;

namespace TechC.ODDESEY.Battle
{
    public class EnemyView : MonoBehaviour
    {
        private Animator animator;
        [SerializeField] private AttackCameraData attackCameraData;

        private Dictionary<EnemyStateNotifier.StateType, List<UniTaskCompletionSource>> waiters = new();
        private UniTaskCompletionSource hitTimingTcs;
        private UniTaskCompletionSource attackFinishedTcs;
        private UniTask cameraTask; // カメラタスクを保持して WaitAttackFinishedAsync で待つ

        private void Awake() => animator = GetComponent<Animator>();

        public void Setup(EnemyData data) { }

        // ─── Animation Event から呼ぶ ──────────────────────────────────────

        public void NotifyHitTiming() => hitTimingTcs?.TrySetResult();

        // ─── EnemyStateNotifier から呼ばれる ──────────────────────────────

        public void NotifyStateFinished(EnemyStateNotifier.StateType type)
        {
            if (type == EnemyStateNotifier.StateType.Attack)
            {
                attackFinishedTcs?.TrySetResult();
                return;
            }

            if (!waiters.TryGetValue(type, out var list)) return;
            foreach (var tcs in list) tcs.TrySetResult();
            list.Clear();
        }

        private UniTask WaitStateAsync(EnemyStateNotifier.StateType type)
        {
            var tcs = new UniTaskCompletionSource();
            if (!waiters.ContainsKey(type))
                waiters[type] = new List<UniTaskCompletionSource>();
            waiters[type].Add(tcs);
            return tcs.Task;
        }

        // ─── 公開API ──────────────────────────────────────────────────────

        /// <summary>
        /// 攻撃アニメを開始し、ヒット判定フレームまで待機する。
        /// カメラは並列で走らせ、WaitAttackFinishedAsync で一緒に待つ。
        /// </summary>
        public async UniTask BeginAttackAnimationAsync()
        {
            hitTimingTcs = new UniTaskCompletionSource();
            attackFinishedTcs = new UniTaskCompletionSource();

            animator?.SetBool(AnimUtil.AttackHash, true);

            cameraTask = CameraManager.I.PlayAttackCameraAsync(attackCameraData);

            await hitTimingTcs.Task;
        }

        /// <summary>
        /// 攻撃アニメとカメラ演出の両方が完了するまで待つ。
        /// </summary>
        public async UniTask WaitAttackFinishedAsync()
        {
            await UniTask.WhenAll(attackFinishedTcs.Task, cameraTask);
            CustomLogger.Info($"敵攻撃アニメーション完了", LogTagUtil.TagBattle);
            animator?.SetBool(AnimUtil.AttackHash, false);
        }

        /// <summary>
        /// 被ダメアニメーションを再生する。BattleView から Forget() で呼ぶ。
        /// </summary>
        public async UniTask PlayDamageAnimationAsync(bool isHit)
        {
            var type = isHit ? EnemyStateNotifier.StateType.Hit : EnemyStateNotifier.StateType.Miss;
            var task = WaitStateAsync(type);
            animator?.SetBool(isHit ? AnimUtil.HitHash : AnimUtil.MissHash, true);
            await task;
            CustomLogger.Info($"敵被ダメアニメーション完了 (isHit={isHit})", LogTagUtil.TagBattle);
            animator?.SetBool(isHit ? AnimUtil.HitHash : AnimUtil.MissHash, false);
        }

        public async UniTask PlayEnterAnimationAsync()
        {
            var task = WaitStateAsync(EnemyStateNotifier.StateType.Enter);
            animator?.SetBool(AnimUtil.EnterHash, true);
            await task;
            CustomLogger.Info($"敵出撃アニメーション完了", LogTagUtil.TagBattle);
            animator?.SetBool(AnimUtil.EnterHash, false);
        }

        public async UniTask PlayDefeatedAnimationAsync()
        {
            var task = WaitStateAsync(EnemyStateNotifier.StateType.Defeated);
            animator?.SetTrigger(AnimUtil.DefeatedHash);
            await task;
        }
    }
}