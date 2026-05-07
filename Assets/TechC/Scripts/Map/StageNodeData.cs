using System.Collections.Generic;
using TechC.ODDESEY.Event;
using UnityEngine;

namespace TechC.ODDESEY.Map
{
    /// <summary>
    /// ノードの種類
    /// </summary>
    public enum NodeType
    {
        Battle,
        Event,
        Rest,
    }

    /// <summary>
    /// マップ上の1ノード（1列）の定義。
    ///
    /// 変更点：
    ///   - EventData フィールドを追加。
    ///     NodeType.Event の選択肢があるノードにのみアサインする。
    ///     Battle / Rest の場合は null でよい。
    /// </summary>
    [System.Serializable]
    public class StageNodeData
    {
        [Tooltip("この列で選べる選択肢リスト（1つなら強制進行）")]
        public List<NodeType> choices = new() { NodeType.Battle };

        [Tooltip("choices に Event が含まれる場合にアサインする EventData")]
        public EventData EventData;
    }
}