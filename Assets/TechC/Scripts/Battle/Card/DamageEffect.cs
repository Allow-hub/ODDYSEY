using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace TechC.ODDESEY.Battle
{
    /// <summary>
    /// 確率付きダメージ効果。
    /// 運ゲージでダメージに上乗せできる（上限なし）。
    /// </summary>
    [CreateAssetMenu(menuName = "CardEffect/Damage")]
    public class DamageEffect : CardEffectBase
    {
        [Header("ダメージ（範囲）")]
        public int DamageMin = 3;
        public int DamageMax = 6;
    }
}