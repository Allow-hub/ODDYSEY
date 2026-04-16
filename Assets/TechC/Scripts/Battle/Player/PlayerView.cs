using System.Collections;
using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using TechC.ODDESEY.Util;
using TechC.VBattle.Core.Extensions;
using UnityEngine;

namespace TechC.ODDESEY.Battle
{
    /// <summary>
    /// プレイヤーの見た目を管理する
    /// </summary>
    public class PlayerView : MonoBehaviour
    {
        [SerializeField] private Animator animator;
        private Dictionary<PlayerAnimationType, List<UniTaskCompletionSource>> waiters
            = new();

        /// <summary>
        /// Animator のステート完了を待っている UniTaskCompletionSource に完了通知を送る。
        /// </summary>
        /// <param name="type"></param>
        public void NotifyStateFinished(PlayerAnimationType type)
        {
            if (!waiters.TryGetValue(type, out var list)) return;

            waiters.Remove(type);

            foreach (var tcs in list)
                tcs.TrySetResult();
        }

        /// <summary>
        /// 指定したステートが完了するまで待機する UniTask を返す。
        /// </summary>
        /// <param name="type"></param>
        /// <returns></returns>
        private UniTask WaitStateAsync(PlayerAnimationType type)
        {
            var tcs = new UniTaskCompletionSource();

            if (!waiters.ContainsKey(type))
                waiters[type] = new List<UniTaskCompletionSource>();

            waiters[type].Add(tcs);

            return tcs.Task;
        }
        /// <summary>
        /// 攻撃アニメーションを再生する。isHit に応じてヒット・ミスのアニメーションを切り替える。
        /// </summary>
        /// <param name="isHit"></param>
        /// <returns></returns>
        public async UniTask PlayAttackAnimationAsync(bool isHit)
        {
            var type = isHit
                ? PlayerAnimationType.Hit
                : PlayerAnimationType.Miss;

            var task = WaitStateAsync(type);

            animator.SetBool(isHit ? "Hit" : "Miss", true);

            await task;

            CustomLogger.Info($"プレイヤー攻撃アニメーション完了 (isHit={isHit})", LogTagUtil.TagBattle);
            animator.SetBool(isHit ? "Hit" : "Miss", false);
        }
    }

    public enum PlayerAnimationType
    {
        Enter,
        Defeated,
        Attack,
        Damage,
        Hit,
        Miss
    }
}