using System;

namespace TechC.ODDESEY.Map
{
    /// <summary>
    /// マップの進行状態。MainManager が保持し、Prefab再生成時に復元する。
    /// </summary>
    [Serializable]
    public class MapProgressState
    {
        /// <summary>次に進むべきノードのインデックス（0 = まだ開始していない）</summary>
        public int currentNodeIndex = 0;

        /// <summary>全ノードをクリアしたか</summary>
        public bool IsCompleted(int totalNodeCount) => currentNodeIndex >= totalNodeCount;

        /// <summary>進行を1つ進める</summary>
        public void Advance() => currentNodeIndex++;

        /// <summary>リセット</summary>
        public void Reset() => currentNodeIndex = 0;
    }
}