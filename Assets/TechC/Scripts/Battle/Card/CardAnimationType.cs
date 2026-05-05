namespace TechC.ODDESEY.Battle
{
    /// <summary>
    /// カードが解決されたときに再生するアニメーションの種類。
    /// CardData に設定し、CardResolveResult 経由で View に渡す。
    ///
    /// プレイヤー・敵の両方で使用する。
    /// 対応するアニメーションは PlayerView / EnemyView 側で実装する。
    /// </summary>
    public enum CardAnimationType
    {
        /// <summary>通常攻撃（デフォルト）</summary>
        Attack,

        /// <summary>複数回攻撃（連続攻撃・連鎖攻撃カード用）</summary>
        MultiAttack,

        /// <summary>防御・バフ系カード用</summary>
        Defense,

        /// <summary>カウンター・特殊攻撃系カード用</summary>
        Special,
    }
}