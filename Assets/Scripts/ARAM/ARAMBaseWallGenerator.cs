using UnityEngine;

// Rectangular base perimeter, visual only. Generate on an unrotated unit grid.
public class ARAMBaseWallGenerator : MonoBehaviour
{
    [Header("=== REFERENCES ===")]
    public ARAMTerrainGenerator terrainGenerator;
    public ARAMBridgeGenerator bridgeGenerator;
    public ARAMBaseGenerator baseGenerator;
    public Transform walls;

    [Header("=== SPRITES ===")]
    public Sprite straightWall;
    [Tooltip("Optional upright art drawn for a vertical wall. If absent, Straight Wall is rotated 90 degrees.")]
    public Sprite verticalWall;
    public Sprite wallPillar;

    [Header("=== WALL SETTINGS ===")]
    [Min(0.5f)] public float moduleWidth = 4f;
    [Min(0f)] public float wallOverlap = 0.8f;
    [Min(0f)] public float seamJoinOverlap = 0.25f;
    [Min(0f)] public float gateJoinOverlap = 0.25f;
    public bool leaveGateOpening = false;
    public float baseWallOffset = 0f;
    public float sideWallOffset = 0f;
    [Min(0.1f)] public float pillarWidth = 0.9f;
    public bool generateEndPillars = true;
    public int sortingOrder = 59;
    public int bottomSortingOrder = 61;

    private const string RootName = "GeneratedBaseWalls";
    private float z;

    [ContextMenu("Generate Base Walls")]
    public void GenerateBaseWalls()
    {
        if (terrainGenerator == null || terrainGenerator.groundTilemap == null ||
            bridgeGenerator == null || walls == null || bridgeGenerator.walls == null ||
            straightWall == null || moduleWidth <= 0 || wallOverlap < 0 ||
            terrainGenerator.baseLength <= 0 || terrainGenerator.laneLength <= 0 ||
            terrainGenerator.baseWidth < terrainGenerator.laneWidth || terrainGenerator.laneWidth <= 0)
        { Debug.LogError("[ARAM] Invalid base wall references or dimensions.", this); return; }
        var grid = terrainGenerator.groundTilemap;
        Vector3 origin = grid.CellToWorld(Vector3Int.zero);
        if ((grid.CellToWorld(Vector3Int.right) - origin - Vector3.right).sqrMagnitude > 0.0001f ||
            (grid.CellToWorld(Vector3Int.up) - origin - Vector3.up).sqrMagnitude > 0.0001f ||
            Quaternion.Angle(walls.rotation, Quaternion.identity) > 0.01f ||
            (walls.lossyScale - Vector3.one).sqrMagnitude > 0.0001f)
        { Debug.LogError("[ARAM] Generate walls before rotating the map; use unit grid and parent scale.", this); return; }
        Transform bridge = bridgeGenerator.walls.Find("GeneratedBridge");
        int count = Mathf.CeilToInt(terrainGenerator.laneLength / Mathf.Max(0.5f, bridgeGenerator.moduleWidth));
        if (bridge == null || count < 1)
        { Debug.LogError("[ARAM] Generate Bridge first.", this); return; }
        SpriteRenderer bt = FindRenderer(bridge, "Wall_Top_0");
        SpriteRenderer bb = FindRenderer(bridge, "Wall_Bottom_0");
        SpriteRenderer rt = FindRenderer(bridge, "Wall_Top_" + (count - 1));
        SpriteRenderer rb = FindRenderer(bridge, "Wall_Bottom_" + (count - 1));
        if (bt == null || bb == null || rt == null || rb == null)
        { Debug.LogError("[ARAM] Missing bridge walls. Generate Bridge again.", this); return; }
        SpriteRenderer bg = null, rg = null;
        if (leaveGateOpening)
        {
            Transform bases = baseGenerator != null && baseGenerator.basePlatforms != null
                ? baseGenerator.basePlatforms.Find("GeneratedBase") : null;
            if (bases != null) { bg = FindRenderer(bases, "Blue_Ancient_Gate"); rg = FindRenderer(bases, "Red_Ancient_Gate"); }
            if (bg == null || rg == null)
            { Debug.LogError("[ARAM] Generate Bases before using Leave Gate Opening.", this); return; }
        }
        float bottom = origin.y - (terrainGenerator.baseWidth / 2) - baseWallOffset;
        float top = origin.y - (terrainGenerator.baseWidth / 2) + terrainGenerator.baseWidth + baseWallOffset;
        z = origin.z;
        ClearBaseWalls();
        Transform root = NewObject(RootName, walls).transform;
        GenerateSide(root, "Blue", origin.x - terrainGenerator.baseLength, origin.x, bottom, top, bt, bb, bg, true);
        GenerateSide(root, "Red", origin.x + terrainGenerator.laneLength + terrainGenerator.baseLength,
            origin.x + terrainGenerator.laneLength, bottom, top, rt, rb, rg, false);
        MarkDirty();
        Debug.Log("[ARAM] Rectangular base walls generated.", this);
    }

    private void GenerateSide(Transform root, string prefix, float outer, float inner,
        float bottom, float top, SpriteRenderer bridgeTop, SpriteRenderer bridgeBottom, SpriteRenderer gate, bool blue)
    {
        float outerX = outer + (blue ? -sideWallOffset : sideWallOffset);
        float left = Mathf.Min(outerX, inner), right = Mathf.Max(outerX, inner);
        float join = Mathf.Max(0, seamJoinOverlap);
        // Horizontal walls stand on the far and near edges. Near wall straddles the rim.
        float nearY = bottom + (bridgeBottom.bounds.min.y - (terrainGenerator.groundTilemap.CellToWorld(Vector3Int.zero).y - terrainGenerator.laneWidth / 2));
        if (gate == null)
            Horizontal(root, prefix + "_Top", left, right, top, sortingOrder);
        else
        {
            Horizontal(root, prefix + "_Top_A", left, Mathf.Clamp(gate.bounds.min.x + gateJoinOverlap, left, right), top, sortingOrder);
            Horizontal(root, prefix + "_Top_B", Mathf.Clamp(gate.bounds.max.x - gateJoinOverlap, left, right), right, top, sortingOrder);
        }
        Horizontal(root, prefix + "_Bottom", left, right, nearY, bottomSortingOrder);
        // Vertical straight walls follow the exact rectangular edges. Leave the lane open.
        float wallH = straightWall.bounds.size.y / straightWall.bounds.size.x * moduleWidth;
        float topCenter = top + wallH * 0.5f;
        float bottomCenter = nearY + wallH * 0.5f;
        Vertical(root, prefix + "_Outer", outerX, bottomCenter - join, topCenter + join, bottomSortingOrder);
        Vertical(root, prefix + "_InnerTop", inner, bridgeTop.bounds.center.y - join, topCenter + join, sortingOrder);
        Vertical(root, prefix + "_InnerBottom", inner, bottomCenter - join, bridgeBottom.bounds.center.y + join, bottomSortingOrder);
        if (generateEndPillars && wallPillar != null)
        {
            Pillar(root, prefix + "_OuterTopPillar", outerX, top, sortingOrder + 1);
            Pillar(root, prefix + "_OuterBottomPillar", outerX, nearY, bottomSortingOrder + 1);
            Pillar(root, prefix + "_InnerTopPillar", inner, top, sortingOrder + 1);
            Pillar(root, prefix + "_InnerBottomPillar", inner, nearY, bottomSortingOrder + 1);
        }
    }

    private void Horizontal(Transform root, string name, float left, float right, float y, int order)
    {
        Fill(root, name, left, right, y, false, order);
    }

    private void Vertical(Transform root, string name, float x, float low, float high, int order)
    {
        if (high <= low) return;
        Fill(root, name, low, high, x, true, order);
    }

    private void Fill(Transform root, string name, float start, float end, float fixedCoord, bool vertical, int order)
    {
        float length = end - start;
        if (length <= 0.01f) return;
        int count = Mathf.Max(1, Mathf.CeilToInt(length / moduleWidth));
        float step = length / count;
        Sprite sprite = vertical && verticalWall != null ? verticalWall : straightWall;
        bool rotate = vertical && verticalWall == null;
        bool nativeVertical = vertical && !rotate;
        for (int i = 0; i < count; i++)
        {
            float overlap = Mathf.Min(wallOverlap * 0.5f, step * 0.45f);
            float a = start + i * step - (i == 0 ? 0 : overlap);
            float b = start + (i + 1) * step + (i == count - 1 ? 0 : overlap);
            float center = (a + b) * 0.5f;
            Bounds bounds = sprite.bounds;
            float along = nativeVertical ? bounds.size.y : bounds.size.x;
            float scale = (b - a) / along;
            float transverse = moduleWidth / (nativeVertical ? bounds.size.y : bounds.size.x);
            Vector3 size = nativeVertical ? new Vector3(transverse, scale, 1) : new Vector3(scale, transverse, 1);
            Vector3 anchor = vertical ? bounds.center : new Vector3(bounds.center.x, bounds.min.y, 0);
            Vector3 target = vertical ? new Vector3(fixedCoord, center, z) : new Vector3(center, fixedCoord, z);
            Place(root, name + "_" + i, sprite, target, anchor, size, rotate ? 90f : 0f, order);
        }
    }

    private void Pillar(Transform root, string name, float x, float y, int order)
    {
        Bounds b = wallPillar.bounds;
        float scale = pillarWidth / b.size.x;
        Place(root, name, wallPillar, new Vector3(x, y, z), new Vector3(b.center.x, b.min.y, 0),
            new Vector3(scale, scale, 1), 0f, order);
    }

    private void Place(Transform root, string name, Sprite sprite, Vector3 target, Vector3 anchor,
        Vector3 scale, float angle, int order)
    {
        GameObject obj = NewObject(name, root);
        SpriteRenderer sr = obj.AddComponent<SpriteRenderer>();
        sr.sprite = sprite;
        sr.sortingLayerName = "Default";
        sr.sortingOrder = order;
        obj.transform.rotation = Quaternion.Euler(0, 0, angle);
        obj.transform.localScale = scale;
        obj.transform.position = target - obj.transform.TransformVector(anchor);
    }

    private SpriteRenderer FindRenderer(Transform parent, string name)
    {
        Transform item = parent.Find(name);
        return item != null ? item.GetComponent<SpriteRenderer>() : null;
    }

    private GameObject NewObject(string name, Transform parent)
    {
        GameObject obj = new GameObject(name);
        obj.transform.SetParent(parent, false);
#if UNITY_EDITOR
        if (!Application.isPlaying) UnityEditor.Undo.RegisterCreatedObjectUndo(obj, "Generate Base Walls");
#endif
        return obj;
    }

    [ContextMenu("Clear Base Walls")]
    public void ClearBaseWalls()
    {
        if (walls == null) return;
        for (int i = walls.childCount - 1; i >= 0; i--)
        {
            Transform child = walls.GetChild(i);
            if (child.name != RootName) continue;
            if (Application.isPlaying) { child.gameObject.SetActive(false); child.SetParent(null); Destroy(child.gameObject); }
            else
            {
#if UNITY_EDITOR
                UnityEditor.Undo.DestroyObjectImmediate(child.gameObject);
#else
                DestroyImmediate(child.gameObject);
#endif
            }
        }
        MarkDirty();
    }

    private void MarkDirty()
    {
#if UNITY_EDITOR
        if (!Application.isPlaying) UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(gameObject.scene);
#endif
    }
}

#if UNITY_EDITOR
[UnityEditor.CustomEditor(typeof(ARAMBaseWallGenerator))]
public class ARAMBaseWallGeneratorEditor : UnityEditor.Editor
{
    public override void OnInspectorGUI()
    {
        DrawDefaultInspector();
        ARAMBaseWallGenerator g = (ARAMBaseWallGenerator)target;
        GUILayout.Space(10);
        if (GUILayout.Button("Generate Base Walls", GUILayout.Height(36))) Run(g.GenerateBaseWalls, "Generate Base Walls");
        if (GUILayout.Button("Clear Base Walls", GUILayout.Height(28))) Run(g.ClearBaseWalls, "Clear Base Walls");
    }
    private void Run(System.Action action, string name)
    {
        UnityEditor.Undo.IncrementCurrentGroup();
        int group = UnityEditor.Undo.GetCurrentGroup();
        UnityEditor.Undo.SetCurrentGroupName(name);
        action();
        UnityEditor.Undo.CollapseUndoOperations(group);
    }
}
#endif
