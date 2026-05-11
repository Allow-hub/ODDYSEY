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

        [Header("カメラデータ")]
        [SerializeField] private AttackCameraData attackCameraData;
        [SerializeField] private AttackCameraData multiAttackCameraData;
        [SerializeField] private AttackCameraData specialCameraData;

        private Dictionary<EnemyStateNotifier.StateType, List<UniTaskCompletionSource>> waiters = new();
        private UniTaskCompletionSource hitTimingTcs;
        private UniTaskCompletionSource attackFinishedTcs;
        private UniTask cameraTask;

        private void Awake() => animator = GetComponent<Animator>();
        public void Setup(EnemyData data) { }

        // ─── Animation Event から呼ぶ ──────────────────────────────────────

        public void NotifyHitTiming()
        {
            Debug.Log($"[Enemy] NotifyHitTiming at {Time.realtimeSinceStartup:F3}");
            hitTimingTcs?.TrySetResult();
        }

        // ─── EnemyStateNotifier から呼ばれる ──────────────────────────────

        public void NotifyStateFinished(EnemyStateNotifier.StateType type)
        {
            if (type == EnemyStateNotifier.StateType.Attack)
            {
                Debug.Log($"[Enemy] NotifyStateFinished(Attack) at {Time.realtimeSinceStartup:F3}");
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
                Debug.Log($"[Enemy] カメラブレンド開始 at {Time.realtimeSinceStartup:F3}");
                await CameraManager.I.SwitchToAndWaitBlendAsync(camData.onAttackState);
                Debug.Log($"[Enemy] カメラブレンド完了 → アニメ開始 at {Time.realtimeSinceStartup:F3}");
            }

            animator?.SetBool(animHash, false);
            await UniTask.Yield();
            animator?.SetBool(animHash, true); Debug.Log($"[Enemy] SetBool({animHash}, true) at {Time.realtimeSinceStartup:F3}");

            cameraTask = camData != null
                ? CameraManager.I.PlayAttackCameraAsync(camData)
                : UniTask.CompletedTask;

            // タイムアウト付きで待つ（Animation Event が来なくても詰まらない）
            var timeout = UniTask.Delay(System.TimeSpan.FromSeconds(5f), ignoreTimeScale: true);
            var hit = hitTimingTcs.Task;

            int index = await UniTask.WhenAny(hit, timeout);
            if (index == 1)
                Debug.LogWarning($"[Enemy] HitTiming タイムアウト（Animation Event が来なかった）at {Time.realtimeSinceStartup:F3}");
            else
                Debug.Log($"[Enemy] HitTiming到達 at {Time.realtimeSinceStartup:F3}");
        }

        public async UniTask WaitAttackFinishedAsync(
            CardAnimationType animType = CardAnimationType.Attack)
        {
            await UniTask.WhenAll(attackFinishedTcs.Task, cameraTask);
            var (animHash, _) = ResolveParams(animType);
            Debug.Log($"[Enemy] 攻撃アニメ完了 ({animType}) at {Time.realtimeSinceStartup:F3}");
            animator?.SetBool(animHash, false);
            await CameraManager.I.ReturnToDefaultAsync();
            Debug.Log($"[Enemy] Default復帰完了 at {Time.realtimeSinceStartup:F3}");
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

        [ContextMenu("Defeat")]
        public void TestPlayDefeatedAnimation()
        {
            PlayDefeatedAnimationAsync().Forget();
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
    }
}