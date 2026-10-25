using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

[InitializeOnLoad]
public static class DirtmouthBuilder
{
    public const string ScenePath = "Assets/Scenes/Dirtmouth.unity";
    private const string Art = "Assets/Resources/Sprites/Architecture & Environment/Area specfic architecture/Dirthmouth/";
    private const string Sprites = "Assets/Resources/Sprites/";
    private static Transform parent;
    static DirtmouthBuilder() => EditorApplication.update += CheckRequest;
    private static void CheckRequest()
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode || EditorApplication.isCompiling || EditorApplication.isUpdating ||
            !File.Exists("Temp/BuildDirtmouth.request")) return;
        File.Delete("Temp/BuildDirtmouth.request");
        try { Build(); File.WriteAllText("Temp/DirtmouthBuild.txt", "PASS: " + ScenePath); }
        catch (Exception error) { File.WriteAllText("Temp/DirtmouthBuild.txt", error.ToString()); Debug.LogException(error); }
    }
    private static Transform Group(string name, Transform root)
    {
        var go = new GameObject(name);
        go.transform.SetParent(root, false);
        return go.transform;
    }
    private static Sprite Load(string path)
    {
        var sprite = AssetDatabase.LoadAssetAtPath<Sprite>(path);
        if (sprite == null) throw new Exception("Missing existing sprite: " + path);
        return sprite;
    }
    private static SpriteRenderer Piece(string name, string path, Vector2 position, Vector2 scale, int order, Color colour)
    {
        var go = new GameObject(name, typeof(SpriteRenderer));
        go.transform.SetParent(parent, false);
        go.transform.position = position;
        go.transform.localScale = new Vector3(scale.x, scale.y, 1f);
        var renderer = go.GetComponent<SpriteRenderer>();
        renderer.sprite = Load(path);
        renderer.sortingOrder = order;
        renderer.color = colour;
        return renderer;
    }
    private static SpriteRenderer Town(string file, float x, float bottom, float scale, int order, Color colour, string name = null)
    {
        var renderer = Piece(name ?? file, Art + file + ".png", new Vector2(x, bottom), Vector2.one * scale, order, colour);
        renderer.transform.position += Vector3.up * (-renderer.sprite.bounds.min.y * scale);
        return renderer;
    }
    private static void Parallax(SpriteRenderer renderer, float amount, float drift = 0f, float sway = 0f)
    {
        var effect = renderer.gameObject.AddComponent<DirtmouthAtmosphere>();
        effect.parallax = amount; effect.drift = drift; effect.sway = sway;
    }
    private static void SolidCollider(string name, Vector2 centre, Vector2 size)
    {
        var go = new GameObject(name);
        go.transform.SetParent(parent, false);
        go.transform.position = centre;
        go.layer = LayerMask.NameToLayer("Terrain");
        go.AddComponent<BoxCollider2D>().size = size;
    }
    private static void Floor(float start, float end)
    {
        for (float x = start; x < end - 0.01f;)
        {
            string file = "town_floor_0" + (1 + ((int)Mathf.Abs(x * 13) % 3));
            Sprite sprite = Load(Art + file + ".png");
            float width = Mathf.Min(sprite.bounds.size.x * 1.2f, end - x);
            var renderer = Piece("Stone paving", Art + file + ".png", new Vector2(x + width * 0.5f, -0.2f),
                new Vector2(width / sprite.bounds.size.x, 1.2f), 15, Color.white);
            x += width;
        }
        SolidCollider("Walkable stone street", new Vector2((start + end) * 0.5f, -1.1f), new Vector2(end - start, 2.2f));
    }
    private static TMP_Text Text(string name, Transform root, string value, TMP_FontAsset font, int size, Vector2 position, Vector2 dimensions)
    {
        var go = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(TextMeshProUGUI));
        go.transform.SetParent(root, false);
        var text = go.GetComponent<TMP_Text>();
        text.font = font; text.text = value; text.fontSize = size; text.color = new Color(0.89f, 0.92f, 0.98f);
        text.alignment = TextAlignmentOptions.Center; text.raycastTarget = false;
        text.rectTransform.sizeDelta = dimensions; text.rectTransform.anchoredPosition = position;
        return text;
    }

    [MenuItem("Tools/Hollow Knight/Build Dirtmouth Scene")]
    public static void Build()
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode) throw new Exception("Build Dirtmouth outside Play Mode.");
        if (File.Exists(ScenePath)) throw new Exception("Dirtmouth already exists. Edit the authored scene instead of overwriting it.");
        var original = SceneManager.GetActiveScene();
        bool originalsClean = Enumerable.Range(0, SceneManager.sceneCount).All(i => !SceneManager.GetSceneAt(i).isDirty);
        var source = SceneManager.GetSceneByPath("Assets/Scenes/Game.unity");
        bool openedSource = !source.IsValid() || !source.isLoaded;
        if (openedSource) source = EditorSceneManager.OpenScene("Assets/Scenes/Game.unity", OpenSceneMode.Additive);
        var knightSource = source.GetRootGameObjects().First(go => go.name == "Knight");
        var hudSource = source.GetRootGameObjects().First(go => go.name == "Canvas");
        var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Additive);
        SceneManager.SetActiveScene(scene);
        var root = new GameObject("Dirtmouth · The Fading Town");
        parent = Group("00 · Night sky", root.transform);
        var sky = Piece("Blue night and distant starlight", Sprites + "Particles & Effects/white_solid.png", new Vector2(10f, 15f), Vector2.one, -250, Color.white);
        sky.transform.localScale = new Vector3(240f / sky.sprite.bounds.size.x, 85f / sky.sprite.bounds.size.y, 1f);
        var skyShader = Shader.Find("Dirtmouth/NightSky");
        if (skyShader == null) throw new Exception("Night sky shader was not imported.");
        var skyMaterial = new Material(skyShader);
        AssetDatabase.CreateAsset(skyMaterial, "Assets/Resources/Material/DirtmouthSky.mat");
        sky.sharedMaterial = skyMaterial;

        parent = Group("01 · Distant silhouettes", root.transform);
        var random = new System.Random(367520);
        string[] farHouses = { "Town_BG_0002_04", "Town_BG_0007_01", "Town_BG_0003_03b", "Town_BG_0005_02b", "town_tmp_BG" };
        for (int i = 0; i < 28; i++)
        {
            float x = -90f + i * 7.2f;
            var distant = Town(farHouses[i % farHouses.Length], x, -1.2f + (float)random.NextDouble() * 1.2f,
                2.4f + (float)random.NextDouble() * 2.6f, -150 + i % 3, new Color(0.37f, 0.46f, 0.63f, 0.65f), "Far town silhouette");
            Parallax(distant, 0.76f);
        }
        parent = Group("02 · Misty middle distance", root.transform);
        for (int i = 0; i < 20; i++)
        {
            var distant = Town(i % 2 == 0 ? "Town_BG_0003_03b" : "Town_BG_0005_02b", -76f + i * 8.5f,
                -0.65f, 3.4f + (float)random.NextDouble() * 1.4f, -95, new Color(0.50f, 0.59f, 0.75f, 0.8f), "Houses in blue haze");
            if (i % 3 == 0) distant.flipX = true;
            Parallax(distant, 0.4f);
        }

        parent = Group("03 · Shell houses and storefronts", root.transform);
        var facadeColour = new Color(0.82f, 0.88f, 1f);
        Town("town_buildings_left_0002_a", -48f, -0.3f, 5.1f, -25, facadeColour, "Western abandoned house");
        Town("town_buildings_left_0000_a", -34f, -0.35f, 2.6f, -20, facadeColour, "Sly's shell house");
        Town("sly_shop_sign_01", -30.6f, 0.05f, 1.1f, -8, Color.white, "Sly shop sign");
        Town("Town_BG_0003_03b", -19f, -0.3f, 5f, -18, facadeColour, "Map shop shell");
        Town("mapper_ext_BG", -18.1f, 0f, 1.25f, -12, Color.white, "Iselda's map shop doorway");
        Town("map_shop_closed_sign", -15.6f, 0.1f, 1.05f, -7, Color.white, "Map shop hanging sign");
        Town("Town_BG_0005_02b", 3.6f, -0.2f, 5.7f, -18, new Color(0.85f, 0.90f, 1f), "Shell house behind the bench");
        Town("town_buildings_left_0001_a", -7.2f, -0.25f, 4.7f, -20, facadeColour, "Empty home west of bench");
        Town("shop_BG_0002_a", 32f, -0.35f, 2.9f, -22, facadeColour, "Stag station shell");
        Town("stag_station_front", 32f, 0f, 1.33f, -10, Color.white, "Stag station ornate entrance");
        Town("town_buildings_left_0000_a", 46f, -0.3f, 2.45f, -18, facadeColour, "Bretta's house");
        Town("bretta_door_ext", 45.2f, 0f, 1.1f, -9, Color.white, "Bretta's door");
        Town("Town_BG_0000_05_02", 71f, 0f, 3.4f, -23, facadeColour, "Graveyard and distant path");

        parent = Group("04 · Street, well and terrain", root.transform);
        Floor(-64f, 17f); Floor(19f, 85f);
        // Dark earth under the paving, with a real gap under the well opening.
        foreach (var span in new[] { new Vector2(-64f, 17f), new Vector2(19f, 85f) })
        {
            var earth = Piece("Ground beneath town", Sprites + "Particles & Effects/white_solid.png", new Vector2((span.x + span.y) * 0.5f, -6f), Vector2.one, 10, new Color(0.012f, 0.018f, 0.027f));
            earth.transform.localScale = new Vector3((span.y-span.x)/earth.sprite.bounds.size.x, 11.5f/earth.sprite.bounds.size.y, 1f);
        }
        Town("town_well_back", 18f, -1.1f, 1.2f, -4, Color.white, "Well rear stone rim");
        Town("town_well_top", 18f, 0.1f, 1.22f, -2, Color.white, "The well's wrought iron arch");
        var wellFront = Town("town_well_front", 18f, -8.35f, 1.2f, 22, Color.white, "Well front and descending shaft");
        SolidCollider("Well left wall", new Vector2(16.8f, -4.5f), new Vector2(0.4f, 9f));
        SolidCollider("Well right wall", new Vector2(19.2f, -4.5f), new Vector2(0.4f, 9f));
        SolidCollider("Western border", new Vector2(-63f, 5f), new Vector2(1f, 15f));
        SolidCollider("Eastern border", new Vector2(84f, 5f), new Vector2(1f, 15f));

        parent = Group("05 · Bench, lamps and graveyard", root.transform);
        var bench = Town("town_bench_lit", 0f, 0.03f, 1.25f, -3, Color.white, "Rest bench");
        foreach (float lampX in new[] { -51f, -24f, 0.9f, 23.5f, 52f, 77f })
        {
            Town("town_lamp_BG", lampX, 0f, 1.75f, -6, Color.white, "Spiral street lamp");
            foreach (var offset in new[] { new Vector2(-0.7f, 5.75f), new Vector2(0.6f, 4.6f) })
            {
                var glow = Piece("Soft lamplight", Sprites + "Particles & Effects/white_light.png", new Vector2(lampX, 0f) + offset,
                    Vector2.one * 1.9f, -5, new Color(0.62f, 0.72f, 1f, 0.18f));
                Parallax(glow, 0f, 0.025f);
            }
        }
        for (int i = 0; i < 8; i++)
        {
            var crosses = Directory.GetFiles(Art, "town_grave_cross*.png").OrderBy(path => path).ToArray();
            var grave = Piece("Old grave marker", crosses[(i * 3) % crosses.Length].Replace('\\','/'), new Vector2(57f + i * 2.6f, 0.1f), Vector2.one * (1.2f + i % 3 * 0.2f), -12, facadeColour);
            grave.transform.position += Vector3.up * (-grave.sprite.bounds.min.y * grave.transform.localScale.y);
        }
        for (int i = 0; i < 6; i++) Town("town_fence", 55f + i * 4.3f, -0.1f, 1.2f, -15, facadeColour, "Graveyard iron fence");

        parent = Group("06 · Near grasses and atmospheric motes", root.transform);
        for (int i = 0; i < 65; i++)
        {
            float x = -62f + (float)random.NextDouble() * 145f;
            if (Mathf.Abs(x-18f) < 3f || Mathf.Abs(x) < 2.2f) continue;
            string grass = "grass_0" + (1+i%3) + "_idle0000.png";
            var tuft = Piece("Windblown verge", Sprites + "Architecture & Environment/Grass & Vines/" + grass,
                new Vector2(x, -0.05f), Vector2.one * (0.6f + (float)random.NextDouble() * 0.6f), 24, new Color(0.18f, 0.25f, 0.39f, 0.95f));
            tuft.transform.position += Vector3.up * (-tuft.sprite.bounds.min.y * tuft.transform.localScale.y);
            Parallax(tuft, -0.035f, 0f, 2f);
        }
        for (int i = 0; i < 70; i++)
        {
            var mote = Piece("Floating dust mote", Sprites + "Particles & Effects/white_light.png", new Vector2(-70f + (float)random.NextDouble() * 165f, 0.2f + (float)random.NextDouble() * 14f),
                Vector2.one * (0.014f + (float)random.NextDouble() * 0.025f), -7, new Color(0.75f, 0.85f, 1f, 0.4f));
            Parallax(mote, 0.08f, 0.7f);
        }
        for (int i = 0; i < 16; i++)
        {
            var haze = Piece("Low drifting haze", Sprites + "Particles & Effects/white_light.png", new Vector2(-65f + i * 10f, 0.5f + (i%3)*0.6f),
                new Vector2(8f, 1.2f), -35, new Color(0.52f, 0.65f, 0.87f, 0.055f));
            Parallax(haze, 0.14f, 2.5f);
        }

        parent = root.transform;
        var knight = UnityEngine.Object.Instantiate(knightSource);
        SceneManager.MoveGameObjectToScene(knight, scene);
        knight.name = "Knight"; knight.transform.SetParent(root.transform, true);
        var collider = knight.GetComponent<BoxCollider2D>();
        knight.transform.position = new Vector3(-3.5f, 0.06f + (collider.size.y*0.5f-collider.offset.y)*Mathf.Abs(knight.transform.localScale.y), 0f);
        knight.GetComponent<SpriteRenderer>().sortingOrder = 18;
        knight.SetActive(true);
        var hud = UnityEngine.Object.Instantiate(hudSource);
        SceneManager.MoveGameObjectToScene(hud, scene); hud.name = "HUD";
        foreach (var text in hud.GetComponentsInChildren<TMP_Text>(true)) if (text.text == "114514") text.text = "0";
        var sourceUI = knightSource.GetComponent<PlayerUI>();
        var playerUI = knight.GetComponent<PlayerUI>();
        if (sourceUI != null && playerUI != null)
        {
            playerUI.healthItem = sourceUI.healthItem.Select(item => item == null ? null : hud.transform.Find(AnimationUtility.CalculateTransformPath(item.transform, hudSource.transform))?.GetComponent<Animator>()).ToArray();
            if (sourceUI.geo != null) playerUI.geo = hud.transform.Find(AnimationUtility.CalculateTransformPath(sourceUI.geo.transform, hudSource.transform))?.GetComponent<Animator>();
            if (playerUI.healthItem.Any(item => item == null) || playerUI.geo == null)
                throw new Exception("The cloned HUD references must all target the new scene.");
        }
        var cameraObject = new GameObject("Main Camera", typeof(Camera), typeof(AudioListener), typeof(DirtmouthCamera));
        cameraObject.tag = "MainCamera";
        var camera = cameraObject.GetComponent<Camera>();
        camera.orthographic = true; camera.orthographicSize = 7.8f;
        camera.backgroundColor = new Color(0.025f, 0.04f, 0.07f);
        camera.clearFlags = CameraClearFlags.SolidColor;
        cameraObject.transform.position = new Vector3(-3.5f, 4.3f, -20f);
        cameraObject.GetComponent<DirtmouthCamera>().target = knight.transform;

        var canvasObject = new GameObject("Town title and interaction", typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler));
        var canvas = canvasObject.GetComponent<Canvas>(); canvas.renderMode = RenderMode.ScreenSpaceOverlay; canvas.sortingOrder = 40;
        var scaler = canvasObject.GetComponent<CanvasScaler>(); scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize; scaler.referenceResolution = new Vector2(1920,1080);
        var fonts = Resources.Load<OptionsVisualAssets>("OptionsVisualAssets");
        var titleRoot = new GameObject("Location title", typeof(RectTransform), typeof(CanvasGroup)); titleRoot.transform.SetParent(canvasObject.transform, false);
        var titleFont = Resources.Load<GameObject>("Prefabs/UI/Panel/OptionsPanel").GetComponentsInChildren<TMP_Text>(true).First().font;
        Text("DIRTMOUTH", titleRoot.transform, "DIRTMOUTH", titleFont, 49, new Vector2(0f,-165f), new Vector2(850,85));
        Text("The Fading Town", titleRoot.transform, "THE FADING TOWN", titleFont, 23, new Vector2(0f,-226f), new Vector2(850,55));
        var prompt = Text("Bench prompt", canvasObject.transform, "REST", fonts.bodyFont, 26, new Vector2(0f,-310f), new Vector2(450,50));
        prompt.gameObject.SetActive(false);
        var town = root.AddComponent<DirtmouthTown>(); town.player = knight.GetComponent<PlayerController>(); town.bench = bench.transform;
        town.prompt = prompt; town.title = titleRoot.GetComponent<CanvasGroup>(); town.title.alpha = 0f;

        parent = Group("07 · Music and wind", root.transform);
        foreach (var track in new[] { ("Dirtmouth 1",0.55f,true,"Dirtmouth1BG"), ("dirtmouth_wind_loop_a",0.18f,false,"dirtmouth_wind_loop_a"), ("dirtmouth_wind_loop_b_grass",0.08f,false,"dirtmouth_wind_loop_a") })
        {
            var audio = new GameObject(track.Item1, typeof(AudioSource), typeof(OptionsAudioChannel)); audio.transform.SetParent(parent, false);
            var channel = audio.GetComponent<OptionsAudioChannel>(); channel.music = track.Item3;
            var sourceAudio = audio.GetComponent<AudioSource>(); sourceAudio.clip = Resources.Load<AudioClip>("Audio/UI/" + track.Item4);
            if (sourceAudio.clip == null) throw new Exception("Missing existing audio clip " + track.Item1);
            sourceAudio.volume = track.Item2; sourceAudio.loop = true; sourceAudio.playOnAwake = true; sourceAudio.spatialBlend = 0f;
        }
        DirtmouthCentralRefinement.NormalizeTownAssets(root);
        EditorSceneManager.SaveScene(scene, ScenePath);
        var buildScenes = EditorBuildSettings.scenes.ToList();
        if (buildScenes.All(s => s.path != ScenePath)) { buildScenes.Add(new EditorBuildSettingsScene(ScenePath, true)); EditorBuildSettings.scenes = buildScenes.ToArray(); }
        AssetDatabase.SaveAssets();
        if (openedSource) EditorSceneManager.CloseScene(source, true);
        if (originalsClean)
        {
            EditorSceneManager.CloseScene(scene, true);
            EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
        }
        else if (original.IsValid() && original.isLoaded) SceneManager.SetActiveScene(original);
        Debug.Log("DIRTMOUTH BUILD PASSED: " + ScenePath);
    }
}
