using UnityEngine;
using UnityEditor;

public class CenterAnchorTools : EditorWindow
{
    [MenuItem("Tools/UI/Anchors To Center %#c")]
    static void AnchorsToCenter()
    {
        foreach (var obj in Selection.transforms)
        {
            RectTransform t = obj as RectTransform;
            if (t == null) continue;

            RectTransform parent = t.parent as RectTransform;
            if (parent == null) continue;

            Undo.RecordObject(t, "Anchors To Center");

            // Calculate the current visual center of the UI element relative to its parent
            Vector2 parentSize = parent.rect.size;
            if (parentSize.x == 0 || parentSize.y == 0) continue; // Prevent division by zero

            Vector2 anchorMin = t.anchorMin;
            Vector2 anchorMax = t.anchorMax;
            Vector2 offsetMin = t.offsetMin;
            Vector2 offsetMax = t.offsetMax;

            // Calculate current local pixel coordinates of min and max corners relative to parent
            Vector2 currentPixelMin = new Vector2(
                anchorMin.x * parentSize.x + offsetMin.x,
                anchorMin.y * parentSize.y + offsetMin.y
            );

            Vector2 currentPixelMax = new Vector2(
                anchorMax.x * parentSize.x + offsetMax.x,
                anchorMax.y * parentSize.y + offsetMax.y
            );

            // Find center and size in parent space
            Vector2 centerPixel = (currentPixelMin + currentPixelMax) * 0.5f;
            Vector2 size = currentPixelMax - currentPixelMin;

            // Convert center to normalized parent coordinates (0 to 1 range)
            Vector2 newAnchorCenter = new Vector2(
                centerPixel.x / parentSize.x,
                centerPixel.y / parentSize.y
            );

            // Set both min and max anchors to the center point
            t.anchorMin = newAnchorCenter;
            t.anchorMax = newAnchorCenter;

            // Re-apply size and center-align offsets
            t.sizeDelta = size;
            t.anchoredPosition = centerPixel - new Vector2(
                newAnchorCenter.x * parentSize.x,
                newAnchorCenter.y * parentSize.y
            );
        }
    }
}