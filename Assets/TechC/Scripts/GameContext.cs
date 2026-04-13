using System.Collections.Generic;
using TechC.ODDESEY.Battle;
using UnityEngine;

namespace TechC.ODDESEY
{
    /// <summary>
    /// ゲーム全体のコンテキスト。純粋C#クラス。
    /// ステージをまたいで永続するデータのみ持つ。
    /// </summary>
    public class GameContext
    {
        public int PlayerHp;
        public int PlayerHpMax;
        public List<CardData> Deck;
        public List<CardData> RewardCandidates;
        public EnemyData  CurrentEnemy;
        // public BattleResult LastBattleResult;
    }

    /// <summary>
    /// Inspector から GameContext の初期値を設定するデバッグ用データ。
    /// MainManager に [SerializeField] で持たせる。
    /// </summary>
    [System.Serializable]
    public class DebugGameContext
    {
        [Header("プレイヤー")]
        public int PlayerHp = 30;
        public int PlayerHpMax = 30;

        [Header("初期デッキ（CardData SO をドラッグして登録）")]
        public List<CardData> InitialDeck = new();

        [Header("デバッグ用敵")]
        public EnemyData DebugEnemy;

        public GameContext ToGameContext()
        {
            return new GameContext
            {
                PlayerHp = PlayerHp,
                PlayerHpMax = PlayerHpMax,
                Deck = new List<CardData>(InitialDeck),
                CurrentEnemy = DebugEnemy,
            };
        }
    }
}