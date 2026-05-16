using UnityEngine;

namespace TechC.ODDESEY.Battle
{
    /// <summary>
    /// BattleStart アニメーションの完了を BattleView に通知する StateMachineBehaviour。
    /// Animator の BattleStart ステートの最後のフレームで OnBattleStartFinished を呼ぶ。
    ///
    /// 設定方法：
    ///   Animator の BattleStart ステートを選択
    ///   → Add Behaviour → BattleStartNotifier を追加
    /// </summary>
    public class BattleStartNotifier : StateMachineBehaviour
    {
        private bool notified = false;

        public override void OnStateEnter(Animator animator, AnimatorStateInfo stateInfo, int layerIndex)
        {
            notified = false;
        }

        public override void OnStateUpdate(Animator animator, AnimatorStateInfo stateInfo, int layerIndex)
        {
            // normalizedTime >= 1f でアニメ終了を検知
            if (!notified && stateInfo.normalizedTime >= 1f && !animator.IsInTransition(layerIndex))
            {
                notified = true;
                animator.GetComponent<BattleView>()?.NotifyBattleStartFinished();
            }
        }
    }
}
