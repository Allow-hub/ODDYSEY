using System.Collections;
using System.Collections.Generic;
using TechC.ODDESEY.Util;
using TechC.VBattle.Core.Extensions;
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

        public override void Execute(EffectContext context, int effectIndex)
        {
            var instance = context.Source;

            bool isHit = instance.TryExecuteEffect(effectIndex);

            if (!isHit)
            {
                context.Result.IsHit = false;
                return;
            }

            int damage = instance.GetEffectiveDamage(effectIndex);

            if (context.IsEnemy)
                context.Logic.TakePlayerDamage(damage, context.Result);
            else
                context.Logic.TakeEnemyDamage(damage, context.Result);

            context.Result.IsHit = true;
            context.Result.DamageDealt += damage;
        }
    }
}