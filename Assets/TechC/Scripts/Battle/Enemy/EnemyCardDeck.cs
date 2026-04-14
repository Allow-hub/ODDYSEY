using System.Collections.Generic;
using UnityEngine;

namespace TechC.ODDESEY.Battle
{
    /// <summary>
    /// 敵が持つカードのプールを定義する ScriptableObject。
    /// EnemyData からアサインして使う。
    /// プレイヤーと同じ CardData を共用するため、
    /// 敵専用の効果が必要になったタイミングで CardEffectBase を継承した
    /// EnemyExclusiveEffect などを作って差し込める。
    /// </summary>
    [CreateAssetMenu(fileName = "EnemyCardDeck", menuName = "ODDESEY/EnemyCardDeck")]
    public class EnemyCardDeck : ScriptableObject
    {
        [Header("カードプール")]
        [Tooltip("このデッキに含まれるカード。重複登録すると出現率が上がる。")]
        public List<CardData> Cards = new();

        [Header("配置設定")]
        [Tooltip("1ターンに配置するカード枚数（プレイゾーンのスロット数を超えない値にすること）")]
        [Min(1)]
        public int CardsPerTurn = 2;

        [Header("配置戦略")]
        [Tooltip("どの戦略でカードを配置するか")]
        public PlacementStrategyType StrategyType = PlacementStrategyType.Random;

        /// <summary>
        /// StrategyType に対応する IEnemyCardPlacementStrategy を生成して返す。
        /// BattleLogic の StartBattle で1度だけ呼ぶ。
        /// </summary>
        public IEnemyCardPlacementStrategy CreateStrategy()
        {
            return StrategyType switch
            {
                PlacementStrategyType.Sequential => new SequentialEnemyCardPlacementStrategy(),
                _                                => new RandomEnemyCardPlacementStrategy(),
            };
        }
    }

    public enum PlacementStrategyType
    {
        /// <summary>毎ターンデッキからランダムに選ぶ</summary>
        Random,

        /// <summary>デッキの先頭から順に出す（ループあり）</summary>
        Sequential,
    }
}