using UnityEngine;
using UnityEditor;
using TMPro;

public class TMPGlyphOXFixer : EditorWindow
{
    private TMP_FontAsset fontAsset;
    private string targetLetter = "X";
    private int targetGlyphID = 0;

    public enum InputMode { Letter, GlyphID }
    private InputMode inputMode = InputMode.Letter;

    public enum GlyphPosition { LeftGlyph, RightGlyph }
    private GlyphPosition searchPosition = GlyphPosition.LeftGlyph;
    private GlyphPosition clearPosition = GlyphPosition.RightGlyph;

    [MenuItem("Tools/TMP Glyph OX Fixer")]
    public static void ShowWindow()
    {
        var window = GetWindow<TMPGlyphOXFixer>("Glyph OX Fixer");
        window.minSize = new Vector2(350, 310);
    }

    private void OnGUI()
    {
        GUILayout.Space(10);
        EditorGUILayout.HelpBox("Finds pairs containing your Target Letter/Glyph ID and either clears the OX to 0 or deletes the pair completely.", MessageType.Info);
        GUILayout.Space(10);

        fontAsset = (TMP_FontAsset)EditorGUILayout.ObjectField("Font Asset", fontAsset, typeof(TMP_FontAsset), false);

        GUILayout.Space(5);
        inputMode = (InputMode)EditorGUILayout.EnumPopup("Input Mode", inputMode);

        if (inputMode == InputMode.Letter)
        {
            targetLetter = EditorGUILayout.TextField("Target Letter", targetLetter);
        }
        else
        {
            targetGlyphID = EditorGUILayout.IntField("Target Glyph ID", targetGlyphID);
        }

        GUILayout.Space(10);
        searchPosition = (GlyphPosition)EditorGUILayout.EnumPopup("Target Position is the:", searchPosition);
        clearPosition = (GlyphPosition)EditorGUILayout.EnumPopup("Clear OX of the:", clearPosition);

        GUILayout.Space(15);

        // Clear OX Button
        GUI.backgroundColor = new Color(0.2f, 0.6f, 1f);
        if (GUILayout.Button($"Clear {clearPosition} OX to 0", GUILayout.Height(35)))
        {
            FixGlyphs();
        }

        GUILayout.Space(5);

        // Delete Pairs Button
        GUI.backgroundColor = new Color(0.9f, 0.25f, 0.25f);
        if (GUILayout.Button("Delete Matching Pairs", GUILayout.Height(35)))
        {
            DeleteGlyphPairs();
        }
        GUI.backgroundColor = Color.white;
    }

    private uint GetTargetGlyphIndex(out bool success)
    {
        success = false;
        if (inputMode == InputMode.Letter)
        {
            if (string.IsNullOrEmpty(targetLetter))
            {
                Debug.LogWarning("TMP Fixer: Please enter a target letter.");
                return 0;
            }

            char targetChar = targetLetter[0];
            uint unicode = (uint)targetChar;

            if (!fontAsset.characterLookupTable.TryGetValue(unicode, out TMP_Character character))
            {
                Debug.LogWarning($"TMP Fixer: The character '{targetChar}' was not found in this font asset.");
                return 0;
            }

            success = true;
            return character.glyphIndex;
        }
        else
        {
            success = true;
            return (uint)targetGlyphID;
        }
    }

    private void FixGlyphs()
    {
        if (fontAsset == null)
        {
            Debug.LogWarning("TMP Fixer: Please assign a TMP_FontAsset first!");
            return;
        }

        uint targetGlyphIndex = GetTargetGlyphIndex(out bool success);
        if (!success) return;

        int modifiedCount = 0;
        Undo.RecordObject(fontAsset, "Clear Glyph OX");

        var records = fontAsset.fontFeatureTable.glyphPairAdjustmentRecords;

        for (int i = 0; i < records.Count; i++)
        {
            var record = records[i];

            // 1. Check if the target matches our chosen search position
            bool isMatch = false;
            if (searchPosition == GlyphPosition.LeftGlyph && record.firstAdjustmentRecord.glyphIndex == targetGlyphIndex)
            {
                isMatch = true;
            }
            else if (searchPosition == GlyphPosition.RightGlyph && record.secondAdjustmentRecord.glyphIndex == targetGlyphIndex)
            {
                isMatch = true;
            }

            // 2. If we found a match, clear the chosen side's OX
            if (isMatch)
            {
                if (clearPosition == GlyphPosition.LeftGlyph)
                {
                    var firstRec = record.firstAdjustmentRecord;
                    var valRec = firstRec.glyphValueRecord;

                    if (valRec.xPlacement != 0)
                    {
                        valRec.xPlacement = 0;
                        firstRec.glyphValueRecord = valRec;
                        record.firstAdjustmentRecord = firstRec;
                        records[i] = record;
                        modifiedCount++;
                    }
                }
                else if (clearPosition == GlyphPosition.RightGlyph)
                {
                    var secondRec = record.secondAdjustmentRecord;
                    var valRec = secondRec.glyphValueRecord;

                    if (valRec.xPlacement != 0)
                    {
                        valRec.xPlacement = 0;
                        secondRec.glyphValueRecord = valRec;
                        record.secondAdjustmentRecord = secondRec;
                        records[i] = record;
                        modifiedCount++;
                    }
                }
            }
        }

        if (modifiedCount > 0)
        {
            EditorUtility.SetDirty(fontAsset);
            AssetDatabase.SaveAssets();
            Debug.Log($"<color=lime><b>Success!</b></color> Cleared OX to 0 for {modifiedCount} glyph pairs.");
        }
        else
        {
            Debug.Log("Checked pairs, but no OX values needed changing (they are already 0).");
        }
    }

    private void DeleteGlyphPairs()
    {
        if (fontAsset == null)
        {
            Debug.LogWarning("TMP Fixer: Please assign a TMP_FontAsset first!");
            return;
        }

        uint targetGlyphIndex = GetTargetGlyphIndex(out bool success);
        if (!success) return;

        Undo.RecordObject(fontAsset, "Delete Glyph Pairs");

        var records = fontAsset.fontFeatureTable.glyphPairAdjustmentRecords;
        int initialCount = records.Count;

        // Iterate backwards when deleting elements from a list
        for (int i = records.Count - 1; i >= 0; i--)
        {
            var record = records[i];

            bool isMatch = false;
            if (searchPosition == GlyphPosition.LeftGlyph && record.firstAdjustmentRecord.glyphIndex == targetGlyphIndex)
            {
                isMatch = true;
            }
            else if (searchPosition == GlyphPosition.RightGlyph && record.secondAdjustmentRecord.glyphIndex == targetGlyphIndex)
            {
                isMatch = true;
            }

            if (isMatch)
            {
                records.RemoveAt(i);
            }
        }

        int removedCount = initialCount - records.Count;

        if (removedCount > 0)
        {
            EditorUtility.SetDirty(fontAsset);
            AssetDatabase.SaveAssets();
            Debug.Log($"<color=lime><b>Success!</b></color> Deleted {removedCount} matching glyph pairs.");
        }
        else
        {
            Debug.Log("No matching adjustment pairs were found to delete.");
        }
    }
}