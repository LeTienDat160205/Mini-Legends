using UnityEngine;

// Generates visual scenery only. Requires the existing horizontal terrain and bridge.
public class ARAMEnvironmentGenerator : MonoBehaviour
{
    [Header("=== REFERENCES ===")]
    public ARAMTerrainGenerator terrainGenerator;
    public ARAMBridgeGenerator bridgeGenerator;
    public Transform cliffs, bridgeArchitecture, ruins, bushes, props;

    [Header("=== SPRITES ===")]
    public Sprite[] cliffSprites, waterfallSprites, ruinSprites, bushSprites;
    public Sprite[] pineSprites, crystalSprites, snowSprites, brazierSprites;

    [Header("=== SIZE (WORLD UNITS) ===")]
    [Min(0.1f)] public float cliffWidth = 8f;
    [Min(0.1f)] public float waterfallWidth = 2.5f;
    [Min(0.1f)] public float ruinWidth = 5f;
    [Min(0.1f)] public float bushWidth = 2f;
    [Min(0.1f)] public float pineWidth = 2.5f;
    [Min(0.1f)] public float crystalWidth = 1.5f;
    [Min(0.1f)] public float snowWidth = 2f;
    [Min(0.1f)] public float brazierWidth = 0.9f;

    [Header("=== SPACING & OFFSETS ===")]
    [Min(1f)] public float cliffSpacing = 9f;
    [Min(1f)] public float ruinSpacing = 15f;
    [Min(1f)] public float waterfallSpacing = 14f;
    [Min(0f)] public float cliffOffset = 8f;
    [Min(0f)] public float ruinOffset = 3f;
    [Min(0f)] public float bushInset = 2f;
    [Min(1)] public int bushesPerCluster = 3;
    [Min(1)] public int brazierEveryModules = 3;
    public float waterfallOffset = 0.25f;
    public float brazierHeightOffset = 0f;
    public int seed = 2026;

    private const string RootName = "GeneratedEnvironment";
    private System.Random rng;
    private float length, bottom, top, baseLength, baseBottom, baseTop;
    private Vector3 origin;

    [ContextMenu("Generate Environment")]
    public void GenerateEnvironment()
    {
        if (!ValidateSettings()) return;
        ClearEnvironment();
        rng = new System.Random(seed);
        length = terrainGenerator.laneLength;
        bottom = -(terrainGenerator.laneWidth / 2);
        top = bottom + terrainGenerator.laneWidth;
        baseLength = terrainGenerator.baseLength;
        baseBottom = -(terrainGenerator.baseWidth / 2);
        baseTop = baseBottom + terrainGenerator.baseWidth;
        origin = terrainGenerator.groundTilemap.CellToWorld(Vector3Int.zero);

        GenerateCliffs(Container(cliffs));
        GenerateWaterfalls(Container(bridgeArchitecture));
        GenerateRuins(Container(ruins));
        GenerateBushes(Container(bushes));
        GenerateProps(Container(props));
        MarkDirty();
        Debug.Log("[ARAM] Environment generated.", this);
    }

    private void GenerateCliffs(Transform root)
    {
        int i = 0;
        for (float x = -baseLength + cliffSpacing / 2f;
             x < length + baseLength; x += cliffSpacing)
        {
            bool isBase = x < 0f || x >= length;
            float low = isBase ? baseBottom : bottom;
            float high = isBase ? baseTop : top;
            Place(root, "Cliff_Far_" + i, Pick(cliffSprites), x,
                high + cliffOffset, cliffWidth, false, 10);
            Place(root, "Cliff_Near_" + i, Pick(cliffSprites), x,
                low - cliffOffset, cliffWidth, true, 10);
            i++;
        }
    }

    private void GenerateWaterfalls(Transform root)
    {
        int i = 0;
        for (float x = waterfallSpacing / 2f; x < length; x += waterfallSpacing)
            Place(root, "Waterfall_" + i++, Pick(waterfallSprites), x,
                bottom - waterfallOffset, waterfallWidth, true, 24);
    }

    private void GenerateRuins(Transform root)
    {
        int i = 0;
        for (float x = ruinSpacing / 2f; x < length; x += ruinSpacing)
            Place(root, "Ruins_" + i++, Pick(ruinSprites), x,
                top + ruinOffset, ruinWidth, false, 15);
    }

    private void GenerateBushes(Transform root)
    {
        // Keep clusters inside the playable lane and clear the middle.
        float inset = Mathf.Clamp(bushInset, 0.5f, (top - bottom) / 2f);
        float[] centers = { length * 0.3f, length * 0.7f };
        float spacing = bushWidth * 0.78f;
        for (int c = 0; c < centers.Length; c++)
        {
            for (int j = 0; j < bushesPerCluster; j++)
            {
                float x = centers[c] + (j - (bushesPerCluster - 1) / 2f) * spacing;
                if (x < bushWidth / 2f || x > length - bushWidth / 2f) continue;
                Place(root, "Bush_Far_" + c + "_" + j, Pick(bushSprites), x,
                    top - inset, bushWidth, false, 70);
                Place(root, "Bush_Near_" + c + "_" + j, Pick(bushSprites), x,
                    bottom + inset, bushWidth, false, 70);
            }
        }
    }

    private void GenerateProps(Transform root)
    {
        // Reserve the middle of both bases for platforms in the next stage.
        float[] baseCenters = { -baseLength / 2f, length + baseLength / 2f };
        for (int i = 0; i < baseCenters.Length; i++)
        {
            float x = baseCenters[i];
            float dx = baseLength * 0.25f;
            Place(root, "Pine_Far_" + i, Pick(pineSprites), x - dx,
                baseTop - 2f, pineWidth, false, 80);
            Place(root, "Pine_Near_" + i, Pick(pineSprites), x + dx,
                baseBottom + 1f, pineWidth, false, 80);
            Place(root, "Crystal_Far_" + i, Pick(crystalSprites), x + dx,
                baseTop - 2f, crystalWidth, false, 80);
            Place(root, "Crystal_Near_" + i, Pick(crystalSprites), x - dx,
                baseBottom + 1f, crystalWidth, false, 80);
            Place(root, "Snow_Far_" + i, Pick(snowSprites), x,
                baseTop - 2f, snowWidth, false, 45);
            Place(root, "Snow_Near_" + i, Pick(snowSprites), x,
                baseBottom + 1f, snowWidth, false, 45);
        }

        // Align braziers to the bridge module seams, not random X positions.
        int modules = Mathf.Max(1, Mathf.CeilToInt(length / bridgeGenerator.moduleWidth));
        float step = length / modules;
        Sprite wall = null;
        if (bridgeGenerator.wallSprites != null)
            foreach (Sprite candidate in bridgeGenerator.wallSprites)
                if (candidate != null) { wall = candidate; break; }
        float wallHeight = wall != null && wall.bounds.size.x > 0f
            ? wall.bounds.size.y * (step + bridgeGenerator.wallOverlap) / wall.bounds.size.x : 0f;
        for (int i = brazierEveryModules; i < modules; i += brazierEveryModules)
        {
            float x = i * step;
            Place(root, "Brazier_Far_" + i, Pick(brazierSprites), x,
                top + bridgeGenerator.topWallOffset + bridgeGenerator.wallOffset + wallHeight + brazierHeightOffset,
                brazierWidth, false, 80);
            Place(root, "Brazier_Near_" + i, Pick(brazierSprites), x,
                bottom + bridgeGenerator.bottomWallOffset - bridgeGenerator.wallOffset + wallHeight + brazierHeightOffset,
                brazierWidth, false, 81);
        }
    }

    private Sprite Pick(Sprite[] sprites)
    {
        if (sprites == null || sprites.Length == 0) return null;
        int start = rng.Next(sprites.Length);
        for (int i = 0; i < sprites.Length; i++)
        {
            Sprite s = sprites[(start + i) % sprites.Length];
            if (s != null) return s;
        }
        return null;
    }

    private void Place(Transform root, string objectName, Sprite sprite,
        float x, float y, float width, bool anchorTop, int order)
    {
        if (sprite == null || sprite.bounds.size.x <= 0f) return;
        GameObject obj = new GameObject(objectName);
        obj.transform.SetParent(root, false);
        SpriteRenderer sr = obj.AddComponent<SpriteRenderer>();
        sr.sprite = sprite;
        sr.sortingLayerName = "Default";
        sr.sortingOrder = order;
        float scale = width / sprite.bounds.size.x;
        obj.transform.localScale = new Vector3(scale, scale, 1f);
        Bounds b = sprite.bounds;
        Vector3 anchor = new Vector3(b.center.x, anchorTop ? b.max.y : b.min.y, 0f);
        obj.transform.position = origin + new Vector3(x, y, 0f) - obj.transform.TransformVector(anchor);
    }

    private Transform Container(Transform parent)
    {
        GameObject go = new GameObject(RootName);
        go.transform.SetParent(parent, false);
        return go.transform;
    }

    [ContextMenu("Clear Environment")]
    public void ClearEnvironment()
    {
        foreach (Transform parent in new[] { cliffs, bridgeArchitecture, ruins, bushes, props })
        {
            if (parent == null) continue;
            for (int i = parent.childCount - 1; i >= 0; i--)
            {
                Transform child = parent.GetChild(i);
                if (child.name != RootName) continue;
                if (Application.isPlaying)
                {
                    child.gameObject.SetActive(false);
                    child.SetParent(null);
                    Destroy(child.gameObject);
                }
                else DestroyImmediate(child.gameObject);
            }
        }
        MarkDirty();
    }

    private bool ValidateSettings()
    {
        if (terrainGenerator == null || terrainGenerator.groundTilemap == null || bridgeGenerator == null ||
            cliffs == null || bridgeArchitecture == null || ruins == null || bushes == null || props == null)
        { Debug.LogError("[ARAM] Assign all Environment references.", this); return false; }
        var roots = new System.Collections.Generic.HashSet<Transform> { cliffs, bridgeArchitecture, ruins, bushes, props };
        if (roots.Count != 5 || terrainGenerator.laneLength <= 0 || terrainGenerator.laneWidth < 3 ||
            terrainGenerator.baseLength <= 0 || terrainGenerator.baseWidth < terrainGenerator.laneWidth ||
            cliffSpacing <= 0 || ruinSpacing <= 0 || waterfallSpacing <= 0 ||
            bridgeGenerator.moduleWidth <= 0 || brazierEveryModules <= 0 || bushesPerCluster <= 0 ||
            cliffWidth <= 0 || waterfallWidth <= 0 || ruinWidth <= 0 || bushWidth <= 0 ||
            pineWidth <= 0 || crystalWidth <= 0 || snowWidth <= 0 || brazierWidth <= 0)
        { Debug.LogError("[ARAM] Invalid Environment settings.", this); return false; }
        return true;
    }

    private void MarkDirty()
    {
#if UNITY_EDITOR
        if (!Application.isPlaying && gameObject.scene.IsValid())
            UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(gameObject.scene);
#endif
    }
}

#if UNITY_EDITOR
[UnityEditor.CustomEditor(typeof(ARAMEnvironmentGenerator))]
public class ARAMEnvironmentGeneratorEditor : UnityEditor.Editor
{
    public override void OnInspectorGUI()
    {
        DrawDefaultInspector();
        ARAMEnvironmentGenerator generator = (ARAMEnvironmentGenerator)target;
        GUILayout.Space(12);
        if (GUILayout.Button("Generate Environment", GUILayout.Height(40))) generator.GenerateEnvironment();
        if (GUILayout.Button("Clear Environment", GUILayout.Height(30))) generator.ClearEnvironment();
    }
}
#endif
