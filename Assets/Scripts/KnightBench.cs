using System;
using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>Uses the same foreground bench for scene entry and the rest interaction.</summary>
public static class KnightBench
{
    public static Transform Find(Scene scene, Transform preferred = null)
    {
        if (!scene.IsValid() || !scene.isLoaded) return null;
        SpriteRenderer selected = Usable(preferred, scene) ? preferred.GetComponent<SpriteRenderer>() : null;
        foreach (var root in scene.GetRootGameObjects())
        foreach (var renderer in root.GetComponentsInChildren<SpriteRenderer>())
        {
            if (!Usable(renderer.transform, scene) ||
                renderer.sprite.name.IndexOf("bench", StringComparison.OrdinalIgnoreCase) < 0) continue;
            if (selected == null || InFrontOf(renderer, selected)) selected = renderer;
        }
        return selected != null ? selected.transform : null;
    }

    private static bool Usable(Transform candidate, Scene scene)
    {
        if (candidate == null || candidate.gameObject.scene != scene || !candidate.gameObject.activeInHierarchy) return false;
        var renderer = candidate.GetComponent<SpriteRenderer>();
        return renderer != null && renderer.enabled && renderer.sprite != null && renderer.color.a > 0f;
    }

    private static bool InFrontOf(SpriteRenderer candidate, SpriteRenderer selected)
    {
        int layer = SortingLayer.GetLayerValueFromID(candidate.sortingLayerID);
        int selectedLayer = SortingLayer.GetLayerValueFromID(selected.sortingLayerID);
        return layer > selectedLayer || (layer == selectedLayer && candidate.sortingOrder > selected.sortingOrder);
    }
}
