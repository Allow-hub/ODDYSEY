using UnityEngine;

namespace TechC.ODDESEY.Battle
{
    /// <summary>
    /// E-12「ブラックノイズ・インジェクト / 黒ノイズ注入」エフェクト。敵専用。
    /// NoiseInjectEffect を継承。
    ///
    /// 効果：
    ///   命中時にプレイヤーの捨て札に黒ノイズカードを追加する。
    ///   黒ノイズはノイズより多くのゲージを得られるが
    ///   プレイすると HP を少し失う。
    ///
    /// カード設定値（Inspector）:
    ///   ProbabilityMin    : 0.60
    ///   ProbabilityMax    : 0.80
    ///   InjectCount       : 1
    ///   BlackNoiseCardData: 黒ノイズ CardData をアサイン
    ///   （NoiseCardData は使わない）
    /// </summary>
    [CreateAssetMenu(menuName = "ODDESEY/CardEffect/BlackNoiseInject")]
    public class BlackNoiseInjectEffect : NoiseInjectEffect
    {
        [Header("黒ノイズ設定")]
        [Tooltip("注入する黒ノイズカードの CardData")]
        public CardData BlackNoiseCardData;

        protected override CardData GetNoiseCard() => BlackNoiseCardData;
    }
}
