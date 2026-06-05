using System.Collections.Generic;
using UnityEngine;

namespace TechC.ODDESEY.Map
{
    /// <summary>
    /// ステージ1本分のマップ定義。
    /// Inspector から nodes を並べ、各ノードの接続先を nextNodeIndices で設定する。
    /// 
    /// 例：
    ///   nodes[0] = { nodeType: Rest, nextNodeIndices: [1, 2] }
    ///   nodes[1] = { nodeType: Battle, nextNodeIndices: [3] }
    ///   nodes[2] = { nodeType: Event, nextNodeIndices: [3] }
    ///   nodes[3] = { nodeType: Battle, nextNodeIndices: [] }
    /// </summary>
    [CreateAssetMenu(fileName = "StageMapData", menuName = "ODDESEY/Stage/StageMapData")]
    public class StageMapData : ScriptableObject
    {
        [Tooltip("ノードを上から順に並べる（index 0 が最初）")]
        public List<StageNodeData> nodes = new();
    }
}
