using UnityEngine;

namespace TechC.ODDESEY.Battle
{
    /// <summary>
    /// 敵の基本情報を定義する ScriptableObject。
    /// 名前・HP・見た目のプレハブ・カードデッキを持つ。EnemyView はこれを元に初期化される。
    /// </summary>
    [CreateAssetMenu(fileName = "EnemyData", menuName = "ODDESEY/EnemyData")]
    public class EnemyData : ScriptableObject
    {
        [Header("基本情報")]
        public string EnemyName;
        public int Hp;

        [Header("見た目")]
        public GameObject EnemyPrefab;

        [Header("カードデッキ")]
        [Tooltip("この敵が使うカードプールと配置設定。null の場合はカードを配置しない。")]
        public EnemyCardDeck CardDeck;
    }
}