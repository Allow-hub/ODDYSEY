using UnityEngine;

namespace TechC.ODDESEY.Battle
{
    [CreateAssetMenu(fileName = "EnemyData", menuName = "ODDESEY/EnemyData")]
    public class EnemyData : ScriptableObject
    {
        [Header("基本情報")]
        public string EnemyName;
        public int Hp;

        [Header("見た目")]
        public GameObject EnemyPrefab;  // SpriteRenderer を持つ Prefab

        // TODO: 敵カードリストなど追加予定
    }
}