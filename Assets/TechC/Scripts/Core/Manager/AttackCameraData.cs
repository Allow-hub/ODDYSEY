using UnityEngine;

namespace TechC.ODDESEY.Core.Manager
{
    [CreateAssetMenu(menuName = "Camera/AttackCameraData")]
    public class AttackCameraData : ScriptableObject
    {
        [Header("VCam切り替え")]
        public CameraState onAttackState = CameraState.BattleStart;
        [SerializeField] private string triggerName = "PlayerAttack";
        public int AnimTriggerHash => Animator.StringToHash(triggerName);
        public float returnDelay = 1f;
    }
}
