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

    [System.Serializable]
    public class StageNodeData
    {
        [Tooltip("このノード自体の種類。マップ上の見た目と下部ボタンの見た目に使います。")]
        public NodeType nodeType = NodeType.Battle;

        [Tooltip("このノードから次に進めるノードのindex。未設定なら次のindexへ進む線形マップとして扱います。")]
        public List<int> nextNodeIndices = new();

        [Tooltip("旧仕様の選択肢リスト。移行用に残しています。")]
        public List<NodeType> choices = new() { NodeType.Battle };

        [Tooltip("Eventノードの場合に使うEventData。MapIconTypeもここから参照します。")]
        public EventData EventData;

        [Tooltip("Battleノードの場合に使う報酬データ。")]
        public BattleRewardData RewardData;

        [Tooltip("ボスノード。trueの場合はバトル勝利後にリザルトへ進みます。")]
        public bool IsBossNode = false;
    }
}
