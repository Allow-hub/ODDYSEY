using TechC.ODDESEY.Util;
using TechC.VBattle.Core.Extensions;
using UnityEngine;

namespace TechC.ODDESEY.Battle
{
    /// <summary>
    /// 確率付きダメージ効果。
    /// 運ゲージでダメージに上乗せできる（上限なし）。
    ///
    /// 配置ボーナス：
    ///   UsePositionBonus = true のとき、RequiredSlotIndex と一致するスロットに
    ///   置かれた場合のみ PositionBonusDamage を追加で与える。
    ///   先制特化カードの「左端配置で +5 ダメージ」を ScriptableObject だけで設定できる。
    /// </summary>
    [CreateAssetMenu(menuName = "CardEffect/Damage")]
    public class DamageEffect : CardEffectBase
    {
        [Header("ダメージ（範囲）")]
        public int DamageMin = 3;
        public int DamageMax = 6;

        [Header("配置ボーナス")]
        [Tooltip("true のとき RequiredSlotIndex に配置されていれば追加ダメージを与える")]
        public bool UsePositionBonus = false;

        [Tooltip("ボーナスが発動するスロットインデックス（0 = 左端）")]
        public int RequiredSlotIndex = 0;

        [Tooltip("条件一致時に追加するダメージ量")]
        public int PositionBonusDamage = 5;

        public override void Execute(EffectContext context, int effectIndex)
        {
            var instance = context.Source;

            bool isHit = instance.TryExecuteEffect(effectIndex);

            if (!isHit)
            {
                context.Result.IsHit = false;
                return;
            }

            int damage = instance.GetEffectiveValue(effectIndex);
  

            // 配置ボーナス判定
            if (UsePositionBonus && context.SlotIndex == RequiredSlotIndex)
            {
                damage += PositionBonusDamage;
                CustomLogger.Info(
                    $"配置ボーナス発動: +{PositionBonusDamage} (Slot:{context.SlotIndex})",
                    LogTagUtil.TagCard);
            }

            if (context.IsEnemy)
                context.Logic.TakePlayerDamage(damage, context.Result);
            else
                context.Logic.TakeEnemyDamage(damage, context.Result);

            context.Result.IsHit = true;
            context.Result.DamageDealt += damage;
        }

        public override void RollValue(EffectSlot slot, bool isHotMode)
        {
            slot.RolledProbability = isHotMode
                ? ProbabilityMax
                : Random.Range(ProbabilityMin, ProbabilityMax);

            slot.Value = isHotMode
                ? DamageMax
                : Random.Range(DamageMin, DamageMax + 1);
        }
    }
}