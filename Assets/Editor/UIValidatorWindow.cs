using UnityEditor;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections.Generic;

public class UIValidatorWindow : EditorWindow
{
    private List<Graphic> unnecessaryRaycastTargets = new List<Graphic>();
    private List<GameObject> missingSubCanvasPanels = new List<GameObject>();
    private Vector2 scrollPosition;

    [MenuItem("Tools/UI Validator & Optimizer")]
    public static void ShowWindow()
    {
        var window = GetWindow<UIValidatorWindow>("UI Optimizer");
        window.minSize = new Vector2(400, 500);
        window.Show();
    }

    private void OnGUI()
    {
        GUILayout.Label("UI Performance Validator", EditorStyles.boldLabel);
        GUILayout.Space(10);

        if (GUILayout.Button("Scan Active Scene / UI", GUILayout.Height(30)))
        {
            ScanUI();
        }

        GUILayout.Space(15);
        scrollPosition = GUILayout.BeginScrollView(scrollPosition);

        // Section 1: Raycast Targets
        DrawRaycastTargetSection();

        GUILayout.Space(15);

        // Section 2: Missing Canvas (Sub-Canvas check)
        DrawSubCanvasSection();

        GUILayout.EndScrollView();
    }

    private void ScanUI()
    {
        unnecessaryRaycastTargets.Clear();
        missingSubCanvasPanels.Clear();

        // Find all Graphics (Images, Texts) in the active scene
        Graphic[] allGraphics = FindObjectsOfType<Graphic>(true);

        foreach (var graphic in allGraphics)
        {
            // Check for unnecessary Raycast Targets
            if (graphic.raycastTarget)
            {
                // If it doesn't have a UI component that needs interaction, it's unnecessary
                bool needsRaycast = graphic.GetComponent<Button>() != null ||
                                    graphic.GetComponent<Toggle>() != null ||
                                    graphic.GetComponent<Scrollbar>() != null ||
                                    graphic.GetComponent<InputField>() != null ||
                                    graphic.GetComponent<TMP_InputField>() != null ||
                                    graphic.GetComponent<ScrollRect>() != null;

                if (!needsRaycast)
                {
                    unnecessaryRaycastTargets.Add(graphic);
                }
            }
        }

        // Check for major panels lacking a Sub-Canvas (Look for CanvasGroups as a hint for animated panels)
        CanvasGroup[] canvasGroups = FindObjectsOfType<CanvasGroup>(true);
        foreach (var cg in canvasGroups)
        {
            // If it's a major group (often animated) but lacks a Canvas component
            if (cg.GetComponent<Canvas>() == null)
            {
                missingSubCanvasPanels.Add(cg.gameObject);
            }
        }
    }

    private void DrawRaycastTargetSection()
    {
        GUILayout.Label($"Unnecessary Raycast Targets: {unnecessaryRaycastTargets.Count}", EditorStyles.boldLabel);
        if (unnecessaryRaycastTargets.Count > 0)
        {
            GUI.backgroundColor = new Color(1f, 0.4f, 0.4f); // Red tint
            if (GUILayout.Button("Auto-Fix All (Disable Raycast Targets)"))
            {
                Undo.RecordObjects(unnecessaryRaycastTargets.ToArray(), "Disable Raycast Targets");
                foreach (var graphic in unnecessaryRaycastTargets)
                {
                    graphic.raycastTarget = false;
                    EditorUtility.SetDirty(graphic);
                }
                unnecessaryRaycastTargets.Clear();
                Debug.Log("Fixed all unnecessary Raycast Targets!");
            }
            GUI.backgroundColor = Color.white;

            GUILayout.Space(5);
            foreach (var graphic in unnecessaryRaycastTargets)
            {
                EditorGUILayout.ObjectField(graphic.gameObject, typeof(GameObject), true);
            }
        }
    }

    private void DrawSubCanvasSection()
    {
        GUILayout.Label($"Animated Panels Missing Sub-Canvas: {missingSubCanvasPanels.Count}", EditorStyles.boldLabel);
        EditorGUILayout.HelpBox("Panels with CanvasGroup (often animated) should have their own Canvas component to prevent full-screen layout rebuilds during Tween animations.", MessageType.Info);

        if (missingSubCanvasPanels.Count > 0)
        {
            GUI.backgroundColor = new Color(1f, 0.8f, 0.4f); // Orange tint
            if (GUILayout.Button("Auto-Fix All (Add Canvas & GraphicRaycaster)"))
            {
                foreach (var panel in missingSubCanvasPanels)
                {
                    Undo.AddComponent<Canvas>(panel);
                    Undo.AddComponent<GraphicRaycaster>(panel);

                    Canvas canvas = panel.GetComponent<Canvas>();
                    canvas.overrideSorting = true; // Crucial for isolating rebuilds
                    EditorUtility.SetDirty(panel);
                }
                missingSubCanvasPanels.Clear();
                Debug.Log("Added Sub-Canvas to all animated panels!");
            }
            GUI.backgroundColor = Color.white;

            GUILayout.Space(5);
            foreach (var panel in missingSubCanvasPanels)
            {
                EditorGUILayout.ObjectField(panel, typeof(GameObject), true);
            }
        }
    }
}
