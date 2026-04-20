using System.Collections.Generic;
using UnityEngine;

namespace TechC.ODDESEY.Map
{
    /// <summary>
    /// ステージ1本分のマップ定義。
    /// Inspector から nodes を並べて選択肢を設定する。
    /// 
    /// 例：
    ///   nodes[0] = { choices: [Battle] }
    ///   nodes[1] = { choices: [Battle, Event] }
    ///   nodes[2] = { choices: [Battle] }
    ///   nodes[3] = { choices: [Event] }
    ///   nodes[4] = { choices: [Battle, Event] }
    /// </summary>
    [CreateAssetMenu(fileName = "StageMapData", menuName = "ODDESEY/Stage/StageMapData")]
    public class StageMapData : ScriptableObject
    {
        [Tooltip("ノードを上から順に並べる（index 0 が最初）")]
        public List<StageNodeData> nodes = new();
    }
}