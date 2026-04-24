using TechC.ODDESEY.Util;
using TechC.VBattle.Core.Extensions;
using UnityEngine;

namespace TechC.ODDESEY.Battle
{
    /// <summary>
    /// 直前の効果が外れたときにプレイヤー自身がダメージを受ける効果。
    /// 捨て身カードの「外れたとき自分に5固定ダメージ」を実現する。
    ///
    /// 使い方：CardData.Effects に DamageEffect の次に本クラスを配置する。
    ///   Effects[0] = DamageEffect  (60〜80%で 10〜25 ダメージ)
    ///   Effects[1] = SelfDamageEffect (外れたとき 5 固定)
    ///
    /// Execute() の呼び出し順は BattleLogic.ConfirmTurn() が Effect[0] → Effect[1] の順で
    /// 呼ぶ実装を前提とする。Result.IsHit が false のとき（直前が外れ）に自傷が発動する。
    ///
    /// ※ ProbabilityMin / ProbabilityMax は 1.0 固定にすること（判定不使用）。
    /// </summary>
    [CreateAssetMenu(menuName = "CardEffect/SelfDamage")]
    public class SelfDamageEffect : CardEffectBase
    {
        [Header("自傷ダメージ（固定値）")]
        public int SelfDamage = 5;

        public override void Execute(EffectContext context, int effectIndex)
        {
            // 直前の効果がヒットしていたら自傷しない
            // BattleLogic は Effects を 0 → n-1 の順に Execute するため
            // Result.IsHit が true ならメイン効果は成功している
            if (context.Result.IsHit) return;

            context.Logic.TakePlayerDamage(SelfDamage, context.Result);
            context.Result.SelfDamageDealt += SelfDamage;

            CustomLogger.Info(
                $"自傷ダメージ: {SelfDamage} Slot:{context.SlotIndex}",
                LogTagUtil.TagCard);
        }
    }
}