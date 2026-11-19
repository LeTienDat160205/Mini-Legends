using UnityEngine;

[DisallowMultipleComponent]
public class ARAMLandmarkGenerator : MonoBehaviour
{
    [Header("=== REFERENCES ===")]
    public ARAMTerrainGenerator terrainGenerator;
    public Transform ruins;

    [Header("=== SPRITES ===")]
    public Sprite ancientGate;
    public Sprite leftMonument;
    public Sprite giantStatue;
    public Sprite chainFence;
    public Sprite leftRuin;
    public Sprite rightRuin;
    public Sprite chainEnd;

    [Header("=== LANDMARK SIZE ===")]
    [Min(0.1f)] public float gateWidth = 13f;
    [Min(0.1f)] public float monumentWidth = 7f;
    [Min(0.1f)] public float statueWidth = 9f;
    [Min(0.1f)] public float sideRuinWidth = 5f;
    [Min(0.1f)] public float anchorWidth = 2f;

    [Header("=== POSITION ===")]
    public float gateOffset = 1.2f;
    public float leftOffset = 2.5f;
    public float statueOffset = 1.5f;
    [Min(0.1f)] public float monumentDistance = 17f;
    [Min(0.1f)] public float statueDistance = 17f;
    public bool flipStatue = false;
    public bool generateSideRuins = true;
    [Min(0.1f)] public float sideRuinDistance = 25f;
    public float sideRuinOffset = 1.5f;

    [Header("=== CHAINS ===")]
    public bool generateChains = true;
    public float chainTopOffset = 5.5f;
    public float chainOverlap = 1.5f;
    public float leftChainYAdjustment = 0f;
    public float rightChainYAdjustment = 0f;
    public bool generateChainAnchors = false;

    [Header("=== SORTING ===")]
    public int chainOrder = 14;
    public int landmarkOrder = 16;
    public int sideRuinOrder = 13;

    private const string RootName = "GeneratedLandmark";
    private Vector3 origin;
    private Transform root;

    [ContextMenu("Generate Landmark")]
    public void GenerateLandmark()
    {
        if (!ValidateSettings()) return;
        ClearLandmark();
        origin = terrainGenerator.groundTilemap.CellToWorld(Vector3Int.zero);
        float center = terrainGenerator.laneLength * 0.5f;
        float top = -(terrainGenerator.laneWidth / 2) + terrainGenerator.laneWidth;
        float gateY = top + gateOffset;
        float leftX = center - monumentDistance;
        float rightX = center + statueDistance;

        root = new GameObject(RootName).transform;
        root.SetParent(ruins, false);
#if UNITY_EDITOR
        if (!Application.isPlaying) UnityEditor.Undo.RegisterCreatedObjectUndo(root.gameObject, "Generate ARAM landmark");
#endif
        Place("Central_Gate", ancientGate, center, gateY, gateWidth, false, false, landmarkOrder);
        Place("Left_Monument", leftMonument, leftX, top + leftOffset, monumentWidth, false, false, landmarkOrder);
        Place("Right_Giant_Statue", giantStatue, rightX, top + statueOffset, statueWidth, false, flipStatue, landmarkOrder);

        if (generateChains && chainFence != null)
        {
            float leftA = leftX + monumentWidth * 0.5f - chainOverlap;
            float leftB = center - gateWidth * 0.5f + chainOverlap;
            float rightA = center + gateWidth * 0.5f - chainOverlap;
            float rightB = rightX - statueWidth * 0.5f + chainOverlap;
            MakeChain("Left_Seal_Chain", leftA, leftB, gateY + chainTopOffset + leftChainYAdjustment, false);
            MakeChain("Right_Seal_Chain", rightA, rightB, gateY + chainTopOffset + rightChainYAdjustment, true);
            if (generateChainAnchors && chainEnd != null)
            {
                Place("Left_Anchor", chainEnd, leftX + monumentWidth * 0.35f, top + leftOffset, anchorWidth, false, false, landmarkOrder + 1);
                Place("Right_Anchor", chainEnd, rightX - statueWidth * 0.35f, top + statueOffset, anchorWidth, false, true, landmarkOrder + 1);
            }
        }
        if (generateSideRuins)
        {
            Place("Left_Outer_Ruin", leftRuin, center - sideRuinDistance, top + sideRuinOffset, sideRuinWidth, false, false, sideRuinOrder);
            Place("Right_Outer_Ruin", rightRuin, center + sideRuinDistance, top + sideRuinOffset, sideRuinWidth, false, true, sideRuinOrder);
        }
        MarkDirty();
        Debug.Log("[ARAM] Landmark generated. Check chain anchors visually.", this);
    }

    private void MakeChain(string objectName, float a, float b, float y, bool flip)
    {
        if (b <= a) { Debug.LogWarning("[ARAM] Chain has no room: " + objectName, this); return; }
        Place(objectName, chainFence, (a + b) * 0.5f, y, b - a, true, flip, chainOrder);
    }

    private void Place(string objectName, Sprite sprite, float x, float y, float width, bool anchorTop, bool flip, int order)
    {
        if (sprite == null || width <= 0f || sprite.bounds.size.x <= 0f) return;
        GameObject obj = new GameObject(objectName);
        obj.transform.SetParent(root, false);
        SpriteRenderer sr = obj.AddComponent<SpriteRenderer>();
        sr.sprite = sprite;
        sr.sortingLayerName = "Default";
        sr.sortingOrder = order;
        sr.flipX = flip;
        Bounds b = sprite.bounds;
        float scale = width / b.size.x;
        obj.transform.localScale = new Vector3(scale, scale, 1f);
        Vector3 anchor = new Vector3(flip ? -b.center.x : b.center.x, anchorTop ? b.max.y : b.min.y, 0f);
        obj.transform.position = origin + new Vector3(x, y, 0f) - obj.transform.TransformVector(anchor);
    }

    [ContextMenu("Clear Landmark")]
    public void ClearLandmark()
    {
        if (ruins == null) return;
        for (int i = ruins.childCount - 1; i >= 0; i--)
        {
            Transform child = ruins.GetChild(i);
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

    private bool ValidateSettings()
    {
        if (terrainGenerator == null || terrainGenerator.groundTilemap == null || ruins == null || ancientGate == null || leftMonument == null || giantStatue == null)
        {
            Debug.LogError("[ARAM] Assign terrain, ruins, gate, left monument and statue.", this);
            return false;
        }
        if (terrainGenerator.laneLength <= 0 || terrainGenerator.laneWidth <= 0 || gateWidth <= 0 || monumentWidth <= 0 || statueWidth <= 0)
        {
            Debug.LogError("[ARAM] Invalid landmark dimensions.", this); return false;
        }
        if (generateChains && chainFence == null) Debug.LogWarning("[ARAM] Chain Fence is missing; chains will be skipped.", this);
        if (Quaternion.Angle(terrainGenerator.groundTilemap.transform.rotation, Quaternion.identity) > 0.01f || (terrainGenerator.groundTilemap.transform.lossyScale - Vector3.one).sqrMagnitude > 0.0001f || Quaternion.Angle(ruins.rotation, Quaternion.identity) > 0.01f || (ruins.lossyScale - Vector3.one).sqrMagnitude > 0.0001f)
        {
            Debug.LogError("[ARAM] Generate before rotating or scaling the map.", this); return false;
        }
        return true;
    }

    private void MarkDirty()
    {
#if UNITY_EDITOR
        if (!Application.isPlaying && gameObject.scene.IsValid()) UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(gameObject.scene);
#endif
    }
}

#if UNITY_EDITOR
[UnityEditor.CustomEditor(typeof(ARAMLandmarkGenerator))]
public class ARAMLandmarkGeneratorEditor : UnityEditor.Editor
{
    public override void OnInspectorGUI()
    {
        DrawDefaultInspector();
        ARAMLandmarkGenerator generator = (ARAMLandmarkGenerator)target;
        GUILayout.Space(10);
        if (GUILayout.Button("Generate Landmark", GUILayout.Height(36))) generator.GenerateLandmark();
        if (GUILayout.Button("Clear Landmark", GUILayout.Height(28))) generator.ClearLandmark();
    }
}
#endif
