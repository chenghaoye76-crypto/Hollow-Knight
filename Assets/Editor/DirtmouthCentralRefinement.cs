using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

[InitializeOnLoad]
public static class DirtmouthCentralRefinement
{
    private const string TownArt = "Assets/Resources/Sprites/Architecture & Environment/Area specfic architecture/Dirthmouth/";
    private const string LightArt = "Assets/Resources/Sprites/Particles & Effects/white_light.png";
    static DirtmouthCentralRefinement() => EditorApplication.update += Check;
    private static void Check()
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode || EditorApplication.isCompiling || EditorApplication.isUpdating || !File.Exists("Temp/RefineDirtmouth.request")) return;
        File.Delete("Temp/RefineDirtmouth.request");
        try { Refine(); File.WriteAllText("Temp/DirtmouthCentral.txt", "PASS: central square composition updated"); }
        catch (Exception error) { File.WriteAllText("Temp/DirtmouthCentral.txt", error.ToString()); Debug.LogException(error); }
    }
    private static SpriteRenderer Add(Transform group, string name, string path, Vector2 position, Vector2 scale, int order, Color colour, bool bottom = false)
    {
        var go = new GameObject(name, typeof(SpriteRenderer)); go.transform.SetParent(group, false);
        var renderer = go.GetComponent<SpriteRenderer>(); renderer.sprite = AssetDatabase.LoadAssetAtPath<Sprite>(path);
        if (renderer.sprite == null) throw new Exception("Existing asset missing: " + path);
        if (group.root.Find("Town art scale normalization") != null && path.StartsWith(TownArt,StringComparison.Ordinal)) scale *= renderer.sprite.pixelsPerUnit / 64f;
        go.transform.position = position; go.transform.localScale = new Vector3(scale.x, scale.y, 1);
        if (bottom) go.transform.position += Vector3.up * (-renderer.sprite.bounds.min.y * scale.y);
        renderer.sortingOrder = order; renderer.color = colour;
        return renderer;
    }
    private static void Place(SpriteRenderer r, Vector2 position, float scale, bool bottom = true)
    {
        if(r.transform.root.Find("Town art scale normalization")!=null && AssetDatabase.GetAssetPath(r.sprite).StartsWith(TownArt,StringComparison.Ordinal)) scale*=r.sprite.pixelsPerUnit/64f;
        r.transform.localScale = new Vector3(scale, scale, 1);
        r.transform.position = position + (bottom ? Vector2.up * (-r.sprite.bounds.min.y * scale) : Vector2.zero);
    }
    [MenuItem("Tools/Hollow Knight/Refine Dirtmouth Central Square")]
    public static void Refine()
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode) throw new Exception("Refine the authored scene outside Play Mode.");
        var scene = SceneManager.GetSceneByPath(DirtmouthBuilder.ScenePath);
        if (!scene.IsValid() || !scene.isLoaded) scene = EditorSceneManager.OpenScene(DirtmouthBuilder.ScenePath, OpenSceneMode.Additive);
        var previous = SceneManager.GetActiveScene(); SceneManager.SetActiveScene(scene);
        var root = scene.GetRootGameObjects().First(go => go.GetComponent<DirtmouthTown>() != null);
        if (root.transform.Find("08 · Central reference composition") != null)
        {
            Polish(root);
            EditorSceneManager.MarkSceneDirty(scene); EditorSceneManager.SaveScene(scene); AssetDatabase.SaveAssets();
            return;
        }
        var renderers = root.GetComponentsInChildren<SpriteRenderer>(true);
        SpriteRenderer Find(string name) => renderers.First(r => r.name == name);
        var central = new GameObject("08 · Central reference composition").transform; central.SetParent(root.transform, false);
        // Clear the central silhouette instead of stacking extra facades over the existing bench view.
        foreach (var r in renderers)
        {
            if (r.name == "Shell house behind the bench" || r.name == "Empty home west of bench") r.gameObject.SetActive(false);
            if (r.name == "Windblown verge" && Mathf.Abs(r.transform.position.x) < 16f) r.gameObject.SetActive(false);
            if (r.name == "Spiral street lamp" && Mathf.Abs(r.transform.position.x-0.9f)<0.1f) r.gameObject.SetActive(false);
            if (r.name == "Soft lamplight" && Mathf.Abs(r.transform.position.x)<2f) r.gameObject.SetActive(false);
            if ((r.name == "Far town silhouette" || r.name == "Houses in blue haze") && Mathf.Abs(r.transform.position.x) < 17f) r.gameObject.SetActive(false);
        }
        Place(Find("Rest bench"), new Vector2(0.95f,0.02f), 0.98f);
        var left = Add(central,"Central left shell house",TownArt+"town_buildings_left_0001_a.png",new Vector2(-13f,-0.2f),Vector2.one*6.4f,-20,new Color(0.67f,0.74f,0.98f),true);
        left.flipX=true;
        // Existing lit shop texture provides the tall light-filled doorway, framed by the shell facade.
        var door = Add(central,"Central left lit doorway",TownArt+"shop_BG_0002_a.png",new Vector2(-10.5f,0f),new Vector2(1.05f,1.05f),-14,new Color(0.86f,0.89f,1f),true);
        Add(central,"Left doorway light",LightArt,new Vector2(-10.5f,1.6f),new Vector2(0.8f,1.35f),-13,new Color(0.84f,0.84f,1f,0.32f));
        Add(central,"Left hanging shop sign",TownArt+"sly_shop_sign_01.png",new Vector2(-12.55f,0.08f),Vector2.one*1.1f,-9,Color.white,true);
        Add(central,"Right hanging shop sign",TownArt+"sly_shop_sign_02.png",new Vector2(-8.6f,0.08f),Vector2.one*1.1f,-9,Color.white,true);

        var stationShell=Find("Stag station shell"); Place(stationShell,new Vector2(14.6f,-0.3f),3.7f);
        stationShell.sprite=AssetDatabase.LoadAssetAtPath<Sprite>(TownArt+"town_buildings_left_0000_a.png");
        Place(stationShell,new Vector2(15.2f,-0.3f),3.7f); stationShell.color=new Color(0.58f,0.67f,0.91f);
        var entrance=Find("Stag station ornate entrance"); Place(entrance,new Vector2(10.7f,0f),1.32f);
        Add(central,"Station interior glow",LightArt,new Vector2(10.7f,2.15f),new Vector2(1.15f,1.75f),-11,new Color(0.88f,0.85f,1f,0.8f));
        Add(central,"Station inner light shaft",TownArt+"shop_BG_0002_a.png",new Vector2(10.8f,0f),new Vector2(0.95f,1.35f),-12,new Color(0.95f,0.94f,1f,0.8f),true);
        Add(central,"Lamp above the central square",TownArt+"town_lamp_BG.png",new Vector2(-1.4f,0f),Vector2.one*2.5f,-6,new Color(0.88f,0.91f,1f),true).flipX=true;
        foreach(var p in new[]{new Vector2(-3.4f,6f),new Vector2(0.7f,8f)})
        {
            Add(central,"Central lamp halo",LightArt,p,Vector2.one*1.4f,-5,new Color(0.59f,0.79f,1f,0.32f));
            Add(central,"Central lamp bright core",LightArt,p,Vector2.one*0.24f,-4,new Color(0.9f,0.97f,1f,0.8f));
        }
        // Tall, indistinct houses are visible through the central opening; no crisp foreground building fills it.
        foreach(var item in new[]{(-7f,7.0f,"Town_BG_0003_03b"),(0f,7.8f,"Town_BG_0005_02b"),(6.3f,6.8f,"Town_BG_0003_03b")})
        {
            var house=Add(central,"Central hazy house",TownArt+item.Item3+".png",new Vector2(item.Item1,0.3f),Vector2.one*item.Item2,-95,new Color(0.40f,0.49f,0.73f,0.75f),true);
            house.gameObject.AddComponent<DirtmouthAtmosphere>().parallax=0.45f;
        }
        Add(central,"Blue fog in the square",LightArt,new Vector2(0.5f,3.2f),new Vector2(7.5f,3.5f),-30,new Color(0.37f,0.51f,0.86f,0.32f));
        Add(central,"Pale mist along paving",LightArt,new Vector2(1f,0.65f),new Vector2(6f,0.75f),-25,new Color(0.65f,0.76f,1f,0.26f));
        // The reference has a broad foreground of staggered stone courses, not a single thin strip.
        for(int row=0;row<4;row++)
            for(int col=0;col<9;col++)
            {
                float x=-23f+col*6f+(row%2)*2f;
                Add(central,"Foreground stone course",TownArt+"town_floor_0"+(1+(col+row)%3)+".png",new Vector2(x,-0.6f-row*0.7f),new Vector2(1.15f,1.65f),35+row,new Color(0.48f-row*0.07f,0.57f-row*0.08f,0.86f-row*0.11f));
            }
        foreach(float x in new[]{-6.6f,4.9f,7.3f})
        {
            var grass=Add(central,"Sparse grass between paving", "Assets/Resources/Sprites/Architecture & Environment/Grass & Vines/grass_01_idle0000.png",new Vector2(x,-0.01f),Vector2.one*0.14f,25,new Color(0.12f,0.17f,0.28f),true);
            grass.gameObject.AddComponent<DirtmouthAtmosphere>().sway=2f;
        }
        var camera=scene.GetRootGameObjects().SelectMany(go=>go.GetComponentsInChildren<Camera>()).First();
        camera.orthographicSize=7.8f; camera.transform.position=new Vector3(0f,4.9f,-20f);
        var follow=camera.GetComponent<DirtmouthCamera>(); follow.height=4.9f; follow.frameCentralSquare=true;
        var canvas=scene.GetRootGameObjects().First(go=>go.name=="Town title and interaction").GetComponent<Canvas>();
        var edge=new GameObject("Soft dark framing",typeof(RectTransform),typeof(CanvasRenderer),typeof(RawImage));
        edge.transform.SetParent(canvas.transform,false); edge.transform.SetAsFirstSibling();
        var rect=edge.GetComponent<RectTransform>(); rect.anchorMin=Vector2.zero; rect.anchorMax=Vector2.one; rect.offsetMin=rect.offsetMax=Vector2.zero;
        var vignette=new Material(Shader.Find("Dirtmouth/Vignette")); AssetDatabase.CreateAsset(vignette,"Assets/Resources/Material/DirtmouthVignette.mat");
        edge.GetComponent<RawImage>().material=vignette; edge.GetComponent<RawImage>().raycastTarget=false;
        Polish(root);
        EditorSceneManager.MarkSceneDirty(scene); EditorSceneManager.SaveScene(scene); AssetDatabase.SaveAssets();
        if(previous.IsValid() && previous.isLoaded && previous!=scene) SceneManager.SetActiveScene(previous);
        Debug.Log("DIRTMOUTH CENTRAL REFINEMENT PASSED");
    }
    public static void NormalizeTownAssets(GameObject root)
    {
        if(root.transform.Find("Town art scale normalization")!=null) return;
        foreach(var r in root.GetComponentsInChildren<SpriteRenderer>(true))
        {
            if(r.sprite==null || !AssetDatabase.GetAssetPath(r.sprite).StartsWith(TownArt,StringComparison.Ordinal)) continue;
            float ratio=r.sprite.pixelsPerUnit/64f;
            var oldScale=r.transform.localScale;
            float bottom=r.transform.position.y+r.sprite.bounds.min.y*oldScale.y;
            r.transform.localScale=new Vector3(oldScale.x*ratio,oldScale.y*ratio,oldScale.z);
            var p=r.transform.position; p.y=bottom-r.sprite.bounds.min.y*r.transform.localScale.y; r.transform.position=p;
        }
        new GameObject("Town art scale normalization").transform.SetParent(root.transform,false);
    }
    private static void Polish(GameObject root)
    {
        NormalizeTownAssets(root);
        var central=root.transform.Find("08 · Central reference composition");
        var lamp=central.Find("Lamp above the central square").GetComponent<SpriteRenderer>();
        lamp.sprite=AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Resources/Sprites/Architecture & Environment/Area specfic architecture/Forgotten Crossroads/lamp_02.png");
        Place(lamp,new Vector2(-1.4f,0f),1.92f); lamp.flipX=true;
        foreach(Transform child in central)
            if(child.name=="Central lamp halo" || child.name=="Central lamp bright core") child.gameObject.SetActive(false);
        foreach(var r in root.GetComponentsInChildren<SpriteRenderer>(true))
        {
            float targetHeight=r.name=="Old grave marker" ? 2.5f : r.name=="Windblown verge" ? 0.7f : r.name=="Central hazy house" ? 8.7f : 0f;
            if(targetHeight<=0f) continue;
            float bottom=r.transform.position.y+r.sprite.bounds.min.y*r.transform.localScale.y;
            float scale=targetHeight/r.sprite.bounds.size.y;
            r.transform.localScale=new Vector3(scale,scale,1f);
            var p=r.transform.position; p.y=bottom-r.sprite.bounds.min.y*scale; r.transform.position=p;
            if(r.name=="Central hazy house") r.color=new Color(0.33f,0.42f,0.67f,0.58f);
        }
    }
}
