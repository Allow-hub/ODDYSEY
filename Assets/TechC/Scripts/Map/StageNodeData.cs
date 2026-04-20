using System.Collections.Generic;
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
        Rest,   // 休憩（HP回復など、後で拡張用）
    }

    /// <summary>
    /// マップ上の1ノード（1列）の定義。
    /// 選択肢が1つなら強制、複数なら選択式。
    /// </summary>
    [System.Serializable]
    public class StageNodeData
    {
        [Tooltip("この列で選べる選択肢リスト（1つなら強制進行）")]
        public List<NodeType> choices = new() { NodeType.Battle };
    }
}