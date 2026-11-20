using UnityEngine;

[RequireComponent(typeof(EdgeCollider2D))]
public class ARAMBoundaryGenerator : MonoBehaviour
{
    [Header("=== REFERENCES ===")]
    public ARAMTerrainGenerator terrainGenerator;

    [Header("=== BOUNDARY SETTINGS ===")]
    [Range(-5f, 5f)]
    public float sideInset = 0.3f;

    [Range(-5f, 5f)]
    public float topInset = 0.3f;

    [Range(-5f, 5f)]
    public float bottomInset = 0.3f;

    private EdgeCollider2D edgeCollider;

    [ContextMenu("Generate Boundary")]
    public void GenerateBoundary()
    {
        if (terrainGenerator == null ||
            terrainGenerator.groundTilemap == null)
        {
            Debug.LogError("[ARAM] Missing Terrain Generator!");
            return;
        }

        edgeCollider = GetComponent<EdgeCollider2D>();

        float length = terrainGenerator.laneLength;
        float baseLength = terrainGenerator.baseLength;

        // Dùng float để xử lý đúng cả kích thước lẻ
        float laneBottom = -(terrainGenerator.laneWidth / 2);
        float laneTop = laneBottom + terrainGenerator.laneWidth;

        float baseBottom = -(terrainGenerator.baseWidth / 2);
        float baseTop = baseBottom + terrainGenerator.baseWidth;

        float leftRight = sideInset;
        float top = topInset;
        float bottom = bottomInset;

        // Kiểm tra giá trị Inset
        if (top + bottom >= terrainGenerator.laneWidth ||
            top + bottom >= terrainGenerator.baseWidth ||
            leftRight * 2f >= baseLength)
        {
            Debug.LogError("[ARAM] Boundary Insets are too large!");
            return;
        }

        // Tọa độ đường biên theo Grid
        Vector2[] points = new Vector2[]
        {
            // Căn cứ xanh - mép dưới
            new Vector2(-baseLength + leftRight, baseBottom + bottom),
            new Vector2(0f - leftRight, baseBottom + bottom),

            // Chuyển từ căn cứ xanh sang cầu
            new Vector2(0f - leftRight, laneBottom + bottom),

            // Mép dưới cầu
            new Vector2(length + leftRight, laneBottom + bottom),

            // Căn cứ đỏ - mép dưới
            new Vector2(length + leftRight, baseBottom + bottom),
            new Vector2(length + baseLength - leftRight, baseBottom + bottom),

            // Mép ngoài bên phải căn cứ đỏ
            new Vector2(length + baseLength - leftRight, baseTop - top),

            // Căn cứ đỏ - mép trên
            new Vector2(length + leftRight, baseTop - top),

            // Chuyển từ căn cứ đỏ sang cầu
            new Vector2(length + leftRight, laneTop - top),

            // Mép trên cầu
            new Vector2(0f - leftRight, laneTop - top),

            // Căn cứ xanh - mép trên
            new Vector2(0f - leftRight, baseTop - top),
            new Vector2(-baseLength + leftRight, baseTop - top),

            // Khép kín đường biên
            new Vector2(-baseLength + leftRight, baseBottom + bottom)
        };

        // Chuyển tọa độ Grid sang tọa độ Collider
        var tilemap = terrainGenerator.groundTilemap;

        for (int i = 0; i < points.Length; i++)
        {
            Vector3 cellPosition = new Vector3(
                points[i].x,
                points[i].y,
                0f
            );

            Vector3 localPosition =
                tilemap.CellToLocalInterpolated(cellPosition);

            Vector3 worldPosition =
                tilemap.transform.TransformPoint(localPosition);

            points[i] = transform.InverseTransformPoint(
                worldPosition
            );
        }

        edgeCollider.points = points;
        edgeCollider.offset = Vector2.zero;
        edgeCollider.isTrigger = false;
        edgeCollider.enabled = true;

#if UNITY_EDITOR
        UnityEditor.EditorUtility.SetDirty(edgeCollider);

        if (!Application.isPlaying)
        {
            UnityEditor.SceneManagement.EditorSceneManager
                .MarkSceneDirty(gameObject.scene);
        }
#endif

        Debug.Log("[ARAM] Boundary generated successfully!");
    }

    [ContextMenu("Clear Boundary")]
    public void ClearBoundary()
    {
        EdgeCollider2D collider =
            GetComponent<EdgeCollider2D>();

        collider.enabled = false;
    }
}

#if UNITY_EDITOR

[UnityEditor.CustomEditor(typeof(ARAMBoundaryGenerator))]
public class ARAMBoundaryGeneratorEditor : UnityEditor.Editor
{
    public override void OnInspectorGUI()
    {
        DrawDefaultInspector();

        ARAMBoundaryGenerator generator =
            (ARAMBoundaryGenerator)target;

        GUILayout.Space(10);

        if (GUILayout.Button(
            "Generate Boundary", GUILayout.Height(35)))
        {
            generator.GenerateBoundary();
        }

        if (GUILayout.Button("Clear Boundary"))
        {
            generator.ClearBoundary();
        }
    }
}

#endif
