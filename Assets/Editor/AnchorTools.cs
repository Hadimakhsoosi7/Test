using UnityEngine;
using UnityEditor;

public class AnchorTools : EditorWindow
{
    [MenuItem("Tools/UI/Anchors To Corners %#a")]
    static void AnchorsToCorners()
    {
        foreach (var obj in Selection.transforms)
        {
            RectTransform t = obj as RectTransform;
            if (t == null) continue;

            RectTransform parent = t.parent as RectTransform;
            if (parent == null) continue;

            Undo.RecordObject(t, "Anchors To Corners");

            Vector2 parentSize = parent.rect.size;
            Vector2 offsetMin = t.offsetMin;
            Vector2 offsetMax = t.offsetMax;

            t.anchorMin = new Vector2(
                t.anchorMin.x + offsetMin.x / parentSize.x,
                t.anchorMin.y + offsetMin.y / parentSize.y
            );

            t.anchorMax = new Vector2(
                t.anchorMax.x + offsetMax.x / parentSize.x,
                t.anchorMax.y + offsetMax.y / parentSize.y
            );

            t.offsetMin = Vector2.zero;
            t.offsetMax = Vector2.zero;
        }
    }
}
