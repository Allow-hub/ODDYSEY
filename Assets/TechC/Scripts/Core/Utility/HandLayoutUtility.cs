using UnityEngine;

namespace TechC.ODDESEY.Util
{
    public static class HandLayoutUtility
    {
        public static Vector2 GetLinearPosition(int index, float startX, float spacing, float y)
        {
            float x = startX + spacing * index;
            return new Vector2(x, y);
        }
    }
}