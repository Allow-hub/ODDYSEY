namespace TechC.ODDESEY.Battle
{
    /// <summary>
    /// バトル中に付与されるバフ/デバフの基底インターフェース。
    /// 新しいバフを追加する場合はこれを実装し、必要な反応インターフェースを追加で実装する。
    /// </summary>
    public interface IBattleBuff
    {
        bool IsExpired { get; }
        void TickTurnEnd();
    }

    /// <summary>
    /// 保持者がダメージを受けたときに反応するバフ。
    /// </summary>
    public interface IOnTakeDamageBuff : IBattleBuff
    {
        void OnTakeDamage(CardResolveResult result, BattleLogic logic);
    }
}
