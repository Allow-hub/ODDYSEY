using UnityEngine;

namespace TechC.ODDESEY.Battle
{
    /// <summary>
    /// EnemyView の Animator ステート完了を通知するための StateMachineBehaviour。
    /// 各ステートの完了を EnemyView に通知し、EnemyView は待機している UniTaskCompletionSource に完了を伝える。
    /// </summary>
    public class EnemyStateNotifier : StateMachineBehaviour
    {
        public enum StateType
        {
            Enter,//登場
            Defeated,//倒されたとき
            Attack,//攻撃実行
            AttackHit,//攻撃ヒット時
            Hit,//攻撃がヒットしたとき
            Miss//攻撃がミスしたとき
        }

        public StateType stateType;

        /// <summary>
        /// ステート終了時に呼び出される。
        /// </summary>
        /// <param name="animator"></param>
        /// <param name="stateInfo"></param>
        /// <param name="layerIndex"></param>
        public override void OnStateExit(Animator animator, AnimatorStateInfo stateInfo, int layerIndex)
        {
            var view = animator.GetComponent<EnemyView>();
            view?.NotifyStateFinished(stateType);
        }
    }
}