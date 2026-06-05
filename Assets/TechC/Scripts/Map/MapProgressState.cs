using System;
using System.Collections.Generic;

namespace TechC.ODDESEY.Map
{
    [Serializable]
    public class MapProgressState
    {
        public int currentNodeIndex = 0;
        public List<int> visitedNodeIndices = new();

        public bool IsCompleted(int totalNodeCount) => currentNodeIndex >= totalNodeCount;

        public bool HasVisited(int nodeIndex) => visitedNodeIndices.Contains(nodeIndex);

        public void MoveTo(int nodeIndex)
        {
            if (!visitedNodeIndices.Contains(currentNodeIndex))
            {
                visitedNodeIndices.Add(currentNodeIndex);
            }

            currentNodeIndex = nodeIndex;
        }

        public void Advance() => MoveTo(currentNodeIndex + 1);

        public void Reset()
        {
            currentNodeIndex = 0;
            visitedNodeIndices.Clear();
        }
    }
}
