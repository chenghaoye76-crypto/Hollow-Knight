using System;
using System.IO;
using System.Linq;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

[InitializeOnLoad]
public static class PromptTextSortingFix
{
    private const string PrefabPath = "Assets/Resources/Prefabs/UI/Prompt.prefab";
    static PromptTextSortingFix() => EditorApplication.update += Check;
    private static void Check()
    {
        if (EditorApplication.isCompiling || EditorApplication.isUpdating || !File.Exists("Temp/FixPromptText.request")) return;
        File.Delete("Temp/FixPromptText.request");
        try { Fix(); }
        catch (Exception error) { File.WriteAllText("Temp/PromptTextSorting.txt", error.ToString()); Debug.LogException(error); }
    }
    private static void Apply(Canvas canvas, SpriteRenderer decoration, bool recordUndo)
    {
        if (recordUndo) Undo.RecordObject(canvas, "Show prompt text above decoration");
        var frontLayer = SortingLayer.layers.OrderBy(layer => layer.value).Last();
        // Copied prefabs can reference a sorting layer that does not exist in this project.
        if (!SortingLayer.IsValid(decoration.sortingLayerID))
        {
            if (recordUndo) Undo.RecordObject(decoration, "Repair missing prompt sorting layer");
            decoration.sortingLayerID = frontLayer.id;
            if (recordUndo) PrefabUtility.RecordPrefabInstancePropertyModifications(decoration);
        }
        canvas.overrideSorting = true;
        canvas.sortingLayerID = frontLayer.id;
        canvas.sortingOrder = Mathf.Max(100, decoration.sortingOrder + 1);
        if (recordUndo) PrefabUtility.RecordPrefabInstancePropertyModifications(canvas);
        EditorUtility.SetDirty(canvas);
    }
    [MenuItem("Tools/Hollow Knight/Fix Prompt Text Sorting")]
    public static void Fix()
    {
        var prefab = PrefabUtility.LoadPrefabContents(PrefabPath);
        try
        {
            foreach (var canvas in prefab.GetComponentsInChildren<Canvas>(true))
            {
                var decoration = canvas.GetComponentInParent<SpriteRenderer>();
                if (decoration != null && canvas.renderMode == RenderMode.WorldSpace && canvas.GetComponentInChildren<TMP_Text>(true) != null)
                    Apply(canvas, decoration, false);
            }
            PrefabUtility.SaveAsPrefabAsset(prefab, PrefabPath);
        }
        finally { PrefabUtility.UnloadPrefabContents(prefab); }
        int count = 0;
        foreach (var canvas in UnityEngine.Object.FindObjectsByType<Canvas>(FindObjectsInactive.Include))
        {
            if (!canvas.gameObject.scene.IsValid() || !canvas.gameObject.scene.isLoaded || EditorSceneManager.IsPreviewScene(canvas.gameObject.scene)) continue;
            var decoration = canvas.GetComponentInParent<SpriteRenderer>();
            if (decoration == null || decoration.name != "Prompt" || canvas.renderMode != RenderMode.WorldSpace || canvas.GetComponentInChildren<TMP_Text>(true) == null) continue;
            Apply(canvas, decoration, true);
            if (!EditorApplication.isPlaying) EditorSceneManager.MarkSceneDirty(canvas.gameObject.scene);
            count++;
        }
        var saved = AssetDatabase.LoadAssetAtPath<GameObject>(PrefabPath).GetComponentInChildren<Canvas>(true);
        // A root Canvas already owns its sorting; Unity resets overrideSorting on save.
        if (saved.sortingOrder < 100) throw new Exception("Prompt text sorting was not saved.");
        File.WriteAllText("Temp/PromptTextSorting.txt", $"PASS: prefab saved; live prompts fixed={count}; layer={saved.sortingLayerName}; order={saved.sortingOrder}");
        Debug.Log("PROMPT TEXT SORTING FIXED (prefab and " + count + " live prompts). Existing unsaved scene edits are preserved.");
    }
}
