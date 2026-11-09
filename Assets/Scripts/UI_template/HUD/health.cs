using System;
using UnityEngine;
using UnityEngine.UI;

public class health : BaseMeshEffect
{
    // The break artwork uses a larger canvas with the mask below its center.
    private const float NormalPixelsPerBreakPixel = 0.57f;
    private static readonly Vector2 NormalMaskOffset = new Vector2(0.75f, 0f);
    private static readonly Vector2 BreakMaskOffset = new Vector2(-1.5f, -16f);

    [SerializeField] private Sprite normalSprite;
    [SerializeField] private Sprite[] breakSprites = Array.Empty<Sprite>();

    Animator animator;

    protected override void OnEnable()
    {
        animator = GetComponent<Animator>();
        base.OnEnable();
    }

    public override void ModifyMesh(VertexHelper vertices)
    {
        if (!IsActive() || normalSprite == null || !(graphic is Image image))
            return;

        Sprite sprite = image.overrideSprite;
        if (sprite == null || Array.IndexOf(breakSprites, sprite) < 0)
            return;

        Rect rect = image.GetPixelAdjustedRect();
        if (rect.width <= 0f || rect.height <= 0f)
            return;

        float currentScale = Mathf.Min(rect.width / sprite.rect.width, rect.height / sprite.rect.height);
        float normalScale = Mathf.Min(rect.width / normalSprite.rect.width, rect.height / normalSprite.rect.height);
        float targetScale = normalScale * NormalPixelsPerBreakPixel;
        Vector2 pivot = rect.min + Vector2.Scale(rect.size, image.rectTransform.pivot);

        Vector2 maskCenter = pivot + NormalMaskOffset * normalScale - BreakMaskOffset * targetScale;
        float scale = targetScale / currentScale;

        UIVertex vertex = default;
        for (int i = 0; i < vertices.currentVertCount; i++)
        {
            vertices.PopulateUIVertex(ref vertex, i);
            Vector2 position = (Vector2)vertex.position - pivot;
            Vector2 aligned = maskCenter + position * scale;
            vertex.position = new Vector3(aligned.x, aligned.y, vertex.position.z);
            vertices.SetUIVertex(vertex, i);
        }
    }

    public void Appear()
    {
        animator.SetTrigger("appear");
    }

    public void Break()
    {
        animator.SetTrigger("break");
    }

    public void Refill()
    {
        animator.SetTrigger("refill");
    }
}
