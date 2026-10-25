using System;
using System.IO;
using System.Linq;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using UIButton = UnityEngine.UI.Button;
using Object = UnityEngine.Object;

[InitializeOnLoad]
public static class MenuUISetup
{
    const string Folder = "Assets/Resources/Prefabs/UI/Panel/";
    const string Marker = "Library/MenuUISetup-v1.done";
    static MenuUISetup() { EditorApplication.delayCall += AutoSetup; }
    static void AutoSetup()
    {
        if (File.Exists(Marker) || EditorApplication.isPlayingOrWillChangePlaymode) return;
        if (SceneManager.GetActiveScene().path != "Assets/Scenes/MainMenu.unity") return;
        try { Rebuild(); File.WriteAllText(Marker, "Completed"); }
        catch (Exception e) { File.WriteAllText("Library/MenuUISetup-error.txt", e.ToString()); Debug.LogException(e); }
    }

    [MenuItem("Tools/UI/Rebuild Main Menu")]
    public static void Rebuild()
    {
        if (EditorApplication.isPlaying) throw new InvalidOperationException("Stop Play Mode first.");
        if (PrefabStageUtility.GetCurrentPrefabStage() != null)
            throw new InvalidOperationException("Exit Prefab Mode first.");
        var scene = SceneManager.GetActiveScene();
        if (scene.path != "Assets/Scenes/MainMenu.unity")
            throw new InvalidOperationException("Open the MainMenu scene first.");

        FixMenuPrefab();
        CreateOptionsPrefab();
        FixBackgroundPrefab();

        var canvas = GameObject.Find("Canvas");
        if (!canvas) canvas = new GameObject("Canvas", typeof(RectTransform), typeof(Canvas));
        ConfigureCanvas(canvas, 1);
        // Remove only the old loose menu copies; keep unrelated scene objects.
        foreach (string name in new[] { "vheart_title", "StartGame", "Options", "QuitGame" })
        {
            var old = canvas.transform.Find(name);
            if (old) Object.DestroyImmediate(old.gameObject);
        }
        var layer = Layer(canvas.transform, "PanelLayer");
        ReplacePreview(layer, "MainMenuPanel");
        var options = layer.Find("OptionsPanel");
        if (options) options.gameObject.SetActive(false);

        var bgCanvas = GameObject.Find("BGCanvas");
        if (!bgCanvas) bgCanvas = new GameObject("BGCanvas", typeof(RectTransform), typeof(Canvas));
        ConfigureCanvas(bgCanvas, 0);
        var black = bgCanvas.transform.Find("Backcanvas");
        if (black) black.gameObject.SetActive(false);
        ReplacePreview(bgCanvas.transform, "VoidHeartBG");
        foreach (var image in bgCanvas.GetComponentsInChildren<Graphic>(true)) image.raycastTarget = false;

        var root = GameObject.Find("GameRoot") ?? new GameObject("GameRoot");
        if (!root.GetComponent<StartManager>()) root.AddComponent<StartManager>();
        var camera = Camera.main;
        if (camera) { camera.clearFlags = CameraClearFlags.SolidColor; camera.backgroundColor = Color.black; }
        CreatePreviewEnvironment();
        var scenes = EditorBuildSettings.scenes.ToList();
        if (!scenes.Any(s => s.path == scene.path))
            scenes.Insert(Math.Min(1, scenes.Count), new EditorBuildSettingsScene(scene.path, true));
        EditorBuildSettings.scenes = scenes.ToArray();
        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene);
        AssetDatabase.SaveAssets();
        Selection.activeGameObject = canvas;
        File.WriteAllText("Library/MenuUISetup-report.txt",
            "MainMenu configured. Canvas/BGCanvas: 1920x1080, Expand. Menu preview and runtime reuse one prefab. Options and keyboard navigation configured.");
        Debug.Log("Main menu UI setup completed.");
    }

    static void ConfigureCanvas(GameObject go, int order)
    {
        var canvas = go.GetComponent<Canvas>() ?? go.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = order;
        var scaler = go.GetComponent<CanvasScaler>() ?? go.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920, 1080);
        // Fit the full 1920x1080 composition on narrow and wide windows.
        scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.Expand;
        if (!go.GetComponent<GraphicRaycaster>()) go.AddComponent<GraphicRaycaster>();
    }
    static RectTransform Layer(Transform parent, string name)
    {
        var child = parent.Find(name);
        var rt = child ? (RectTransform)child : new GameObject(name, typeof(RectTransform)).GetComponent<RectTransform>();
        rt.SetParent(parent, false);
        Stretch(rt);
        rt.gameObject.layer = 5;
        return rt;
    }
    static void Stretch(RectTransform rt)
    {
        rt.localScale = Vector3.one;
        rt.localRotation = Quaternion.identity;
        rt.anchorMin = Vector2.zero;
        rt.anchorMax = Vector2.one;
        rt.offsetMin = rt.offsetMax = Vector2.zero;
        rt.pivot = new Vector2(.5f, .5f);
    }
    static void Place(RectTransform rt, float x, float y, float w, float h)
    {
        rt.anchorMin = rt.anchorMax = rt.pivot = new Vector2(.5f, .5f);
        rt.anchoredPosition = new Vector2(x, y);
        rt.sizeDelta = new Vector2(w, h);
        rt.localScale = Vector3.one;
    }
    static void ReplacePreview(Transform parent, string name)
    {
        var existing = parent.Find(name);
        if (existing) Object.DestroyImmediate(existing.gameObject);
        var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(Folder + name + ".prefab");
        var instance = (GameObject)PrefabUtility.InstantiatePrefab(prefab, parent);
        instance.name = name;
        Stretch((RectTransform)instance.transform);
    }

    static void FixMenuPrefab()
    {
        string path = Folder + "MainMenuPanel.prefab";
        var root = PrefabUtility.LoadPrefabContents(path);
        try
        {
            StripCanvas(root);
            Stretch((RectTransform)root.transform);
            if (!root.GetComponent<CanvasGroup>()) root.AddComponent<CanvasGroup>();
            if (!root.GetComponent<MenuPanelView>()) root.AddComponent<MenuPanelView>();
            var title = root.transform.Find("vheart_title");
            Place((RectTransform)title, 0, 240, 1150, 470);
            title.GetComponent<Image>().preserveAspect = true;
            title.GetComponent<Image>().raycastTarget = false;
            var buttons = new[] { "StartGame", "Options", "QuitGame" };
            var labels = new[] { "START GAME", "OPTIONS", "QUIT GAME" };
            var uiButtons = new UIButton[3];
            for (int i = 0; i < 3; i++)
            {
                var go = root.transform.Find(buttons[i]).gameObject;
                Place((RectTransform)go.transform, 0, -100 - i * 110, 420, 70);
                FormatButton(go, labels[i]);
                uiButtons[i] = go.GetComponent<UIButton>();
            }
            LinkVertical(uiButtons);
            PrefabUtility.SaveAsPrefabAsset(root, path);
        }
        finally { PrefabUtility.UnloadPrefabContents(root); }
    }
    static void StripCanvas(GameObject root)
    {
        var scaler = root.GetComponent<CanvasScaler>();
        if (scaler) Object.DestroyImmediate(scaler);
        var raycaster = root.GetComponent<GraphicRaycaster>();
        if (raycaster) Object.DestroyImmediate(raycaster);
        var canvas = root.GetComponent<Canvas>();
        if (canvas) Object.DestroyImmediate(canvas);
    }
    static void FormatButton(GameObject go, string label)
    {
        var button = go.GetComponent<UIButton>();
        button.onClick = new UIButton.ButtonClickedEvent();
        button.transition = Selectable.Transition.None;
        // Cursor animators handle the visible focus effect.
        var animator = go.GetComponent<Animator>();
        if (animator) Object.DestroyImmediate(animator);
        var text = go.GetComponentInChildren<TMP_Text>(true);
        text.text = label;
        text.enableAutoSizing = false;
        text.fontSize = 42;
        text.alignment = TextAlignmentOptions.Center;
        text.raycastTarget = false;
        Stretch(text.rectTransform);
        var image = go.GetComponent<Image>();
        image.color = new Color(1, 1, 1, 0);
        image.raycastTarget = true;
        foreach (string cursorName in new[] { "CursorL", "CursorR" })
        {
            var cursor = go.transform.Find(cursorName);
            if (!cursor) continue;
            var rt = (RectTransform)cursor;
            rt.anchorMin = rt.anchorMax = new Vector2(cursorName == "CursorL" ? 0 : 1, .5f);
            rt.anchoredPosition = new Vector2(cursorName == "CursorL" ? -35 : 35, 0);
            rt.sizeDelta = new Vector2(52, 52);
            rt.localScale = new Vector3(cursorName == "CursorL" ? 1 : -1, 1, 1);
            cursor.GetComponent<Image>().raycastTarget = false;
        }
    }
    static void LinkVertical(UIButton[] buttons)
    {
        for (int i = 0; i < buttons.Length; i++)
        {
            var nav = new Navigation { mode = Navigation.Mode.Explicit,
                selectOnUp = buttons[(i + buttons.Length - 1) % buttons.Length],
                selectOnDown = buttons[(i + 1) % buttons.Length] };
            buttons[i].navigation = nav;
        }
    }
    static void CreateOptionsPrefab()
    {
        var source = AssetDatabase.LoadAssetAtPath<GameObject>(Folder + "MainMenuPanel.prefab");
        var root = new GameObject("OptionsPanel", typeof(RectTransform), typeof(CanvasGroup), typeof(MenuPanelView));
        try
        {
            Stretch((RectTransform)root.transform);
            var template = source.transform.Find("StartGame").gameObject;
            var font = template.GetComponentInChildren<TMP_Text>().font;
            Label(root.transform, "Heading", "OPTIONS", font, 0, 320, 64);
            Label(root.transform, "VolumeLabel", "MASTER VOLUME  100%", font, 0, 140, 42);
            string[] names = { "VolumeDown", "VolumeUp", "Fullscreen", "Back" };
            string[] labels = { "VOLUME -", "VOLUME +", "FULLSCREEN", "BACK" };
            var buttons = new UIButton[4];
            for (int i = 0; i < names.Length; i++)
            {
                var go = Object.Instantiate(template, root.transform, false);
                go.name = names[i];
                Place((RectTransform)go.transform, 0, 30 - i * 110, 560, 70);
                FormatButton(go, labels[i]);
                buttons[i] = go.GetComponent<UIButton>();
            }
            LinkVertical(buttons);
            PrefabUtility.SaveAsPrefabAsset(root, Folder + "OptionsPanel.prefab");
        }
        finally { Object.DestroyImmediate(root); }
    }
    static void Label(Transform parent, string name, string text, TMP_FontAsset font, float x, float y, float size)
    {
        var go = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(TextMeshProUGUI));
        go.transform.SetParent(parent, false);
        go.layer = 5;
        var label = go.GetComponent<TextMeshProUGUI>();
        label.font = font;
        label.text = text;
        label.fontSize = size;
        label.alignment = TextAlignmentOptions.Center;
        label.raycastTarget = false;
        Place(label.rectTransform, x, y, 1000, 100);
    }
    static void FixBackgroundPrefab()
    {
        string path = Folder + "VoidHeartBG.prefab";
        var root = PrefabUtility.LoadPrefabContents(path);
        try
        {
            Stretch((RectTransform)root.transform);
            var image = root.GetComponent<Image>();
            if (image)
            {
                image.color = Color.white;
                var fitter = root.GetComponent<AspectRatioFitter>() ?? root.AddComponent<AspectRatioFitter>();
                fitter.aspectMode = AspectRatioFitter.AspectMode.EnvelopeParent;
                fitter.aspectRatio = image.sprite.rect.width / image.sprite.rect.height;
            }
            foreach (var graphic in root.GetComponentsInChildren<Graphic>(true)) graphic.raycastTarget = false;
            var group = root.GetComponent<CanvasGroup>();
            if (group) { group.blocksRaycasts = false; group.interactable = false; group.alpha = 1; }
            PrefabUtility.SaveAsPrefabAsset(root, path);
        }
        finally { PrefabUtility.UnloadPrefabContents(root); }
    }
    static void CreatePreviewEnvironment()
    {
        string path = "Assets/Scenes/UIEnvironment.unity";
        var environment = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Additive);
        var go = new GameObject("Canvas", typeof(RectTransform), typeof(Canvas));
        SceneManager.MoveGameObjectToScene(go, environment);
        ConfigureCanvas(go, 0);
        EditorSceneManager.SaveScene(environment, path);
        EditorSceneManager.CloseScene(environment, true);
        var settings = new SerializedObject(AssetDatabase.LoadAllAssetsAtPath("ProjectSettings/EditorSettings.asset")[0]);
        settings.FindProperty("m_PrefabUIEnvironment").objectReferenceValue = AssetDatabase.LoadAssetAtPath<SceneAsset>(path);
        settings.ApplyModifiedPropertiesWithoutUndo();
    }
}
