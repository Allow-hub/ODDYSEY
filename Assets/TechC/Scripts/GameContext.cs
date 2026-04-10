using System.Collections.Generic;
using TechC.ODDESEY.Battle;

namespace TechC.ODDESEY
{
    /// <summary>
    /// ゲーム全体のコンテキストを管理するクラス
    /// </summary>
    public class GameContext
    {
        public int PlayerHp;
        public List<CardData> Deck;
        // public EnemyData CurrentEnemy;
        public BattleResult LastBattleResult;
        public List<CardData> RewardCandidates;
    }
}