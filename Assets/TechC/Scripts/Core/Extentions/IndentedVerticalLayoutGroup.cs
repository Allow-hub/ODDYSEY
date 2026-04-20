using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace UnityEngine.UI
{
    [AddComponentMenu("Layout/Indented Vertical Layout Group")]
    public class IndentedVerticalLayoutGroup : LayoutGroup
    {
        [SerializeField] private float _spacing = 0f;
        [SerializeField] private float _indentLeft = 10f;
        [SerializeField] private float _indentRight = 0f;

        public override void CalculateLayoutInputHorizontal()
        {
            base.CalculateLayoutInputHorizontal();

            float minWidth = padding.horizontal;
            float preferredWidth = padding.horizontal;

            foreach (var child in rectChildren)
            {
                minWidth = Mathf.Max(minWidth, LayoutUtility.GetMinWidth(child) + padding.horizontal);
                preferredWidth = Mathf.Max(preferredWidth, LayoutUtility.GetPreferredWidth(child) + padding.horizontal);
            }

            SetLayoutInputForAxis(minWidth, preferredWidth, -1, 0);
        }

        public override void CalculateLayoutInputVertical()
        {
            float totalHeight = padding.vertical;

            for (int i = 0; i < rectChildren.Count; i++)
            {
                var child = rectChildren[i];
                totalHeight += LayoutUtility.GetPreferredHeight(child);
                if (i < rectChildren.Count - 1)
                    totalHeight += _spacing;
            }

            SetLayoutInputForAxis(totalHeight, totalHeight, -1, 1);
        }

        public override void SetLayoutHorizontal()
        {
            float parentWidth = rectTransform.rect.width;

            foreach (var child in rectChildren)
            {
                float left = padding.left + _indentLeft;
                float childWidth = parentWidth - left - (padding.right + _indentRight);

                child.SetInsetAndSizeFromParentEdge(RectTransform.Edge.Left, left, childWidth);
            }
        }

        public override void SetLayoutVertical()
        {
            float y = padding.top;

            for (int i = 0; i < rectChildren.Count; i++)
            {
                var child = rectChildren[i];
                // ForceRebuild後の実際の高さを取得
                float childHeight = LayoutUtility.GetPreferredHeight(child);

                child.SetInsetAndSizeFromParentEdge(RectTransform.Edge.Top, y, childHeight);

                y += childHeight + _spacing;
            }
        }
    }
}