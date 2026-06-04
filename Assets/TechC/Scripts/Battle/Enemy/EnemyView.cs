using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using TechC.Core.Manager;
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

        [Header("カメラデータ")]
        [SerializeField] private AttackCameraData attackCameraData;
        [SerializeField] private AttackCameraData multiAttackCameraData;
        [SerializeField] private AttackCameraData specialCameraData;

        private Dictionary<EnemyStateNotifier.StateType, List<UniTaskCompletionSource>> waiters = new();
        private UniTaskCompletionSource hitTimingTcs;
        private UniTaskCompletionSource attackFinishedTcs;
        private UniTask cameraTask;
        private EnemyAudioData enemyAudioData;


        private void Awake() => animator = GetComponent<Animator>();
        public void Setup(EnemyData data)
        {
            enemyAudioData = data.AudioData;
        }

        // ─── Animation Event から呼ぶ ──────────────────────────────────────

        public void NotifyHitTiming()
        {
            hitTimingTcs?.TrySetResult();
        }

        // ─── EnemyStateNotifier から呼ばれる ──────────────────────────────

        public void NotifyStateFinished(EnemyStateNotifier.StateType type)
        {
            if (type == EnemyStateNotifier.StateType.Attack)
            {
                hitTimingTcs?.TrySetResult();
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

        // EnemyView.cs
        public async UniTask BeginAttackAnimationAsync(CardAnimationType animType)
        {
            hitTimingTcs = new UniTaskCompletionSource();
            attackFinishedTcs = new UniTaskCompletionSource();

            var (animHash, camData) = ResolveParams(animType);

            if (camData != null)
            {
                await CameraManager.I.SwitchToAndWaitBlendAsync(camData.onAttackState);
            }

            animator?.SetBool(animHash, false);
            await UniTask.Yield();
            animator?.SetBool(animHash, true); 

            cameraTask = camData != null
                ? CameraManager.I.PlayAttackCameraAsync(camData)
                : UniTask.CompletedTask;

            // タイムアウト付きで待つ（Animation Event が来なくても詰まらない）
            var timeout = UniTask.Delay(System.TimeSpan.FromSeconds(5f), ignoreTimeScale: true);
            var hit = hitTimingTcs.Task;

            int index = await UniTask.WhenAny(hit, timeout);
            // if (index == 1)
            //     Debug.LogWarning($"[Enemy] HitTiming タイムアウト（Animation Event が来なかった）at {Time.realtimeSinceStartup:F3}");
            // else
            //     Debug.Log($"[Enemy] HitTiming到達 at {Time.realtimeSinceStartup:F3}");
        }

        public async UniTask WaitAttackFinishedAsync(
            CardAnimationType animType = CardAnimationType.Attack,
            bool skipCameraReturn = false)
        {
            // ① 攻撃アニメ完了とカメラアニメ完了を待つ（タイムアウト付き）
            var timeout = UniTask.Delay(System.TimeSpan.FromSeconds(8f), ignoreTimeScale: true);
            var allTasks = UniTask.WhenAll(attackFinishedTcs.Task, cameraTask);
            var completedTask = await UniTask.WhenAny(allTasks, timeout);

            if (completedTask == 1) // timeout が完了した場合
            {
                CustomLogger.Warning(
                    $"敵攻撃完了がタイムアウト",
                    LogTagUtil.TagBattle);
            }

            // ② アニメハッシュをリセット
            var (animHash, _) = ResolveParams(animType);
            animator?.SetBool(animHash, false);

            // ③ カメラ復帰
            if (!skipCameraReturn)
                await CameraManager.I.ReturnToDefaultAsync();
        }

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

        private (int animHash, AttackCameraData camData) ResolveParams(CardAnimationType animType)
        {
            return animType switch
            {
                CardAnimationType.MultiAttack => (AnimUtil.MultiAttackHash, multiAttackCameraData ?? attackCameraData),
                CardAnimationType.Special => (AnimUtil.SpecialHash, specialCameraData ?? attackCameraData),
                CardAnimationType.Defense => (AnimUtil.DefenseHash, null),
                _ => (AnimUtil.AttackHash, attackCameraData),
            };
        }

        public void PlaySE(EnemyActionSEID seId)
        {
            if (enemyAudioData == null) return;
            AudioManager.I.PlayEnemySE(enemyAudioData, seId);
        }
    }
}