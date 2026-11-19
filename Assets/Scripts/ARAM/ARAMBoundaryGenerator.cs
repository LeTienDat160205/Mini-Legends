
using UnityEngine;

[RequireComponent(typeof(EdgeCollider2D))]
public class ARAMBoundaryGenerator : MonoBehaviour
{
    [Header("=== REFERENCES ===")]
    public ARAMTerrainGenerator terrainGenerator;

    [Header("=== BOUNDARY SETTINGS ===")]
    [Min(0f)]
    public float boundaryInset = 0.3f;

    private EdgeCollider2D edgeCollider;

    [ContextMenu("Generate Boundary")]
    public void GenerateBoundary()
    {
        if (terrainGenerator == null ||
            terrainGenerator.groundTilemap == null)
        {
            Debug.LogError("Missing Terrain Generator!");
            return;
        }

        edgeCollider = GetComponent<EdgeCollider2D>();

        float length = terrainGenerator.laneLength;
        float baseLength = terrainGenerator.baseLength;

        float laneBottom = -(terrainGenerator.laneWidth / 2);
        float laneTop = laneBottom + terrainGenerator.laneWidth;

        float baseBottom = -(terrainGenerator.baseWidth / 2);
        float baseTop = baseBottom + terrainGenerator.baseWidth;

        float inset = boundaryInset;

        if (inset * 2 >= Mathf.Min(
            length, baseLength, terrainGenerator.laneWidth))
        {
            Debug.LogError("Boundary Inset is too large!");
            return;
        }

        // Tọa độ đường biên theo Grid
        Vector2[] points = new Vector2[]
        {
            // Blue Base
            new Vector2(-baseLength + inset, baseBottom + inset),
            new Vector2(-inset, baseBottom + inset),
            new Vector2(-inset, laneBottom + inset),

            // Phía dưới cầu
            new Vector2(length + inset, laneBottom + inset),

            // Red Base
            new Vector2(length + inset, baseBottom + inset),
            new Vector2(length + baseLength - inset, baseBottom + inset),
            new Vector2(length + baseLength - inset, baseTop - inset),
            new Vector2(length + inset, baseTop - inset),
            new Vector2(length + inset, laneTop - inset),

            // Phía trên cầu
            new Vector2(-inset, laneTop - inset),

            // Blue Base
            new Vector2(-inset, baseTop - inset),
            new Vector2(-baseLength + inset, baseTop - inset),

            // Khép kín đường biên
            new Vector2(-baseLength + inset, baseBottom + inset)
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

            points[i] = transform.InverseTransformPoint(worldPosition);
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
            generator.GetComponent<EdgeCollider2D>().enabled = true;

            generator.GenerateBoundary();
        }

        if (GUILayout.Button("Clear Boundary"))
        {
            generator.ClearBoundary();
        }
    }
}

#endif
