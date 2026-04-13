using System.Collections.Generic;
using Cysharp.Threading.Tasks;
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
        private static readonly int EnterHash = Animator.StringToHash("Enter");
        private static readonly int DamageHash = Animator.StringToHash("Damage");
        private static readonly int DefeatedHash = Animator.StringToHash("Defeated");
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

        public void PlayDamageAnimation()
        {
            animator.SetTrigger(DamageHash);
        }

        public async UniTask PlayDefeatedAnimationAsync()
        {
            var task = WaitStateAsync(EnemyStateNotifier.StateType.Defeated);
            animator.SetTrigger(DefeatedHash);
            await task;
        }
    }
}