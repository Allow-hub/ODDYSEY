using TechC.ODDESEY.Util;
using TechC.VBattle.Core.Extensions;
using UnityEngine;

namespace TechC.ODDESEY.Battle
{
    /// <summary>
    /// 確定ダメージ＋一定確率でダメージ倍率を適用するクリティカル効果。
    ///
    /// 設計：
    ///   1. 確定部分（BaseDamageMin〜Max）は RollValues() でロール済み。
    ///   2. クリティカル確率（ProbabilityMin〜Max）も RollValues() でロール済み。
    ///   3. Execute() で確定ダメージを与えたのち、クリティカル判定を追加で行う。
    ///
    /// ゲージ連携：
    ///   - ProbabilityMin/Max → GetEffectiveProbability() でクリ確率を操作（設計指針§3-2 準拠）
    ///   - BaseDamage → GetEffectiveDamage() で確定部分を操作可能（ゲージ投資の選択肢）
    /// </summary>
    [CreateAssetMenu(menuName = "CardEffect/CriticalDamage")]
    public class CriticalDamageEffect : CardEffectBase
    {
        [Header("確定ダメージ（範囲）")]
        public int BaseDamageMin = 2;
        public int BaseDamageMax = 3;

        [Header("クリティカル倍率")]
        [Tooltip("クリティカルヒット時にダメージにかける倍率")]
        public int CriticalMultiplier = 3;

        // ProbabilityMin / ProbabilityMax がクリティカル確率（30〜50% 等）として使われる。
        // CardEffectBase.ProbabilityMin/Max の説明を Inspector の Tooltip で補足すること。

        public override void Execute(EffectContext context, int effectIndex)
        {
            int baseDamage = context.Source.GetEffectiveDamage(effectIndex);
            bool isCritical = context.Source.TryExecuteEffect(effectIndex);

            int finalDamage = isCritical ? baseDamage * CriticalMultiplier : baseDamage;

            if (context.IsEnemy)
                context.Logic.TakePlayerDamage(finalDamage, context.Result);
            else
                context.Logic.TakeEnemyDamage(finalDamage, context.Result);

            context.Result.IsHit = true;
            context.Result.DamageDealt += finalDamage;
            context.Result.IsCritical = isCritical;

            CustomLogger.Info(
                $"クリティカル判定: baseDmg={baseDamage} crit={isCritical} finalDmg={finalDamage} Slot:{context.SlotIndex}",
                LogTagUtil.TagCard);
        }
    }
}