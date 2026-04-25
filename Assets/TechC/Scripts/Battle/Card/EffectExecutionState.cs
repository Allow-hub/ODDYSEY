namespace TechC.ODDESEY.Battle
{
    /// <summary>
    /// カード1枚の解決中に各Effectが読み書きする一時状態。
    /// Effect間の通信（前の効果がヒットしたか、など）はここを経由する。
    ///
    /// 設計意図：
    ///   EffectContext は「外部リソースへのアクセス手段」（BattleLogic, CardInstance など）。
    ///   EffectExecutionState は「解決フロー内の可変状態」。
    ///   両者を分けることで、Effectが「何にアクセスできるか」と「何を知っているか」を明確に分離する。
    /// </summary>
    public class EffectExecutionState
    {
        // ─── 直前のEffect結果 ───────────────────────────────────────
        /// <summary>直前のEffectがヒット判定を持っていたか（確率判定をそもそも行ったか）</summary>
        public bool PreviousEffectHadHitCheck { get; set; } = false;

        /// <summary>直前のEffectがヒットしたか。SelfDamageEffect など「前の外れに反応する」効果が参照する。</summary>
        public bool PreviousEffectHit { get; set; } = false;

        // ─── 累積値 ──────────────────────────────────────────────────
        /// <summary>このカードで与えた敵へのダメージ累計</summary>
        public int TotalDamageToEnemy { get; set; } = 0;

        /// <summary>このカードで受けた自傷ダメージ累計</summary>
        public int TotalSelfDamage { get; set; } = 0;

        /// <summary>クリティカルが発生したか</summary>
        public bool IsCritical { get; set; } = false;

        /// <summary>軽減率（0〜100）。DefenseEffectが書き込み、BattleLogicが参照する。</summary>
        public int DamageReductionRate { get; set; } = 0;
    }
}