using TechC.ODDESEY.Util;
using TechC.VBattle.Core.Extensions;
using UnityEngine;

namespace TechC.ODDESEY.Battle
{
    /// <summary>
    /// ノイズカードのエフェクト。
    ///
    /// プレイしても何も起こらない。
    /// 砕いたときの運ゲージ変換は CardData.LuckConversionRate で設定する。
    ///
    /// 黒ノイズは BlackNoiseEffect がこのクラスを継承して
    /// HP ロスを追加する。
    ///
    /// カード設定値（Inspector）:
    ///   LuckConversionRate : 5（砕いたとき5%のゲージを得る）
    /// </summary>
    [CreateAssetMenu(menuName = "ODDESEY/CardEffect/Noise")]
    public class NoiseEffect : CardEffectBase
    {
        // プレイしても何も起こらないので確率・値の強化は無効
        public override bool CanBoostProbability => false;
        public override bool CanBoostValue => false;

        public override void RollValue(EffectSlot slot, bool isHotMode)
        {
            slot.RolledProbability = 1f; // 確定発動（でも何もしない）
            slot.Value = 0;
            slot.ValueRange = (0, 0);
        }

        public override void Execute(EffectContext context, EffectExecutionState state, int effectIndex)
        {
            // プレイしても何も起こらない
            state.PreviousEffectHadHitCheck = false;
            context.Result.IsHit = false;

            OnNoiseResolved(context, state);

            CustomLogger.Info(
                $"[ノイズ] {context.Source.OriginalData.CardName} をプレイ：何も起こらない",
                LogTagUtil.TagCard);
        }

        /// <summary>
        /// 派生クラスがプレイ時の追加効果を実装するためのフック。
        /// BlackNoiseEffect はここで HP を削る。
        /// </summary>
        protected virtual void OnNoiseResolved(EffectContext context, EffectExecutionState state) { }
    }
}