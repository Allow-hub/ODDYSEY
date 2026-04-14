using System.Collections.Generic;

namespace TechC.ODDESEY.Battle
{
    /// <summary>
    /// 敵がどのスロットにどのカードを配置するかを決める戦略インターフェース。
    /// 敵の種類やフェーズに応じて実装を差し替えることで、
    /// ランダム・固定順・パターン交互などあらゆる配置ロジックに対応できる。
    /// </summary>
    public interface IEnemyCardPlacementStrategy
    {
        /// <summary>
        /// デッキとスロット数を受け取り、「スロットインデックス → CardData」のマッピングを返す。
        /// 配置しないスロットはエントリーに含めなくてよい。
        /// </summary>
        /// <param name="deck">敵のカードプール（EnemyData.CardDeck.Cards）</param>
        /// <param name="slotCount">プレイゾーンのスロット総数</param>
        /// <param name="cardsPerTurn">1ターンに配置するカード枚数</param>
        /// <returns>スロットインデックス → CardData の辞書</returns>
        Dictionary<int, CardData> SelectCards(
            IReadOnlyList<CardData> deck,
            int slotCount,
            int cardsPerTurn);
    }
}