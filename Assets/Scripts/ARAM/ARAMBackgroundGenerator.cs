using UnityEngine;
using UnityEngine.Tilemaps;
#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.SceneManagement;
#endif

// Decorative background only; generate in Edit Mode, on an unrotated 1x1 grid.
[DisallowMultipleComponent]
public class ARAMBackgroundGenerator : MonoBehaviour
{
    [Header("References")]
    public ARAMTerrainGenerator terrainGenerator;
    public Transform backgroundRoot;
    public Transform oldCliffs; // optional: Environment/Cliffs/GeneratedEnvironment

    [Header("Sprites (existing assets)")]
    public Sprite[] cliffSprites;
    public Sprite[] ruinSprites;
    public Sprite[] waterfallSprites;
    public Sprite floatingRock;
    public Sprite customBackdrop; // optional wide landscape image

    [Header("Coverage (world units)")]
    [Min(4)] public float horizontalPadding = 16;
    [Min(8)] public float verticalPadding = 25;
    [Min(4)] public float farCliffWidth = 17;
    [Min(2)] public float nearCliffWidth = 11;
    [Min(2)] public float farSpacing = 12;
    [Min(2)] public float ruinSpacing = 16;
    [Min(1)] public float ruinWidth = 8;
    [Min(1)] public float waterfallWidth = 5;
    [Min(0)] public float landmarkClearance = 11;
    [Min(0)] public int floatingRockCount = 7;
    public int seed = 2026;
    public bool hideOldCliffs = true;

    [Header("Layer tint")]
    public Color farTint = new Color(.28f,.48f,.69f,1);
    public Color midTint = new Color(.50f,.69f,.86f,1);
    public Color nearTint = new Color(.81f,.91f,1f,1);
    public Color backdropTint = Color.white;

    const string Generated = "GeneratedBackground";
    [SerializeField, HideInInspector] TilemapRenderer savedRenderer;
    [SerializeField, HideInInspector] bool savedRendererEnabled;
    [SerializeField, HideInInspector] Transform savedCliffs;
    [SerializeField, HideInInspector] bool savedCliffsActive;
    System.Random rng;
    Vector3 origin;
    float left, right, bottom, top, laneBottom, laneTop, length, baseLength;

    [ContextMenu("Generate Background")]
    public void GenerateBackground()
    {
#if UNITY_EDITOR
        if (Application.isPlaying) { Debug.LogWarning("Generate the background in Edit Mode.", this); return; }
        if (!terrainGenerator || !terrainGenerator.groundTilemap || !terrainGenerator.backgroundTilemap || !backgroundRoot ||
            terrainGenerator.laneLength <= 0 || terrainGenerator.baseLength <= 0 || terrainGenerator.laneWidth <= 0 || terrainGenerator.baseWidth <= 0 ||
            farSpacing < 2 || ruinSpacing < 2 || horizontalPadding < 4 || verticalPadding < 8 ||
            farCliffWidth <= 0 || nearCliffWidth <= 0 || ruinWidth <= 0 || waterfallWidth <= 0)
        { Debug.LogError("Assign valid terrain and background references/settings.", this); return; }
        if (backgroundRoot == transform || backgroundRoot.IsChildOf(terrainGenerator.groundTilemap.transform) ||
            (oldCliffs && (backgroundRoot == oldCliffs || backgroundRoot.IsChildOf(oldCliffs))))
        { Debug.LogError("Use a separate BackgroundScenery root under Environment.", this); return; }
        var grid = terrainGenerator.groundTilemap;
        Vector3 o = grid.CellToWorld(Vector3Int.zero);
        if ((grid.CellToWorld(Vector3Int.right)-o-Vector3.right).sqrMagnitude > .0001f ||
            (grid.CellToWorld(Vector3Int.up)-o-Vector3.up).sqrMagnitude > .0001f ||
            backgroundRoot.rotation != Quaternion.identity || (backgroundRoot.lossyScale-Vector3.one).sqrMagnitude > .0001f)
        { Debug.LogError("Requires an unrotated 1x1 Grid and unit-scale BackgroundScenery.", this); return; }
        Sprite backdrop = customBackdrop ? customBackdrop : GetAtmosphereSprite();
        if (!backdrop) return;
        Undo.IncrementCurrentGroup();
        int undo = Undo.GetCurrentGroup(); Undo.SetCurrentGroupName("Generate ARAM background");
        ClearInternal();
        Undo.RecordObject(this, "Save background visibility");
        origin = o; rng = new System.Random(seed);
        length = terrainGenerator.laneLength; baseLength = terrainGenerator.baseLength;
        laneBottom = -(terrainGenerator.laneWidth / 2); laneTop = laneBottom + terrainGenerator.laneWidth;
        float baseBottom = -(terrainGenerator.baseWidth / 2); float baseTop = baseBottom + terrainGenerator.baseWidth;
        left = -baseLength-horizontalPadding; right = length+baseLength+horizontalPadding;
        bottom = Mathf.Min(laneBottom,baseBottom)-verticalPadding;
        top = Mathf.Max(laneTop,baseTop)+verticalPadding;
        Transform root = NewRoot(backgroundRoot,Generated);
        Transform far = NewRoot(root,"Far_Background");
        Transform mid = NewRoot(root,"Mid_Background");
        Transform near = NewRoot(root,"Near_Background");
        Place(far,"Atmosphere",backdrop,(left+right)/2,(bottom+top)/2,right-left,top-bottom,-30,backdropTint,false);
        int i=0;
        // Two staggered rows. Dark, large silhouettes sit behind the landmarks.
        for(float x=left; x<=right+farSpacing*.5f; x+=farSpacing)
        {
            Place(far,"Upper_Cliff_"+i,Pick(cliffSprites),x+Range(-3,3),top-Range(4,10),farCliffWidth*Range(.82f,1.25f),0,-22,farTint, rng.Next(2)==0);
            Place(far,"Lower_Cliff_"+i,Pick(cliffSprites),x+farSpacing*.45f,bottom+Range(3,8),farCliffWidth*Range(.85f,1.25f),0,-21,farTint,rng.Next(2)==0);
            i++;
        }
        // Lower chasm ruins. Keep the top centre clear for the existing sealed gate.
        i=0;
        for(float x=-baseLength+ruinSpacing*.5f; x<length+baseLength; x+=ruinSpacing)
        {
            float px=x+Range(-2,2);
            Place(mid,"Sunken_Ruin_"+i,Pick(ruinSprites),px,laneBottom-Range(11,18),ruinWidth*Range(.8f,1.25f),0,-10,midTint,rng.Next(2)==0);
            if(i%2==0) Place(mid,"Distant_Fall_"+i,Pick(waterfallSprites),px+3,laneBottom-Range(8,13),waterfallWidth*Range(.8f,1.2f),0,-12,midTint,false);
            if(Mathf.Abs(px-length*.5f)>length*.40f)
                Place(mid,"Outer_Ruin_"+i,Pick(ruinSprites),px,laneTop+landmarkClearance+Range(3,7),ruinWidth,0,-11,midTint,rng.Next(2)==0);
            i++;
        }
        // Frame the two bases instead of adding another regular row alongside the lane.
        float[] ends={-baseLength-2,length+baseLength+2};
        for(i=0;i<ends.Length;i++)
        {
            float dir=i==0?-1:1;
            Place(near,"Base_Upper_Cliff_"+i,Pick(cliffSprites),ends[i],baseTop+nearCliffWidth*.32f,nearCliffWidth,0,6,nearTint,i!=0);
            Place(near,"Base_Lower_Cliff_"+i,Pick(cliffSprites),ends[i],baseBottom-nearCliffWidth*.48f,nearCliffWidth*1.2f,0,6,nearTint,i==0);
            Place(near,"Outer_Cliff_"+i,Pick(cliffSprites),ends[i]+dir*nearCliffWidth*.6f,(baseTop+baseBottom)*.5f,nearCliffWidth*1.5f,0,5,nearTint,i!=0);
        }
        for(i=0;i<Mathf.Clamp(floatingRockCount,0,60);i++)
        {
            float x=Mathf.Lerp(-baseLength,length+baseLength,(i+.5f)/Mathf.Max(1,floatingRockCount))+Range(-2,2);
            Place(near,"Floating_Rock_"+i,floatingRock,x,laneBottom-Range(10,21),Range(2.2f,4.5f),0,3,nearTint,rng.Next(2)==0);
        }
        savedRenderer=terrainGenerator.backgroundTilemap.GetComponent<TilemapRenderer>();
        if(savedRenderer){ savedRendererEnabled=savedRenderer.enabled; Undo.RecordObject(savedRenderer,"Hide old tile background"); savedRenderer.enabled=false; }
        if(hideOldCliffs && oldCliffs){ savedCliffs=oldCliffs; savedCliffsActive=oldCliffs.gameObject.activeSelf; Undo.RecordObject(oldCliffs.gameObject,"Hide old cliff row"); oldCliffs.gameObject.SetActive(false); }
        EditorUtility.SetDirty(this); EditorSceneManager.MarkSceneDirty(gameObject.scene); Undo.CollapseUndoOperations(undo);
        Debug.Log("[ARAM] Layered background generated. Existing bridge, bases and landmarks preserved.",this);
#else
        Debug.LogWarning("Background generation is an editor operation.",this);
#endif
    }
    [ContextMenu("Clear Background / Restore Old Background")]
    public void ClearBackground()
    {
#if UNITY_EDITOR
        if(Application.isPlaying)return;
        Undo.IncrementCurrentGroup(); int undo=Undo.GetCurrentGroup(); Undo.SetCurrentGroupName("Clear ARAM background");
        ClearInternal(); EditorUtility.SetDirty(this); EditorSceneManager.MarkSceneDirty(gameObject.scene); Undo.CollapseUndoOperations(undo);
#endif
    }
#if UNITY_EDITOR
    void ClearInternal()
    {
        Undo.RecordObject(this,"Restore background visibility");
        if(backgroundRoot) for(int i=backgroundRoot.childCount-1;i>=0;i--)
        { Transform child=backgroundRoot.GetChild(i); if(child.name==Generated) Undo.DestroyObjectImmediate(child.gameObject); }
        if(savedRenderer){ Undo.RecordObject(savedRenderer,"Restore old tile background"); savedRenderer.enabled=savedRendererEnabled; }
        if(savedCliffs){ Undo.RecordObject(savedCliffs.gameObject,"Restore old cliffs"); savedCliffs.gameObject.SetActive(savedCliffsActive); }
        savedRenderer=null; savedCliffs=null;
    }
    Transform NewRoot(Transform parent,string name)
    {
        var go=new GameObject(name); Undo.RegisterCreatedObjectUndo(go,"Generate background group"); go.transform.SetParent(parent,false); return go.transform;
    }
    void Place(Transform parent,string name,Sprite s,float x,float y,float width,float height,int order,Color tint,bool flip)
    {
        if(!s || s.bounds.size.x<=0 || s.bounds.size.y<=0)return;
        Transform t=NewRoot(parent,name); var sr=t.gameObject.AddComponent<SpriteRenderer>();
        sr.sprite=s; sr.color=tint; sr.flipX=flip; sr.sortingLayerName="Default"; sr.sortingOrder=order;
        float sx=width/s.bounds.size.x; float sy=height>0?height/s.bounds.size.y:sx;
        t.localScale=new Vector3(sx,sy,1);
        Vector3 anchor=s.bounds.center; if(flip) anchor.x=-anchor.x;
        t.position=origin+new Vector3(x,y,0)-t.TransformVector(anchor);
    }
    float Range(float a,float b){return Mathf.Lerp(a,b,(float)rng.NextDouble());}
    Sprite Pick(Sprite[] sprites)
    {
        if(sprites==null || sprites.Length==0)return null;
        int start=rng.Next(sprites.Length);
        for(int i=0;i<sprites.Length;i++){var s=sprites[(start+i)%sprites.Length]; if(s)return s;}
        return null;
    }
    // Persistent editor asset: a continuous low-frequency atmospheric backdrop.
    // No dependence on a transient runtime texture after scene reload.
    Sprite GetAtmosphereSprite()
    {
        const string folder="Assets/ARAM/GeneratedBackgroundAssets";
        const string path=folder+"/FrozenAbyssAtmosphere.png";
        if(!AssetDatabase.IsValidFolder("Assets/ARAM")) AssetDatabase.CreateFolder("Assets","ARAM");
        if(!AssetDatabase.IsValidFolder(folder)) AssetDatabase.CreateFolder("Assets/ARAM","GeneratedBackgroundAssets");
        var existing=AssetDatabase.LoadAssetAtPath<Sprite>(path); if(existing)return existing;
        if(!System.IO.File.Exists(path))
        {
            int w=1024,h=512; var tex=new Texture2D(w,h,TextureFormat.RGBA32,false); Color32[] pixels=new Color32[w*h];
            Color deep=new Color(.035f,.085f,.17f), blue=new Color(.10f,.23f,.39f), mist=new Color(.31f,.53f,.67f);
            for(int y=0;y<h;y++) for(int x=0;x<w;x++)
            {
                float u=x/(float)(w-1), v=y/(float)(h-1);
                float n=Mathf.PerlinNoise(u*5.7f+14,v*3.8f+22);
                float m=Mathf.PerlinNoise(u*4.1f+67,v*4.4f+11);
                float band=Mathf.Exp(-Mathf.Pow((v-.20f)/.21f,2));
                float shafts=Mathf.Pow(Mathf.PerlinNoise(u*21f+2,v*.7f+9),3)*.28f;
                Color c=Color.Lerp(deep,blue,Mathf.Clamp01(n*.8f+shafts));
                c=Color.Lerp(c,mist,band*(.16f+.37f*m)); pixels[y*w+x]=c;
            }
            tex.SetPixels32(pixels); tex.Apply(); System.IO.File.WriteAllBytes(path,tex.EncodeToPNG()); DestroyImmediate(tex);
        }
        AssetDatabase.ImportAsset(path,ImportAssetOptions.ForceSynchronousImport);
        var importer=AssetImporter.GetAtPath(path) as TextureImporter;
        if(importer==null)return null;
        importer.textureType=TextureImporterType.Sprite; importer.spriteImportMode=SpriteImportMode.Single;
        importer.spritePixelsPerUnit=128; importer.mipmapEnabled=false; importer.filterMode=FilterMode.Bilinear;
        importer.wrapMode=TextureWrapMode.Clamp; importer.textureCompression=TextureImporterCompression.Uncompressed;
        importer.SaveAndReimport(); return AssetDatabase.LoadAssetAtPath<Sprite>(path);
    }
#endif
}
#if UNITY_EDITOR
[CustomEditor(typeof(ARAMBackgroundGenerator))]
public class ARAMBackgroundGeneratorEditor : Editor
{
    public override void OnInspectorGUI()
    {
        DrawDefaultInspector(); var g=(ARAMBackgroundGenerator)target;
        EditorGUILayout.Space();
        using(new EditorGUI.DisabledScope(Application.isPlaying))
        {
            if(GUILayout.Button("Generate Background",GUILayout.Height(36)))g.GenerateBackground();
            if(GUILayout.Button("Clear / Restore Old Background",GUILayout.Height(30)))g.ClearBackground();
        }
    }
}
#endif
