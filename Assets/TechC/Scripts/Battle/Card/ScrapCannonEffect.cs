using TechC.ODDESEY.Util;
using TechC.VBattle.Core.Extensions;
using UnityEngine;

namespace TechC.ODDESEY.Battle
{
    /// <summary>
    /// A「スクラップキャノン（破砕カウント砲）」エフェクト。
    ///
    /// 効果：
    ///   50〜70% の確率で、
    ///   「このターン中にカードを砕いた枚数 × damagePerScrap」のダメージを与える。
    ///   砕いた枚数が 0 のときはダメージ 0（ミス扱いではなくヒット0）。
    ///
    /// 砕いた枚数は BattleLogic.CurrentTurnScrapCount で管理する。
    ///
    /// カード設定値（Inspector）:
    ///   ProbabilityMin  : 0.50
    ///   ProbabilityMax  : 0.70
    ///   DamagePerScrap  : 5
    /// </summary>
    [CreateAssetMenu(menuName = "ODDESEY/CardEffect/ScrapCannon")]
    public class ScrapCannonEffect : CardEffectBase
    {
        [Header("1枚砕くごとのダメージ量")]
        public int DamagePerScrap = 5;

        [Header("ダメージ（表示用・ValueRange に使う）")]
        public int DamageMin = 5;
        public int DamageMax = 5;

        // 砕き枚数で決まるので値の強化は意味なし
        public override bool CanBoostValue => false;

        public override void RollValue(EffectSlot slot, bool isHotMode)
        {
            slot.RolledProbability = isHotMode
                ? ProbabilityMax
                : Random.Range(ProbabilityMin, ProbabilityMax);

            // Value は「1枚砕いた場合のダメージ」を基準値として保持
            slot.Value = isHotMode ? DamageMax : Random.Range(DamageMin, DamageMax + 1);
            slot.ValueRange = (DamageMin, DamageMax);
        }

        public override void Execute(EffectContext context, EffectExecutionState state, int effectIndex)
        {
            var instance = context.Source;

            // ─── 命中判定 ─────────────────────────────────────────────────
            bool isHit = instance.TryExecuteEffect(effectIndex);

            state.PreviousEffectHadHitCheck = true;
            state.PreviousEffectHit = isHit;

            if (!isHit)
            {
                context.Result.IsHit = false;
                return;
            }

            // ─── 砕いた枚数を BattleLogic から取得 ───────────────────────
            int scrapCount = context.Logic.CurrentTurnScrapCount;
            int finalDamage = scrapCount * DamagePerScrap;

            CustomLogger.Info(
                $"[スクラップキャノン] 砕いた枚数={scrapCount} × {DamagePerScrap} = {finalDamage}ダメージ",
                LogTagUtil.TagCard);

            context.Result.IsHit = true;

            if (finalDamage <= 0)
            {
                // 砕いていない場合はヒットしたがダメージ0
                CustomLogger.Info("[スクラップキャノン] 砕いたカードなし → ダメージ0", LogTagUtil.TagCard);
                return;
            }

            // ─── ダメージ適用 ─────────────────────────────────────────────
            if (context.IsEnemy)
                context.Logic.TakePlayerDamage(finalDamage, context.Result);
            else
                context.Logic.TakeEnemyDamage(finalDamage, context.Result);

            context.Result.DamageDealt += finalDamage;
            state.TotalDamageToEnemy += finalDamage;
        }
    }
}