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

        [Header("カメラデータ")]
        [SerializeField] private AttackCameraData attackCameraData;
        [SerializeField] private AttackCameraData multiAttackCameraData;
        [SerializeField] private AttackCameraData specialCameraData;

        private Dictionary<PlayerAnimationType, List<UniTaskCompletionSource>> waiters = new();
        private UniTaskCompletionSource hitTimingTcs;
        private UniTaskCompletionSource attackFinishedTcs;
        private UniTask cameraTask;

        // ─── Animation Event から呼ぶ ──────────────────────────────────────

        public void NotifyHitTiming()      => hitTimingTcs?.TrySetResult();
        public void NotifyAttackFinished() => attackFinishedTcs?.TrySetResult();
        public void NotifyHitFinished()    => NotifyStateFinished(PlayerAnimationType.Hit);
        public void NotifyMissFinished()   => NotifyStateFinished(PlayerAnimationType.Miss);

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
        /// CardAnimationType に応じた Animator パラメータとカメラを使う。
        /// </summary>
        public async UniTask BeginAttackAnimationAsync(
            CardAnimationType animType = CardAnimationType.Attack)
        {
            hitTimingTcs      = new UniTaskCompletionSource();
            attackFinishedTcs = new UniTaskCompletionSource();

            var (animHash, camData) = ResolveParams(animType);
            animator?.SetBool(animHash, true);
            cameraTask = CameraManager.I.PlayAttackCameraAsync(camData);

            await hitTimingTcs.Task;
        }

        /// <summary>
        /// 攻撃アニメとカメラ演出の両方が完了するまで待つ。
        /// </summary>
        public async UniTask WaitAttackFinishedAsync(
            CardAnimationType animType = CardAnimationType.Attack)
        {
            await UniTask.WhenAll(attackFinishedTcs.Task, cameraTask);
            var (animHash, _) = ResolveParams(animType);
            CustomLogger.Info($"プレイヤー攻撃アニメーション完了 ({animType})", LogTagUtil.TagBattle);
            animator?.SetBool(animHash, false);
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

        // ─── 内部処理 ────────────────────────────────────────────────────

        private (int animHash, AttackCameraData camData) ResolveParams(CardAnimationType animType)
        {
            return animType switch
            {
                CardAnimationType.MultiAttack => (AnimUtil.MultiAttackHash, multiAttackCameraData ?? attackCameraData),
                CardAnimationType.Special     => (AnimUtil.SpecialHash,     specialCameraData ?? attackCameraData),
                CardAnimationType.Defense     => (AnimUtil.DefenseHash,     null),
                _                             => (AnimUtil.AttackHash,      attackCameraData),
            };
        }
    }

    public enum PlayerAnimationType { Enter, Defeated, Attack, Damage, Hit, Miss }
}