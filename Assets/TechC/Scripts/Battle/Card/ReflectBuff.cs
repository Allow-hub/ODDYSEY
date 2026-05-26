namespace TechC.ODDESEY.Battle
{
    /// <summary>
    /// 反撃バフ状態。保持者が攻撃を受けたとき、攻撃者に反撃ダメージを与える。
    ///
    /// ライフサイクル：
    ///   付与されたターン → EndTurn で appliedThisTurn を落とすだけ（カウント維持）
    ///   次のターン中に発動 → count=0 で即失効
    ///   発動しなかった場合 → 次のターン EndTurn で count-- → 失効
    /// </summary>
    public class ReflectBuff
    {
        public int Damage { get; }

        /// <summary>true = プレイヤーが保持（被弾時に敵へ反撃）、false = 敵が保持（被弾時にプレイヤーへ反撃）</summary>
        public bool IsOnPlayer { get; }

        private int count = 1;
        private bool appliedThisTurn = true;

        public bool IsExpired => count <= 0;

        public ReflectBuff(int damage, bool isOnPlayer)
        {
            Damage = damage;
            IsOnPlayer = isOnPlayer;
        }

        /// <summary>
        /// 保持者が攻撃を受けたとき呼ぶ。発動すれば攻撃者にダメージを与えカウントをゼロにする。
        /// </summary>
        /// <returns>発動したか</returns>
        public bool TryTrigger(CardResolveResult result, BattleLogic logic)
        {
            if (count <= 0) return false;

            if (IsOnPlayer)
                logic.TakeEnemyDamage(Damage, result);   // プレイヤー反撃 → 敵にダメージ
            else
                logic.TakePlayerDamage(Damage, result);  // 敵反撃 → プレイヤーにダメージ

            count = 0;
            return true;
        }

        /// <summary>
        /// ターン終了時に呼ぶ。付与されたターンは初回をスキップし、次回以降カウントを1減らす。
        /// </summary>
        public void TickTurnEnd()
        {
            if (appliedThisTurn)
            {
                appliedThisTurn = false;
                return;
            }
            count--;
        }
    }
}
