using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.Rendering;
using UnityEngine.UI;

// Scene-independent display and audio settings. Created before the first scene loads.
public sealed class OptionsRuntime : MonoBehaviour
{
    private static OptionsRuntime instance;
    private readonly Dictionary<AudioSource, float> sourceVolumes = new();
    private Image brightness;
    private Camera targetCamera;
    private Rect authoredViewport;
    private float scanTimer;
    private float shakeTime;
    private Vector3 cameraPosition;
    private bool cameraOffset;
    private Texture2D mousePointer;

    public static void Shake()
    {
        if (instance != null && GameOptions.CameraShake) instance.shakeTime = 0.18f;
    }

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void ResetStatics() => instance = null;
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    private static void Bootstrap()
    {
        if (instance == null) new GameObject(nameof(OptionsRuntime)).AddComponent<OptionsRuntime>();
    }
    private void Awake()
    {
        if (instance != null && instance != this) { Destroy(gameObject); return; }
        instance = this;
        DontDestroyOnLoad(gameObject);
        mousePointer = Resources.Load<Texture2D>("Sprites/Inventory & UI/Pointers/Cursor");
        ApplyMousePointer();
        var overlay = new GameObject("Brightness", typeof(RectTransform), typeof(Canvas), typeof(CanvasGroup));
        overlay.transform.SetParent(transform, false);
        var canvas = overlay.GetComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 32760;
        var group = overlay.GetComponent<CanvasGroup>();
        group.interactable = group.blocksRaycasts = false;
        var image = new GameObject("Adjustment", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
        image.transform.SetParent(overlay.transform, false);
        var rect = image.GetComponent<RectTransform>();
        rect.anchorMin = Vector2.zero; rect.anchorMax = Vector2.one; rect.offsetMin = rect.offsetMax = Vector2.zero;
        brightness = image.GetComponent<Image>();
        brightness.raycastTarget = false;
        GameOptions.Changed += Apply;
        SceneManager.sceneLoaded += SceneLoaded;
        RenderPipelineManager.beginCameraRendering += BeginCamera;
        RenderPipelineManager.endCameraRendering += EndCamera;
        Apply();
    }
    private void SceneLoaded(Scene scene, LoadSceneMode mode)
    {
        sourceVolumes.Clear();
        targetCamera = Camera.main;
        if (targetCamera != null) authoredViewport = targetCamera.rect;
        ScanSources();
        Apply();
    }
    private void Update()
    {
        shakeTime = Mathf.Max(0f, shakeTime - Time.unscaledDeltaTime);
        scanTimer += Time.unscaledDeltaTime;
        if (scanTimer < 1f) return;
        scanTimer = 0f;
        ScanSources();
    }
    private void BeginCamera(ScriptableRenderContext context, Camera camera)
    {
        if (camera != targetCamera || shakeTime <= 0f || !GameOptions.CameraShake) return;
        cameraPosition = camera.transform.position;
        Vector2 offset = Random.insideUnitCircle * (0.07f * shakeTime / 0.18f);
        camera.transform.position += new Vector3(offset.x, offset.y, 0f);
        cameraOffset = true;
    }
    private void EndCamera(ScriptableRenderContext context, Camera camera)
    {
        if (camera != targetCamera || !cameraOffset) return;
        camera.transform.position = cameraPosition;
        cameraOffset = false;
    }
    private void ScanSources()
    {
        foreach (var source in FindObjectsByType<AudioSource>())
        {
            if (source.GetComponent<OptionsAudioChannel>() != null || sourceVolumes.ContainsKey(source)) continue;
            sourceVolumes.Add(source, source.volume);
            source.volume *= GameOptions.SoundVolume;
        }
    }
    private void Apply()
    {
        if (brightness != null)
        {
            float value = GameOptions.Float("Brightness", 0.5f) - 0.5f;
            brightness.color = value < 0f ? new Color(0f, 0f, 0f, -value * 1.3f) : new Color(1f, 1f, 1f, value * 0.55f);
        }
        if (targetCamera != null)
        {
            float scale = Mathf.Clamp(GameOptions.Float("ScreenScale", 1f), 0.5f, 1f);
            float w = authoredViewport.width * scale, h = authoredViewport.height * scale;
            targetCamera.rect = new Rect(authoredViewport.center.x - w * 0.5f, authoredViewport.center.y - h * 0.5f, w, h);
        }
        foreach (var pair in sourceVolumes)
            if (pair.Key != null && pair.Key.GetComponent<OptionsAudioChannel>() == null) pair.Key.volume = pair.Value * GameOptions.SoundVolume;
    }
    private void ApplyMousePointer()
    {
        if (mousePointer != null)
            UnityEngine.Cursor.SetCursor(mousePointer, new Vector2(4f, 2f), CursorMode.Auto);
    }
    private void OnApplicationFocus(bool focused)
    {
        if (focused) ApplyMousePointer();
    }
    private void OnDestroy()
    {
        if (instance == this)
        {
            UnityEngine.Cursor.SetCursor(null, Vector2.zero, CursorMode.Auto);
            instance = null;
        }
        GameOptions.Changed -= Apply;
        SceneManager.sceneLoaded -= SceneLoaded;
        RenderPipelineManager.beginCameraRendering -= BeginCamera;
        RenderPipelineManager.endCameraRendering -= EndCamera;
    }
}
