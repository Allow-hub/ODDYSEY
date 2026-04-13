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

        /// <summary>手札に来た時点で確定した基礎ダメージ</summary>
        public int BaseDamage { get; private set; }

        /// <summary>運ゲージ消費で上乗せされたダメージボーナス（上限なし）</summary>
        public int BonusDamage { get; set; }

        /// <summary>実効ダメージ（基礎 + ボーナス）</summary>
        public int EffectiveDamage => BaseDamage + BonusDamage;

        /// <summary>手札に来たタイミングでダメージをロールする</summary>
        public void RollDamage(bool isHotMode)
        {
            BaseDamage = isHotMode ? DamageMax : Random.Range(DamageMin, DamageMax + 1);
        }
    }
}