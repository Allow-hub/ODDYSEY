using UnityEngine;

namespace TechC.ODDESEY.Battle
{
    public class EnemyStateNotifier : StateMachineBehaviour
    {
        public enum StateType
        {
            Enter,
            Defeated,
            Attack,
            Damage
        }

        public StateType stateType;

        public override void OnStateExit(Animator animator, AnimatorStateInfo stateInfo, int layerIndex)
        {
            var view = animator.GetComponent<EnemyView>();
            view?.NotifyStateFinished(stateType);
        }
    }
}