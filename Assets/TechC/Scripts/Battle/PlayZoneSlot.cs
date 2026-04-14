namespace TechC.ODDESEY.Battle
{
    /// <summary>
    /// プレイゾーン1スロットのロジック側データ。
    /// プレイヤーカードと敵カードのどちらかが入る。
    /// </summary>
    public class PlayZoneSlot
    {
        /// <summary>プレイヤーが配置した CardInstance（IsEnemyCard=false のとき有効）</summary>
        public CardInstance PlayerCardInstance { get; set; }

        /// <summary>
        /// 敵が配置した CardData（IsEnemyCard=true のとき有効）。
        /// 敵カードはインスタンスを使い回さないため CardData 直参照で持つ。
        /// 将来的に EnemyCardInstance を作る場合はここを差し替える。
        /// </summary>
        public CardInstance EnemyCardInstance { get; set; }

        /// <summary>このスロットが敵カードで占有されているか</summary>
        public bool IsEnemyCard { get; set; }

        /// <summary>プレイヤーカードが置かれているか（IsEnemyCard=false かつ PlayerCardInstance が非null）</summary>
        public bool IsPlayerCard => !IsEnemyCard && PlayerCardInstance != null;

        /// <summary>何も配置されていないか</summary>
        public bool IsEmpty => PlayerCardInstance == null && EnemyCardInstance == null;

        /// <summary>スロットをリセットする。ターン終了時に呼ぶ。</summary>
        public void Clear()
        {
            PlayerCardInstance = null;
            EnemyCardInstance = null;
            IsEnemyCard = false;
        }
    }
}