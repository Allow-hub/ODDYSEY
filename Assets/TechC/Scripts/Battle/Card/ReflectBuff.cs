using TechC.ODDESEY.Util;
using TechC.VBattle.Core.Extensions;

namespace TechC.ODDESEY.Battle
{
    /// <summary>
    /// 反撃バフ。保持者が攻撃を受けたとき攻撃者に反撃ダメージを与える。
    ///
    /// ライフサイクル：
    ///   付与されたターンの EndTurn → appliedThisTurn を落とすだけ（カウント維持）
    ///   次のターン中に発動 → count=0 で即失効
    ///   発動しなかった場合 → 次のターン EndTurn で count-- → 失効
    /// </summary>
    public class ReflectBuff : IOnTakeDamageBuff
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

        public void OnTakeDamage(CardResolveResult result, BattleLogic logic)
        {
            if (count <= 0) return;

            if (IsOnPlayer)
                logic.TakeEnemyDamage(Damage, result);   // プレイヤー反撃 → 敵にダメージ
            else
                logic.TakePlayerDamage(Damage, result);  // 敵反撃 → プレイヤーにダメージ

            result.SetExtra(ResultKeys.ReflectTriggered, true);
            result.SetExtra(ResultKeys.ReflectDamage, Damage);

            count = 0;

            CustomLogger.Info(
                $"{(IsOnPlayer ? "プレイヤー" : "敵")}反撃バフ発動: {(IsOnPlayer ? "敵" : "プレイヤー")}に{Damage}ダメージ",
                LogTagUtil.TagCard);
        }

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
