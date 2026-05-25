using TechC.ODDESEY.Util;
using TechC.VBattle.Core.Extensions;
using UnityEngine;

namespace TechC.ODDESEY.Battle
{
    /// <summary>
    /// E-11「ノイズ・インジェクト / ノイズ注入」エフェクト。敵専用。
    ///
    /// 効果：
    ///   命中時にプレイヤーの捨て札にノイズカードを injectCount 枚追加する。
    ///   次のシャッフル時にデッキに混入し、手札密度を下げる。
    ///   ノイズは砕くと少量のゲージになるが、プレイしても何も起こらない。
    ///
    /// 黒ノイズ版は BlackNoiseInjectEffect がこのクラスを継承して
    ///   注入するカードを差し替える。
    ///
    /// カード設定値（Inspector）:
    ///   ProbabilityMin : 0.70
    ///   ProbabilityMax : 0.90
    ///   InjectCount    : 2
    ///   NoiseCardData  : ノイズ CardData をアサイン
    /// </summary>
    [CreateAssetMenu(menuName = "ODDESEY/CardEffect/NoiseInject")]
    public class NoiseInjectEffect : CardEffectBase
    {
        [Header("注入設定")]
        [Tooltip("捨て札に追加するノイズカードの枚数")]
        public int InjectCount = 2;

        [Tooltip("注入するノイズカードの CardData")]
        public CardData NoiseCardData;

        // 注入枚数で決まるので値の強化は無効
        public override bool CanBoostValue => false;

        public override void RollValue(EffectSlot slot, bool isHotMode)
        {
            slot.RolledProbability = isHotMode
                ? ProbabilityMax
                : Random.Range(ProbabilityMin, ProbabilityMax);

            // Value は注入枚数（表示用）
            slot.Value = InjectCount;
            slot.ValueRange = (InjectCount, InjectCount);
        }

        public override void Execute(EffectContext context, EffectExecutionState state, int effectIndex)
        {
            var instance = context.Source;

            bool isHit = instance.TryExecuteEffect(effectIndex);
            state.PreviousEffectHadHitCheck = true;
            state.PreviousEffectHit = isHit;

            if (!isHit)
            {
                context.Result.IsHit = false;
                return;
            }

            var cardToInject = GetNoiseCard();
            if (cardToInject == null)
            {
                CustomLogger.Warning(
                    "[NoiseInject] NoiseCardData が未設定です",
                    LogTagUtil.TagCard);
                return;
            }

            // 敵カードがプレイヤーの捨て札に注入する
            // AddToDiscard は常にプレイヤーの discardPile に追加する
            int count = instance.GetEffectiveValue(effectIndex);
            for (int i = 0; i < count; i++)
                context.Logic.AddToDiscard(cardToInject);

            context.Result.IsHit = true;
            context.Result.SetExtra(ResultKeys.NoiseInjected, count);

            CustomLogger.Info(
                $"[ノイズ注入] プレイヤーの捨て札に {cardToInject.CardName} × {count} 追加",
                LogTagUtil.TagCard);
        }

        /// <summary>
        /// 注入するカードを返す。
        /// 黒ノイズ版はここをオーバーライドして差し替える。
        /// </summary>
        protected virtual CardData GetNoiseCard() => NoiseCardData;
    }
}