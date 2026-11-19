using UnityEngine;

public class ARAMBridgeGenerator : MonoBehaviour
{
    [Header("=== REFERENCES ===")]
    public ARAMTerrainGenerator terrainGenerator;

    public Transform bridgeArchitecture;
    public Transform walls;

    [Header("=== BRIDGE SPRITES ===")]
    public Sprite bridgeArch;
    public Sprite bridgeSupport;

    public Sprite[] wallSprites;
    public Sprite[] icicleSprites;

    [Header("=== SIZE SETTINGS ===")]
    [Min(0.5f)]
    public float moduleWidth = 4f;

    [Min(0.1f)]
    public float supportWidth = 1f;

    [Tooltip("Enable only if the arch sprite has no built-in supports.")]
    public bool generateExtraSupports = false;

    [Min(0.1f)]
    public float icicleWidth = 1.5f;

    [Header("=== POSITION SETTINGS ===")]
    [Tooltip("Additional offset away from the lane on both sides.")]
    public float wallOffset = 0f;
    public float archOffset = 0.2f;
    public float icicleOffset = 0.8f;

    [Header("=== ALIGNMENT SETTINGS ===")]
    [Min(0f)]
    public float wallOverlap = 0.8f;
    [Min(0f)]
    public float archOverlap = 0.65f;
    public float topWallOffset = -0.15f;
    public float bottomWallOffset = -0.3f;

    [Header("=== RANDOM SETTINGS ===")]
    [Range(0f, 1f)]
    public float icicleChance = 0.4f;

    public int seed = 1234;

    private const string GeneratedName = "GeneratedBridge";
    private System.Random random;

    private Transform architectureRoot;
    private Transform wallRoot;

    // ==============================
    // GENERATE BRIDGE
    // ==============================

    [ContextMenu("Generate Bridge")]
    public void GenerateBridge()
    {
        if (!ValidateSettings())
            return;

        ClearBridge();

        random = new System.Random(seed);

        architectureRoot = CreateContainer(bridgeArchitecture);
        wallRoot = CreateContainer(walls);

        GenerateArches();
        GenerateWalls();

        MarkSceneDirty();

        Debug.Log("[ARAM] Generate Bridge successfully!");
    }

    // ==============================
    // GENERATE ARCHES
    // ==============================

    private void GenerateArches()
    {
        float length = terrainGenerator.laneLength;
        float bottomY = -(terrainGenerator.laneWidth / 2); // matches terrain integer division

        int count = Mathf.CeilToInt(length / moduleWidth);
        float width = length / count;

        for (int i = 0; i < count; i++)
        {
            float centerX = (i + 0.5f) * width;

            if (bridgeArch != null)
            {
                CreateSprite(
                    "Bridge_Arch_" + i,
                    bridgeArch,
                    centerX,
                    bottomY + archOffset,
                    width + archOverlap,
                    true,
                    20,
                    architectureRoot
                );
            }

            if (generateExtraSupports && bridgeSupport != null)
            {
                CreateSprite(
                    "Bridge_Support_" + i,
                    bridgeSupport,
                    i * width,
                    bottomY + archOffset,
                    supportWidth,
                    true,
                    21,
                    architectureRoot
                );
            }

            Sprite icicle = GetRandomSprite(icicleSprites);

            if (icicle != null &&
                random.NextDouble() < icicleChance)
            {
                CreateSprite(
                    "Icicle_" + i,
                    icicle,
                    centerX,
                    bottomY - icicleOffset,
                    icicleWidth,
                    true,
                    22,
                    architectureRoot
                );
            }
        }

        if (generateExtraSupports && bridgeSupport != null)
        {
            CreateSprite(
                "Bridge_Support_End",
                bridgeSupport,
                length,
                bottomY + archOffset,
                supportWidth,
                true,
                21,
                architectureRoot
            );
        }
    }

    // ==============================
    // GENERATE WALLS
    // ==============================

    private void GenerateWalls()
    {
        Sprite wall = GetFirstValidSprite(wallSprites);
        if (wall == null) return;

        float length = terrainGenerator.laneLength;
        float bottomY = -(terrainGenerator.laneWidth / 2);
        float topY = bottomY + terrainGenerator.laneWidth;

        int count = Mathf.CeilToInt(length / moduleWidth);
        float width = length / count;
        float actualWidth = width + wallOverlap;

        for (int i = 0; i < count; i++)
        {
            float centerX = (i + 0.5f) * width;

            CreateSprite("Wall_Top_" + i, wall,
                centerX, topY + topWallOffset + wallOffset,
                actualWidth, false, 60, wallRoot);

            CreateSprite("Wall_Bottom_" + i, wall,
                centerX, bottomY + bottomWallOffset - wallOffset,
                actualWidth, false, 61, wallRoot);
        }
    }

    private Sprite GetFirstValidSprite(Sprite[] sprites)
    {
        if (sprites == null) return null;
        foreach (Sprite sprite in sprites)
            if (sprite != null) return sprite;
        return null;
    }

    // ==============================
    // CREATE SPRITE
    // ==============================

    private void CreateSprite(
        string objectName,
        Sprite sprite,
        float x,
        float y,
        float targetWidth,
        bool anchorTop,
        int sortingOrder,
        Transform parent)
    {
        if (sprite == null)
            return;

        GameObject obj = new GameObject(objectName);

        obj.transform.SetParent(parent, false);
        obj.transform.rotation = Quaternion.identity;

        SpriteRenderer renderer =
            obj.AddComponent<SpriteRenderer>();

        renderer.sprite = sprite;
        renderer.sortingLayerName = "Default";
        renderer.sortingOrder = sortingOrder;

        Bounds bounds = sprite.bounds;

        float scale = targetWidth / bounds.size.x;

        obj.transform.localScale =
            new Vector3(scale, scale, 1f);

        Vector3 anchor = new Vector3(
            bounds.center.x,
            anchorTop ? bounds.max.y : bounds.min.y,
            0f
        );

        Vector3 gridPosition =
            terrainGenerator.groundTilemap
                .CellToWorld(Vector3Int.zero);

        Vector3 target = gridPosition + new Vector3(x, y, 0f);

        obj.transform.position =
            target - obj.transform.TransformVector(anchor);
    }

    // ==============================
    // RANDOM SPRITE
    // ==============================

    private Sprite GetRandomSprite(Sprite[] sprites)
    {
        if (sprites == null || sprites.Length == 0)
            return null;

        return sprites[random.Next(sprites.Length)];
    }

    // ==============================
    // CREATE CONTAINER
    // ==============================

    private Transform CreateContainer(Transform parent)
    {
        GameObject container = new GameObject(GeneratedName);

        container.transform.SetParent(parent, false);

        return container.transform;
    }

    // ==============================
    // CLEAR BRIDGE
    // ==============================

    [ContextMenu("Clear Bridge")]
    public void ClearBridge()
    {
        ClearContainer(bridgeArchitecture);
        ClearContainer(walls);

        MarkSceneDirty();
    }

    private void ClearContainer(Transform parent)
    {
        if (parent == null)
            return;

        for (int i = parent.childCount - 1; i >= 0; i--)
        {
            Transform child = parent.GetChild(i);

            if (child.name != GeneratedName)
                continue;

            if (Application.isPlaying)
            {
                child.gameObject.SetActive(false);
                child.SetParent(null);
                Destroy(child.gameObject);
            }
            else
            {
                DestroyImmediate(child.gameObject);
            }
        }
    }

    // ==============================
    // VALIDATION
    // ==============================

    private bool ValidateSettings()
    {
        if (terrainGenerator == null ||
            terrainGenerator.groundTilemap == null ||
            bridgeArchitecture == null ||
            walls == null ||
            bridgeArchitecture == walls)
        {
            Debug.LogError("[ARAM] Invalid bridge references!");
            return false;
        }

        if (bridgeArch == null || GetFirstValidSprite(wallSprites) == null)
        {
            Debug.LogError("[ARAM] Assign Bridge Arch and at least one straight Wall Sprite!");
            return false;
        }

        if (terrainGenerator.laneLength <= 0 ||
            terrainGenerator.laneWidth <= 0 ||
            moduleWidth <= 0 ||
            supportWidth <= 0 ||
            icicleWidth <= 0 ||
            wallOverlap < 0 || archOverlap < 0)
        {
            Debug.LogError("[ARAM] Invalid bridge dimensions!");
            return false;
        }

        // This version is designed for the horizontal 1x1 terrain.
        var map = terrainGenerator.groundTilemap;
        Vector3 origin = map.CellToWorld(Vector3Int.zero);
        if ((map.CellToWorld(Vector3Int.right) - origin - Vector3.right).sqrMagnitude > 0.0001f ||
            (map.CellToWorld(Vector3Int.up) - origin - Vector3.up).sqrMagnitude > 0.0001f ||
            (bridgeArchitecture.lossyScale - Vector3.one).sqrMagnitude > 0.0001f ||
            (walls.lossyScale - Vector3.one).sqrMagnitude > 0.0001f)
        {
            Debug.LogError("[ARAM] Use an unrotated 1x1 Grid and unit-scale environment parents.");
            return false;
        }

        return true;
    }

    private void MarkSceneDirty()
    {
#if UNITY_EDITOR
        if (!Application.isPlaying)
        {
            UnityEditor.SceneManagement.EditorSceneManager
                .MarkSceneDirty(gameObject.scene);
        }
#endif
    }
}

#if UNITY_EDITOR

[UnityEditor.CustomEditor(typeof(ARAMBridgeGenerator))]
public class ARAMBridgeGeneratorEditor : UnityEditor.Editor
{
    public override void OnInspectorGUI()
    {
        DrawDefaultInspector();

        ARAMBridgeGenerator generator =
            (ARAMBridgeGenerator)target;

        GUILayout.Space(15);

        GUI.backgroundColor = new Color(0.3f, 0.8f, 0.4f);

        if (GUILayout.Button("Generate Bridge", GUILayout.Height(40)))
        {
            generator.GenerateBridge();
        }

        GUI.backgroundColor = new Color(0.9f, 0.4f, 0.4f);

        if (GUILayout.Button("Clear Bridge", GUILayout.Height(30)))
        {
            generator.ClearBridge();
        }

        GUI.backgroundColor = Color.white;
    }
}

#endif
