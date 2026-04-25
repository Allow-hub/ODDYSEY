using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using TechC.ODDESEY.Core.Manager;
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
        [SerializeField] private AttackCameraData attackCameraData;
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
            animator?.SetBool(AnimUtil.EnterHash, true);
            await task;
            CustomLogger.Info($"敵出撃アニメーション完了", LogTagUtil.TagBattle);
            animator?.SetBool(AnimUtil.EnterHash, false);
        }

        /// <summary>
        /// 攻撃アニメーションを再生
        /// </summary>
        /// <returns></returns>
        public async UniTask PlayAttackAnimationAsync()
        {
            var task = WaitStateAsync(EnemyStateNotifier.StateType.Attack);
            animator?.SetBool(AnimUtil.AttackHash, true);
            var cameraTask = CameraManager.I.PlayAttackCameraAsync(attackCameraData);
            await task;
            await cameraTask;
            CustomLogger.Info($"プレイヤー攻撃アニメーション完了", LogTagUtil.TagBattle);
            animator?.SetBool(AnimUtil.AttackHash, false);
        }

        /// <summary>
        /// ダメージを受けた時のアニメーション、成功した場合と失敗する場合がある
        /// </summary>
        /// <param name="isHit">攻撃が成功したかどうか</param>
        /// <returns></returns>
        public async UniTask PlayDamageAnimation(bool isHit)
        {
            var type = isHit
                        ? EnemyStateNotifier.StateType.Hit
                        : EnemyStateNotifier.StateType.Miss;

            var task = WaitStateAsync(type);

            animator?.SetBool(isHit ? AnimUtil.HitHash : AnimUtil.MissHash, true);

            await task;

            CustomLogger.Info($"プレイヤー被ダメージアニメーション完了 (isHit={isHit})", LogTagUtil.TagBattle);
            animator?.SetBool(isHit ? AnimUtil.HitHash : AnimUtil.MissHash, false);
        }

        public async UniTask PlayDefeatedAnimationAsync()
        {
            var task = WaitStateAsync(EnemyStateNotifier.StateType.Defeated);
            animator?.SetTrigger(AnimUtil.DefeatedHash);
            await task;
        }
    }
}