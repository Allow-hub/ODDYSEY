using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using TechC.ODDESEY.Core.Manager;
using TechC.ODDESEY.Core.Util;
using TechC.ODDESEY.Util;
using TechC.VBattle.Core.Extensions;
using UnityEngine;

namespace TechC.ODDESEY.Battle
{
    public class PlayerView : MonoBehaviour
    {
        [SerializeField] private Animator animator;
        [SerializeField] private AttackCameraData attackCameraData;

        private Dictionary<PlayerAnimationType, List<UniTaskCompletionSource>> waiters = new();
        private UniTaskCompletionSource hitTimingTcs;
        private UniTaskCompletionSource attackFinishedTcs;
        private UniTask cameraTask; // カメラタスクを保持して WaitAttackFinishedAsync で待つ

        // ─── Animation Event から呼ぶ ──────────────────────────────────────

        public void NotifyHitTiming() => hitTimingTcs?.TrySetResult();
        public void NotifyAttackFinished() => attackFinishedTcs?.TrySetResult();
        public void NotifyHitFinished() => NotifyStateFinished(PlayerAnimationType.Hit);
        public void NotifyMissFinished() => NotifyStateFinished(PlayerAnimationType.Miss);

        // ─── ステート完了通知 ──────────────────────────────────────────────

        public void NotifyStateFinished(PlayerAnimationType type)
        {
            if (!waiters.TryGetValue(type, out var list)) return;
            waiters.Remove(type);
            foreach (var tcs in list) tcs.TrySetResult();
        }

        private UniTask WaitStateAsync(PlayerAnimationType type)
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
        /// カメラタスクは並列で走らせ、WaitAttackFinishedAsync で一緒に待つ。
        /// </summary>
        public async UniTask BeginAttackAnimationAsync()
        {
            hitTimingTcs = new UniTaskCompletionSource();
            attackFinishedTcs = new UniTaskCompletionSource();

            animator?.SetBool(AnimUtil.AttackHash, true);

            // カメラは並列で開始し、フィールドに保持しておく
            cameraTask = CameraManager.I.PlayAttackCameraAsync(attackCameraData);

            await hitTimingTcs.Task; // ヒット瞬間まで待つ
        }

        /// <summary>
        /// 攻撃アニメとカメラ演出の両方が完了するまで待つ。
        /// </summary>
        public async UniTask WaitAttackFinishedAsync()
        {
            // アニメ完了とカメラ演出完了を両方待つ
            await UniTask.WhenAll(attackFinishedTcs.Task, cameraTask);
            CustomLogger.Info($"プレイヤー攻撃アニメーション完了", LogTagUtil.TagBattle);
            animator?.SetBool(AnimUtil.AttackHash, false);
        }

        /// <summary>
        /// 被ダメアニメーションを再生する。BattleView から Forget() で呼ぶ。
        /// </summary>
        public async UniTask PlayDamageAnimationAsync(bool isHit)
        {
            var type = isHit ? PlayerAnimationType.Hit : PlayerAnimationType.Miss;
            var task = WaitStateAsync(type);
            animator?.SetBool(isHit ? AnimUtil.HitHash : AnimUtil.MissHash, true);
            await task;
            CustomLogger.Info($"プレイヤー被ダメアニメーション完了 (isHit={isHit})", LogTagUtil.TagBattle);
            animator?.SetBool(isHit ? AnimUtil.HitHash : AnimUtil.MissHash, false);
        }
    }

    public enum PlayerAnimationType { Enter, Defeated, Attack, Damage, Hit, Miss }
}