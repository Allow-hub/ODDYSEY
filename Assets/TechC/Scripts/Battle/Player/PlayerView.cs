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

        public void NotifyHitTiming()
        {
            hitTimingTcs?.TrySetResult();
        }

        public void NotifyAttackFinished()
        {
            hitTimingTcs?.TrySetResult();
            attackFinishedTcs?.TrySetResult();
        }

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

        public async UniTask BeginAttackAnimationAsync(CardAnimationType animType)
        {
            hitTimingTcs = new UniTaskCompletionSource();
            attackFinishedTcs = new UniTaskCompletionSource();

            var (animHash, camData) = ResolveParams(animType);

            // ① カメラ切り替え＋ブレンド完了を待つ
            if (camData != null)
                await CameraManager.I.SwitchToAndWaitBlendAsync(camData.onAttackState);

            // ② アニメ開始
            animator?.SetBool(animHash, false);
            await UniTask.Yield();
            animator?.SetBool(animHash, true);

            // ③ カメラアニメ並列
            cameraTask = camData != null
                ? CameraManager.I.PlayAttackCameraAsync(camData)
                : UniTask.CompletedTask;

            // ④ ヒット判定フレームまで待つ
            await hitTimingTcs.Task;
        }

        public async UniTask WaitAttackFinishedAsync(
            CardAnimationType animType = CardAnimationType.Attack,
            bool skipCameraReturn = false)
        {
            await UniTask.WhenAll(attackFinishedTcs.Task, cameraTask);
            var (animHash, _) = ResolveParams(animType);
            animator?.SetBool(animHash, false);
            if (!skipCameraReturn)
                await CameraManager.I.ReturnToDefaultAsync();
        }

        public void PlayHitStopEffect() => HitStopManager.I.Play();

        public async UniTask PlayDamageAnimationAsync(bool isHit)
        {
            var type = isHit ? PlayerAnimationType.Hit : PlayerAnimationType.Miss;
            var task = WaitStateAsync(type);
            animator?.SetBool(isHit ? AnimUtil.HitHash : AnimUtil.MissHash, true);
            await task;
            CustomLogger.Info($"プレイヤー被ダメアニメーション完了 (isHit={isHit})", LogTagUtil.TagBattle);
            animator?.SetBool(isHit ? AnimUtil.HitHash : AnimUtil.MissHash, false);
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

        public void PlayAudio(SEID seId)
        {
            AudioManager.I?.PlaySE(seId);
        }
    }

    public enum PlayerAnimationType { Enter, Defeated, Attack, Damage, Hit, Miss }
}