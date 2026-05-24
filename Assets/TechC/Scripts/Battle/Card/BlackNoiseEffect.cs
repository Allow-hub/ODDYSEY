using TechC.ODDESEY.Util;
using TechC.VBattle.Core.Extensions;
using UnityEngine;

namespace TechC.ODDESEY.Battle
{
    /// <summary>
    /// 黒ノイズカードのエフェクト。NoiseEffect を継承。
    ///
    /// プレイしても何も起こらない（ノイズと同じ）。
    /// 砕いたときにHPを失う（BattleController が CardBrokenEvent で処理）。
    /// 砕いたときのゲージはノイズより多く得られる（CardData.LuckConversionRate で設定）。
    ///
    /// カード設定値（Inspector）:
    ///   LuckConversionRate : 15（砕いたとき15%のゲージを得る）
    ///   BreakSelfDamage    : 3（砕いたときに失うHP ← BattleController が参照）
    /// </summary>
    [CreateAssetMenu(menuName = "ODDESEY/CardEffect/BlackNoise")]
    public class BlackNoiseEffect : NoiseEffect
    {
        [Header("砕き時の自傷ダメージ")]
        [Tooltip("砕いたときに失うHP。BattleController の OnCardBroken で処理される。")]
        public int BreakSelfDamage = 3;
    }
}