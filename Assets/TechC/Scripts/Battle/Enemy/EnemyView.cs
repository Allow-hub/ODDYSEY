using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using TechC.ODDESEY.Core.Util;
using TechC.ODDESEY.Util;
using TechC.VBattle.Core.Extensions;
using UnityEngine;

namespace TechC.ODDESEY.Battle
{
    /// <summary>
    /// 敵1体の表示を管理する。
    /// EnemyData から生成され、Animator のステート完了を UniTask で待てる口を提供する。
    /// </summary>
    public class EnemyView : MonoBehaviour
    {
        private Animator animator;
        private Dictionary<EnemyStateNotifier.StateType, List<UniTaskCompletionSource>> waiters
             = new();

        private void Awake()
        {
            animator = GetComponent<Animator>();
        }

        /// <summary>
        /// EnemyData を元に初期化する。BattleView から呼ぶ。
        /// </summary>
        public void Setup(EnemyData data)
        {
        }

        /// <summary>
        /// Animator のステート完了を待っている UniTaskCompletionSource に完了通知を送る。
        /// </summary>
        /// <param name="type"></param>
        public void NotifyStateFinished(EnemyStateNotifier.StateType type)
        {
            if (!waiters.TryGetValue(type, out var list)) return;

            foreach (var tcs in list)
                tcs.TrySetResult();

            list.Clear();
        }

        /// <summary>
        /// 指定したステートが完了するまで待機する UniTask を返す。
        /// </summary>
        /// <param name="type"></param>
        /// <returns></returns>
        private UniTask WaitStateAsync(EnemyStateNotifier.StateType type)
        {
            var tcs = new UniTaskCompletionSource();

            if (!waiters.ContainsKey(type))
                waiters[type] = new List<UniTaskCompletionSource>();

            waiters[type].Add(tcs);

            return tcs.Task;
        }

        public async UniTask PlayEnterAnimationAsync()
        {
            var task = WaitStateAsync(EnemyStateNotifier.StateType.Enter);
            // animator.SetTrigger(EnterHash);
            await task;
            CustomLogger.Info($"敵出撃アニメーション完了", LogTagUtil.TagBattle);
        }

        /// <summary>
        /// カードの効果に応じたアニメーションを再生する。例えば攻撃なら攻撃モーションを再生し、外した場合は外しモーションを再生する。
        /// </summary>
        /// <param name="isHit"></param>
        /// <returns></returns>
        public async UniTask PlayAttackAnimationAsync(bool isHit)
        {
            var type = isHit
                ? EnemyStateNotifier.StateType.Hit
                : EnemyStateNotifier.StateType.Miss;

            var task = WaitStateAsync(type);

            animator.SetBool(isHit ? AnimUtil.HitHash : AnimUtil.MissHash, true);

            await task;

            CustomLogger.Info($"プレイヤー攻撃アニメーション完了 (isHit={isHit})", LogTagUtil.TagBattle);
            animator.SetBool(isHit ? AnimUtil.HitHash : AnimUtil.MissHash, false);
        }

        public void PlayDamageAnimation()
        {
            animator.SetTrigger(AnimUtil.DamageHash);
        }

        public async UniTask PlayDefeatedAnimationAsync()
        {
            var task = WaitStateAsync(EnemyStateNotifier.StateType.Defeated);
            animator.SetTrigger(AnimUtil.DefeatedHash);
            await task;
        }
    }
}