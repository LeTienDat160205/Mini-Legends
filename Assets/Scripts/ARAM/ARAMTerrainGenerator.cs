
using UnityEngine;
using UnityEngine.Tilemaps;

public class ARAMTerrainGenerator : MonoBehaviour
{
    [Header("=== TILEMAP REFERENCES ===")]
    public Tilemap backgroundTilemap;
    public Tilemap groundTilemap;
    public Tilemap snowTilemap;
    public Tilemap floorDecorationTilemap;

    [Header("=== TILE ASSETS ===")]
    public TileBase[] groundTiles;
    public TileBase[] backgroundTiles;
    public TileBase[] snowTiles;
    public TileBase[] decorationTiles;

    [Header("=== BRIDGE SETTINGS ===")]
    [Min(1)]
    public int laneLength = 60;

    [Min(3)]
    public int laneWidth = 12;

    [Header("=== BASE SETTINGS ===")]
    [Min(1)]
    public int baseLength = 14;

    [Min(3)]
    public int baseWidth = 24;

    [Header("=== BACKGROUND SETTINGS ===")]
    [Min(0)]
    public int backgroundPadding = 15;

    [Header("=== TERRAIN SETTINGS ===")]
    [Range(0f, 1f)]
    public float snowChance = 0.35f;

    [Range(0f, 1f)]
    public float decorationChance = 0.03f;

    public int seed = 1234;

    private System.Random random;

    // ==============================
    // GENERATE MAP
    // ==============================

    [ContextMenu("Generate Terrain")]
    public void GenerateTerrain()
    {
        if (!ValidateSettings())
            return;

        ClearTerrain();

        random = new System.Random(seed);

        GenerateBackground();
        GenerateLane();
        GenerateBases();

        Debug.Log("[ARAM] Generate Terrain successfully!");
    }

    // ==============================
    // BACKGROUND
    // ==============================

    private void GenerateBackground()
    {
        int minX = -baseLength - backgroundPadding;

        int maxX = laneLength + baseLength
                   + backgroundPadding;

        int halfWidth =
            Mathf.Max(baseWidth, laneWidth) / 2;

        int minY = -halfWidth - backgroundPadding;

        int maxY = Mathf.Max(baseWidth, laneWidth)
                   - halfWidth + backgroundPadding;

        for (int x = minX; x < maxX; x++)
        {
            for (int y = minY; y < maxY; y++)
            {
                Vector3Int position =
                    new Vector3Int(x, y, 0);

                backgroundTilemap.SetTile(
                    position,
                    GetRandomTile(backgroundTiles)
                );
            }
        }
    }

    // ==============================
    // MAIN LANE
    // ==============================

    private void GenerateLane()
    {
        FillGroundArea(
            0,
            laneLength,
            laneWidth
        );
    }

    // ==============================
    // BLUE AND RED BASE
    // ==============================

    private void GenerateBases()
    {
        // Blue Base
        FillGroundArea(
            -baseLength,
            0,
            baseWidth
        );

        // Red Base
        FillGroundArea(
            laneLength,
            laneLength + baseLength,
            baseWidth
        );
    }

    // ==============================
    // GROUND AREA
    // ==============================

    private void FillGroundArea(
        int minX,
        int maxX,
        int width)
    {
        int minY = -width / 2;
        int maxY = minY + width - 1;

        for (int x = minX; x < maxX; x++)
        {
            for (int y = minY; y <= maxY; y++)
            {
                Vector3Int position =
                    new Vector3Int(x, y, 0);

                // Ground
                groundTilemap.SetTile(
                    position,
                    GetRandomTile(groundTiles)
                );

                bool isEdge =
                    y == minY || y == maxY;

                // Snow near edges
                if (isEdge)
                {
                    if (random.NextDouble() < snowChance)
                    {
                        snowTilemap.SetTile(
                            position,
                            GetRandomTile(snowTiles)
                        );
                    }
                }

                // Random floor decorations
                else
                {
                    if (random.NextDouble() < decorationChance)
                    {
                        floorDecorationTilemap.SetTile(
                            position,
                            GetRandomTile(decorationTiles)
                        );
                    }
                }
            }
        }
    }

    // ==============================
    // RANDOM TILE
    // ==============================

    private TileBase GetRandomTile(TileBase[] tiles)
    {
        if (tiles == null || tiles.Length == 0)
            return null;

        return tiles[random.Next(tiles.Length)];
    }

    // ==============================
    // CLEAR TERRAIN
    // ==============================

    [ContextMenu("Clear Terrain")]
    public void ClearTerrain()
    {
        if (backgroundTilemap != null)
            backgroundTilemap.ClearAllTiles();

        if (groundTilemap != null)
            groundTilemap.ClearAllTiles();

        if (snowTilemap != null)
            snowTilemap.ClearAllTiles();

        if (floorDecorationTilemap != null)
            floorDecorationTilemap.ClearAllTiles();
    }

    // ==============================
    // VALIDATION
    // ==============================

    private bool ValidateSettings()
    {
        if (backgroundTilemap == null ||
            groundTilemap == null ||
            snowTilemap == null ||
            floorDecorationTilemap == null)
        {
            Debug.LogError("Please assign all Tilemaps!");
            return false;
        }

        if (groundTiles == null ||
            groundTiles.Length == 0 ||
            backgroundTiles == null ||
            backgroundTiles.Length == 0)
        {
            Debug.LogError("Please assign Ground and Background Tiles!");
            return false;
        }

        if (baseWidth < laneWidth ||
            laneLength < 1 ||
            baseLength < 1 ||
            laneWidth < 3 ||
            backgroundPadding < 0)
        {
            Debug.LogError("Invalid map dimensions!");
            return false;
        }

        return true;
    }
}

#if UNITY_EDITOR

[UnityEditor.CustomEditor(typeof(ARAMTerrainGenerator))]
public class ARAMTerrainGeneratorEditor : UnityEditor.Editor
{
    public override void OnInspectorGUI()
    {
        DrawDefaultInspector();

        ARAMTerrainGenerator generator =
            (ARAMTerrainGenerator)target;

        GUILayout.Space(15);

        GUI.backgroundColor = new Color(0.3f, 0.8f, 0.4f);

        if (GUILayout.Button("Generate Terrain", GUILayout.Height(40)))
        {
            RegisterUndo(generator);

            generator.GenerateTerrain();

            UnityEditor.SceneManagement.EditorSceneManager
                .MarkSceneDirty(generator.gameObject.scene);
        }

        GUI.backgroundColor = new Color(0.9f, 0.4f, 0.4f);

        if (GUILayout.Button("Clear Terrain", GUILayout.Height(30)))
        {
            RegisterUndo(generator);

            generator.ClearTerrain();

            UnityEditor.SceneManagement.EditorSceneManager
                .MarkSceneDirty(generator.gameObject.scene);
        }

        GUI.backgroundColor = Color.white;
    }

    private void RegisterUndo(ARAMTerrainGenerator generator)
    {
        Tilemap[] tilemaps =
        {
            generator.backgroundTilemap,
            generator.groundTilemap,
            generator.snowTilemap,
            generator.floorDecorationTilemap
        };

        foreach (Tilemap tilemap in tilemaps)
        {
            if (tilemap != null)
            {
                UnityEditor.Undo.RegisterCompleteObjectUndo(
                    tilemap,
                    "Edit ARAM Terrain"
                );
            }
        }
    }
}

#endif
