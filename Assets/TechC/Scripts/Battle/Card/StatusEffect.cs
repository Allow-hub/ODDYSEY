using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace TechC.ODDESEY.Battle
{
    public enum StatusType
    {
        Shock,      // 感電
        Burn,       // 燃焼
        Poison,     // 毒
    }

    /// <summary>
    /// 状態異常付与効果。
    /// 確率は固定（ProbabilityMin = ProbabilityMax = 1f）のものが多い。
    /// </summary>
    [CreateAssetMenu(menuName = "CardEffect/Status")]
    public class StatusEffect : CardEffectBase
    {
        public StatusType StatusType;

        [Tooltip("状態異常の持続ターン数")]
        public int Duration = 2;

        [Tooltip("スタック数（重ね掛け対応の場合）")]
        public int StackCount = 1;
    }
}