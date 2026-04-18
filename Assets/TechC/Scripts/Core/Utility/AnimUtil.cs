using UnityEngine;

namespace TechC.ODDESEY.Core.Util
{
    /// <summary>
    /// aninmator のパラメータハッシュなどをまとめたユーティリティクラス。
    /// </summary>
    public static class AnimUtil
    {
        public static readonly int EnterHash = Animator.StringToHash("Enter");
        public static readonly int AttackHash = Animator.StringToHash("Attack");
        public static readonly int HitHash = Animator.StringToHash("Hit");
        public static readonly int MissHash = Animator.StringToHash("Miss");
        public static readonly int DefeatedHash = Animator.StringToHash("Defeated");
    }
}
