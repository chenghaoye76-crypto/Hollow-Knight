using UnityEngine;
using UnityEngine.UI;

// Small feather-shaped chevron above the thin audio track, with no stretched texture.
[RequireComponent(typeof(CanvasRenderer))]
public sealed class OptionsSliderHandle : MaskableGraphic
{
    protected override void OnPopulateMesh(VertexHelper mesh)
    {
        mesh.Clear();
        var rect = GetPixelAdjustedRect();
        float scale = rect.width / 22f;
        Triangle(mesh, rect.center, scale, new Vector2(-11f, 0f), new Vector2(-4f, -2f), new Vector2(0f, -10f));
        Triangle(mesh, rect.center, scale, new Vector2(11f, 0f), new Vector2(0f, -10f), new Vector2(4f, -2f));
        Triangle(mesh, rect.center, scale, new Vector2(-4f, 9f), new Vector2(-1f, 8f), new Vector2(0f, 4f));
        Triangle(mesh, rect.center, scale, new Vector2(4f, 9f), new Vector2(0f, 4f), new Vector2(1f, 8f));
    }
    private void Triangle(VertexHelper mesh, Vector2 center, float scale, Vector2 a, Vector2 b, Vector2 c)
    {
        int index = mesh.currentVertCount;
        mesh.AddVert(center + a * scale, color, Vector2.zero);
        mesh.AddVert(center + b * scale, color, Vector2.zero);
        mesh.AddVert(center + c * scale, color, Vector2.zero);
        mesh.AddTriangle(index, index + 1, index + 2);
    }
}
