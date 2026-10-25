using System.Collections.Generic;
using UnityEngine;

// Quads are batched by effect, keeping the mist and animated motes to three draw calls.
[ExecuteAlways, DisallowMultipleComponent]
public sealed class DirtmouthAmbientEffects : MonoBehaviour
{
    [Range(0, 2)] public float fogIntensity = 1f;
    [Range(0, 3)] public float fogFlowSpeed = 1f;
    [Range(0, 2)] public float benchIntensity = 1f;
    [Range(1, 12)] public float distantBlur = 6f;
    [ColorUsage(false)] public Color distantTint = new Color(0.65f, 0.70f, 0.88f, 1f);
    [Range(0, 1)] public float distantContrast = 0.14f;
    public bool animateInEditor = true;
    [HideInInspector] public Material sceneryMaterial;
    private bool suspended;
    private sealed class Batch
    {
        public GameObject go;
        public Mesh mesh;
        public Material material;
        public readonly List<Vector3> vertices = new();
        public readonly List<Vector2> uv = new();
        public readonly List<Color> colors = new();
        public readonly List<int> indices = new();
        public void Clear() { vertices.Clear(); uv.Clear(); colors.Clear(); indices.Clear(); }
        public void Quad(Vector2 p, Vector2 size, Color color, float angle = 0)
        {
            int k = vertices.Count;
            float c = Mathf.Cos(angle), s = Mathf.Sin(angle);
            for (int j = 0; j < 4; j++)
            {
                float x = (j == 0 || j == 3 ? -0.5f : 0.5f) * size.x;
                float y = (j < 2 ? -0.5f : 0.5f) * size.y;
                vertices.Add(new Vector3(p.x + x*c-y*s, p.y+x*s+y*c, 0));
                uv.Add(new Vector2(j == 0 || j == 3 ? 0 : 1, j < 2 ? 0 : 1)); colors.Add(color);
            }
            indices.Add(k); indices.Add(k+1); indices.Add(k+2);
            indices.Add(k); indices.Add(k+2); indices.Add(k+3);
        }
        public void Upload() { mesh.Clear(); mesh.SetVertices(vertices); mesh.SetUVs(0,uv); mesh.SetColors(colors); mesh.SetTriangles(indices,0); mesh.RecalculateBounds(); }
    }
    private Batch mist, lights, moths;
    private Material blur;
    private readonly Dictionary<SpriteRenderer, Material> originals = new();
    private readonly Dictionary<SpriteRenderer, MaterialPropertyBlock> originalBlocks = new();
    private readonly List<Vector2> bulbs = new();
    private SpriteRenderer bench;
    private void OnEnable() { Build(); }
    public void Rebuild() { suspended=false; Release(); Build(); }
    public void SuspendRendering() { suspended=true; Release(); }
    private Batch Create(string label, int order, float mode)
    {
        var b = new Batch();
        b.go = new GameObject(label) { hideFlags = HideFlags.HideAndDontSave };
        b.go.transform.SetParent(transform, false);
        // Vertices are world coordinates; keep the generated batch origin at world zero.
        b.go.transform.position = Vector3.zero; b.go.transform.rotation = Quaternion.identity; b.go.transform.localScale = Vector3.one;
        b.mesh = new Mesh { name = label, hideFlags = HideFlags.HideAndDontSave }; b.mesh.MarkDynamic();
        b.material = new Material(Resources.Load<Shader>("DirtmouthMist")) { hideFlags = HideFlags.HideAndDontSave };
        b.material.SetFloat("_Mode", mode);
        b.go.AddComponent<MeshFilter>().sharedMesh = b.mesh;
        var r = b.go.AddComponent<MeshRenderer>(); r.sharedMaterial = b.material; r.sortingOrder = order;
        r.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off; r.receiveShadows = false;
        return b;
    }
    private void Build()
    {
        if (suspended || !gameObject.scene.IsValid() || !gameObject.scene.isLoaded) return;
        if (mist != null || Resources.Load<Shader>("DirtmouthMist") == null) return;
        mist = Create("Drifting blue ground mist", -28, 1);
        lights = Create("Bench soul motes and lamplight", 2, 0);
        moths = Create("White moths around street lamps", 3, 0);
        blur = new Material(Resources.Load<Shader>("DirtmouthDistanceBlur")) { hideFlags = HideFlags.HideAndDontSave };
        bool importedLamps=false;
        foreach(var root in gameObject.scene.GetRootGameObjects())
        foreach(var sr in root.GetComponentsInChildren<SpriteRenderer>())
            if(sr.sprite!=null && sr.sprite.name=="73" && sr.transform.parent!=null && sr.transform.parent.name=="light") importedLamps=true;
        foreach (var r in gameObject.scene.GetRootGameObjects())
        foreach (var sprite in r.GetComponentsInChildren<SpriteRenderer>(true))
        {
            if (sprite.gameObject.activeInHierarchy && sprite.sprite != null && sprite.sprite.name.ToLowerInvariant().Contains("bench") &&
                (bench == null || SortingLayer.GetLayerValueFromID(sprite.sortingLayerID)>SortingLayer.GetLayerValueFromID(bench.sortingLayerID))) bench = sprite;
            if (sprite.sortingLayerName == "Back" || sprite.name == "Far town silhouette" || sprite.name == "Houses in blue haze" || sprite.name == "Central hazy house")
            {
                if(sprite.sprite==null) continue;
                originals[sprite] = sprite.sharedMaterial != null ? sprite.sharedMaterial : sceneryMaterial;
                var previous=new MaterialPropertyBlock();sprite.GetPropertyBlock(previous);originalBlocks[sprite]=previous;
                var block=new MaterialPropertyBlock();sprite.GetPropertyBlock(block);
                Vector2 min=Vector2.one, max=Vector2.zero;
                foreach(var uv in sprite.sprite.uv){min=Vector2.Min(min,uv);max=Vector2.Max(max,uv);}
                block.SetVector("_UvRect",new Vector4(min.x,min.y,max.x,max.y));
                sprite.sharedMaterial = blur;sprite.SetPropertyBlock(block);
            }
            if (!sprite.gameObject.activeInHierarchy || sprite.sprite == null) continue;
            string art = sprite.sprite.name.ToLowerInvariant();
            bool imported=art=="73" && sprite.transform.parent!=null && sprite.transform.parent.name=="light";
            if (importedLamps ? !imported : !(sprite.name == "Lamp above the central square" || sprite.name == "Spiral street lamp" || art == "lamp_02" || art == "town_lamp_bg")) continue;
            Bounds b = sprite.bounds;
            bulbs.Add(new Vector2(Mathf.Lerp(b.min.x,b.max.x,imported?0.28f:0.14f), Mathf.Lerp(b.min.y,b.max.y,0.64f)));
            bulbs.Add(new Vector2(Mathf.Lerp(b.min.x,b.max.x,imported?0.69f:0.89f), Mathf.Lerp(b.min.y,b.max.y,0.82f)));
        }
        if (bench != null)
        {
            var mr=mist.go.GetComponent<MeshRenderer>();mr.sortingLayerID=bench.sortingLayerID;mr.sortingOrder=bench.sortingOrder-1;
            foreach(var batch in new[]{lights,moths}) {var r=batch.go.GetComponent<MeshRenderer>();r.sortingLayerID=bench.sortingLayerID;r.sortingOrder=bench.sortingOrder+20;}
        }
        Tick(9f);
    }
    private void Update()
    {
        if(suspended) return;
        if(mist==null) Build();
        if (!Application.isPlaying && !animateInEditor) return;
        Tick(Application.isPlaying ? Time.time : Time.realtimeSinceStartup);
    }
    public void Tick(float t)
    {
        if (mist == null) return;
        blur.SetFloat("_Radius",distantBlur);
        blur.SetColor("_EnvironmentTint", distantTint);
        blur.SetFloat("_ShadowDepth", distantContrast);
        float flow=t*fogFlowSpeed;
        mist.material.SetFloat("_FlowTime",flow);
        mist.Clear(); lights.Clear(); moths.Clear();
        // Broad translucent banks remain behind the architecture, keeping the knight and paving sharp.
        for (int i=0;i<13;i++)
        {
            float x=-85+Mathf.Repeat(i*14+flow*(0.38f+i%3*0.12f),182);
            mist.Quad(new Vector2(x,1.25f+Mathf.Sin(i*2+flow*0.25f)*0.3f),new Vector2(30,4.2f),new Color(0.27f,0.43f,0.73f,0.075f*fogIntensity));
            mist.Quad(new Vector2(x+4,4.3f),new Vector2(31,10),new Color(0.12f,0.19f,0.39f,0.035f*fogIntensity));
            // Narrow, higher-contrast wisps show their motion against the dark buildings.
            float wispX=-85+Mathf.Repeat(i*14.0f+flow*(0.65f+i%3*0.18f),182);
            mist.Quad(new Vector2(wispX,0.65f+i%4*0.58f+Mathf.Sin(flow*0.42f+i)*0.18f),new Vector2(13+i%3*2,1.2f+i%3*0.32f),new Color(0.38f,0.60f,0.91f,0.23f*fogIntensity),Mathf.Sin(i*1.7f)*0.035f);
        }
        Vector2 centre=bench != null ? new Vector2(bench.bounds.center.x,bench.bounds.max.y) : new Vector2(1,1.5f);
        mist.Quad(centre+Vector2.down*0.6f,new Vector2(14,4.8f),new Color(0.48f,0.75f,1f,0.27f*fogIntensity));
        for(int i=0;i<24;i++)
        {
            float phase=Mathf.Repeat(t*(0.10f+(i%4)*0.009f)+i*0.618034f,1);
            float x=centre.x+Mathf.Sin(i*12.989f)*1.25f+Mathf.Sin(t*0.65f+i)*0.17f;
            float y=centre.y-0.6f+phase*3.25f;
            float fade=Mathf.Sin(phase*Mathf.PI);fade*=fade;
            float size=Mathf.Lerp(0.10f,0.30f,(Mathf.Sin(i*7.1f)+1)*0.5f);
            var p=new Vector2(x,y);
            lights.Quad(p,Vector2.one*size*3,new Color(0.42f,0.77f,1,fade*0.18f*benchIntensity));
            lights.Quad(p,Vector2.one*size,new Color(0.88f,0.98f,1,fade*0.95f*benchIntensity));
        }
        for(int i=0;i<bulbs.Count;i++)
        {
            Vector2 bulb=bulbs[i];
            lights.Quad(bulb,Vector2.one*3.5f,new Color(0.49f,0.66f,1,0.17f));
            for(int j=0;j<4;j++)
            {
                float seed=i*3.17f+j*2.4f;
                Vector2 p=bulb+new Vector2(Mathf.Sin(t*0.63f+seed)*(0.4f+j*0.18f),Mathf.Cos(t*0.83f+seed)*0.5f-0.18f);
                float wing=0.035f+Mathf.Abs(Mathf.Sin(t*(12+j)+seed))*0.12f;
                float bob=Mathf.Sin(t*1.7f+seed)*0.18f;
                moths.Quad(p,Vector2.one*0.34f,new Color(0.57f,0.78f,1,0.18f));
                moths.Quad(p+Vector2.left*wing*0.27f,new Vector2(wing,0.065f),new Color(0.9f,0.98f,1,0.9f),0.35f+bob);
                moths.Quad(p+Vector2.right*wing*0.27f,new Vector2(wing,0.065f),new Color(0.9f,0.98f,1,0.9f),-0.35f+bob);
            }
        }
        mist.Upload(); lights.Upload(); moths.Upload();
    }
    private void OnDisable() { Release(); }
    private static void Dispose(Object obj) { if(obj==null)return; if(Application.isPlaying) Destroy(obj); else DestroyImmediate(obj); }
    private void Release()
    {
        foreach(var pair in originals) if(pair.Key != null && pair.Key.sharedMaterial == blur) {pair.Key.sharedMaterial=pair.Value;if(originalBlocks.TryGetValue(pair.Key,out var block))pair.Key.SetPropertyBlock(block);}
        originals.Clear();originalBlocks.Clear();bulbs.Clear();bench=null;
        foreach(var b in new[]{mist,lights,moths}) if(b!=null){Dispose(b.go);Dispose(b.mesh);Dispose(b.material);}
        Dispose(blur);blur=null;mist=lights=moths=null;
    }
}
