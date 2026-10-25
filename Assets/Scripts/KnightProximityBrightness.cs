using UnityEngine;

[ExecuteAlways, DisallowMultipleComponent, RequireComponent(typeof(SpriteRenderer))]
public sealed class KnightProximityBrightness : MonoBehaviour
{
    public Transform player;
    [Min(1)] public float maximumBrightness = 2f;
    [Min(0)] public float fullBrightnessDistance = 1.5f;
    [Min(.1f)] public float fadeDistance = 5f;
    [Min(.1f)] public float transitionSpeed = 6f;
    public float CurrentBrightness { get; private set; } = 1f;
    private static readonly int ColorId = Shader.PropertyToID("_Color");
    private SpriteRenderer sprite;
    private MaterialPropertyBlock block, originalBlock;
    private Color baseColor = Color.white;

    private void OnEnable()
    {
        sprite = GetComponent<SpriteRenderer>();
        block = new MaterialPropertyBlock();
        originalBlock = new MaterialPropertyBlock();
        sprite.GetPropertyBlock(originalBlock);
        baseColor = sprite.sharedMaterial != null && sprite.sharedMaterial.HasProperty(ColorId)
            ? sprite.sharedMaterial.GetColor(ColorId) : Color.white;
        if (originalBlock.HasColor(ColorId)) baseColor = originalBlock.GetColor(ColorId);
        CurrentBrightness = 1f;
        RefreshBrightness(0);
    }

    private void LateUpdate() => RefreshBrightness(Time.deltaTime);

    public void RefreshBrightness(float deltaTime)
    {
        if (sprite == null || block == null) return;
        float target = 1f;
        if (player != null && player.gameObject.activeInHierarchy)
        {
            float distance = Vector2.Distance(player.position, sprite.bounds.center);
            float proximity = 1f - Mathf.InverseLerp(fullBrightnessDistance, Mathf.Max(fullBrightnessDistance + .01f, fadeDistance), distance);
            target = Mathf.Lerp(1f, maximumBrightness, Mathf.SmoothStep(0, 1, proximity));
        }
        CurrentBrightness = Application.isPlaying
            ? Mathf.Lerp(CurrentBrightness, target, 1f - Mathf.Exp(-transitionSpeed * deltaTime)) : target;
        sprite.GetPropertyBlock(block);
        var color = baseColor;
        color.r *= CurrentBrightness; color.g *= CurrentBrightness; color.b *= CurrentBrightness;
        block.SetColor(ColorId, color);
        sprite.SetPropertyBlock(block);
    }

    private void OnDisable()
    {
        if (sprite != null && originalBlock != null) sprite.SetPropertyBlock(originalBlock);
    }
}
