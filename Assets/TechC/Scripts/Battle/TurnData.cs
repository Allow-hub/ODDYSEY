using System.Collections.Generic;

namespace TechC.ODDESEY.Battle
{
    /// <summary>
    /// ターン開始時に BattleLogic → BattleController → BattleView へ渡すスナップショット。
    /// View が「今ターンを描画するために必要な情報」を全部入れる。
    /// ロジック側の内部状態への参照は持たせない（View が直接変更できないようにするため）。
    /// </summary>
    public class TurnData
    {
        /// <summary>
        /// 今ターンの手札（最大5枚）。
        /// 手札に来た時点で基礎数値（BaseProbability / BaseDamage）は確定済み。
        /// </summary>
        public List<CardInstance> Hand;

        /// <summary>
        /// プレイゾーンの全スロット（3〜5枠）。
        /// 敵カードが配置済みのスロットは IsEnemyCard = true になっている。
        /// プレイヤーはここの空きスロットに手札を配置する。
        /// </summary>
        public PlayZoneSlot[] PlayZone;
        public int PlayerHp;
        public int PlayerHpMax;
        public int EnemyHp;
        public int EnemyHpMax;
        
        /// <summary>現在のゲージ量（0〜100）</summary>
        public float LuckGauge;

        /// <summary>激アツモード中かどうか（View の演出切り替えに使う）</summary>
        public bool IsHotMode;

        // -------------------------------------------------------
        // ターン情報
        // -------------------------------------------------------

        /// <summary>現在のターン数（UIに「Turn 3」などで表示する用）</summary>
        public int TurnCount;
    }
}