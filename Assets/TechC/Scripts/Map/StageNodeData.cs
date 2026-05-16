using System.Collections.Generic;
using TechC.ODDESEY.Event;
using TechC.ODDESEY.Reward;
using UnityEngine;

namespace TechC.ODDESEY.Map
{
    public enum NodeType
    {
        Battle,
        Event,
        Rest,
    }

    /// <summary>
    /// マップ上の1ノードの定義。
    ///
    /// 変更点：
    ///   - RewardData を追加。Battle ノードのバトル勝利後に使う報酬候補。
    ///   - IsBossNode を追加。true のとき勝利後はリザルト画面へ遷移する。
    /// </summary>
    [System.Serializable]
    public class StageNodeData
    {
        [Tooltip("この列で選べる選択肢リスト（1つなら強制進行）")]
        public List<NodeType> choices = new() { NodeType.Battle };

        [Tooltip("choices に Event が含まれる場合にアサインする EventData")]
        public EventData EventData;

        [Tooltip("choices に Battle が含まれる場合の報酬カード候補")]
        public BattleRewardData RewardData;

        [Tooltip("ボスバトル。true のとき勝利後はカード選択をスキップしてリザルトへ遷移する")]
        public bool IsBossNode = false;
    }
}