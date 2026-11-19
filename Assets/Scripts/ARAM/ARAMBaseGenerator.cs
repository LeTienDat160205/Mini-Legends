using UnityEngine;

// Visual-only base decoration. Assumes the same unrotated unit grid
// as ARAMTerrainGenerator and the existing bridge/environment generators.
public class ARAMBaseGenerator : MonoBehaviour
{
    [Header("=== REFERENCES ===")]
    public ARAMTerrainGenerator terrainGenerator;
    public Transform basePlatforms;

    [Header("=== BASE SPRITES ===")]
    public Sprite circularPlatform;
    public Sprite spawnPlatform;
    public Sprite baseStairs;
    public Sprite ancientGate;
    public Sprite centerEmblem;
    public Sprite blueBanner;
    public Sprite redBanner;

    [Header("=== SIZE (WORLD UNITS) ===")]
    [Min(0.1f)] public float platformWidth = 7f;
    [Min(0.1f)] public float spawnWidth = 3f;
    [Min(0.1f)] public float stairsWidth = 4f;
    [Min(0.1f)] public float gateWidth = 7f;
    [Min(0.1f)] public float emblemWidth = 3f;
    [Min(0.1f)] public float bannerWidth = 0.8f;

    [Header("=== POSITION SETTINGS ===")]
    public float gateInset = 0.5f;
    public float bannerHeight = 1.1f;
    public float bannerSpread = 0.36f;
    public float spawnOffset = 0f;
    public float stairsInset = 0.5f;
    public bool generateStairs = true;
    public bool generateBanners = true;

    [Header("=== PLATFORM POSITION ===")]
    [Min(0f)] public float spawnInset = 2.5f;
    [Min(0f)] public float platformDistance = 7.5f;
    public float platformYOffset = 0f;
    [Min(0f)] public float platformEdgeMargin = 0.25f;

    [Header("=== TOWER PLATFORMS (VISUAL ONLY) ===")]
    public bool generateTowerPlatforms = true;
    [Min(0.1f)] public float towerPlatformWidth = 3.5f;
    public float[] towerDistances = { 10f, 22f };
    public float towerYOffset = 0f;
    [Min(0f)] public float towerClearance = 0.5f;

    [Header("=== INHIBITOR PLATFORMS (VISUAL ONLY) ===")]
    public bool generateInhibitorPlatforms = true;
    [Min(0.1f)] public float inhibitorPlatformWidth = 3f;
    [Min(0f)] public float inhibitorDistance = 3f;
    public float inhibitorYOffset = 0f;
    [Min(0f)] public float inhibitorClearance = 0.5f;

    [Header("=== EXTRA BANNERS ===")]
    public ARAMBridgeGenerator bridgeGenerator;
    public Sprite blueBannerSmall;
    public Sprite redBannerSmall;
    public bool generateBridgeBanners = true;
    public bool bannerOnFarWall = true;
    public bool bannerOnNearWall = true;
    public bool bannerOnArches = true;
    [Min(1)] public int bannerEveryModules = 3;
    [Min(0.1f)] public float bridgeBannerWidth = 0.65f;
    [Min(0.1f)] public float archBannerWidth = 0.8f;
    [Range(0.05f, 0.45f)] public float pillarInset = 0.12f;
    public float bridgeBannerDrop = 0.25f;
    public float archBannerDrop = 0.65f;

    private const string RootName = "GeneratedBase";

    [ContextMenu("Generate Bases")]
    public void GenerateBases()
    {
        if (!ValidateSettings()) return;
        ClearBases();
        GameObject container = new GameObject(RootName);
        container.transform.SetParent(basePlatforms, false);
        Transform root = container.transform;
        GenerateSide(root, true);
        GenerateSide(root, false);
        Place(root, "Center_Emblem", centerEmblem,
            terrainGenerator.laneLength / 2f,
            -(terrainGenerator.laneWidth / 2) + terrainGenerator.laneWidth / 2f,
            Mathf.Min(emblemWidth, terrainGenerator.laneWidth * 0.7f), false, 52);
        if (generateTowerPlatforms) GenerateTowerPlatforms(root);
        if (generateInhibitorPlatforms) GenerateInhibitorPlatforms(root);
        if (generateBanners && generateBridgeBanners)
            GenerateExtraBanners(root);
        MarkDirty();
        Debug.Log("[ARAM] Base scenery generated.", this);
    }

    private void GenerateSide(Transform root, bool blue)
    {
        float length = terrainGenerator.baseLength;
        float width = terrainGenerator.baseWidth;
        float bottom = -(terrainGenerator.baseWidth / 2);
        float top = bottom + width;
        float centerY = (bottom + top) / 2f;
        float outer = blue ? -length : terrainGenerator.laneLength + length;
        float direction = blue ? 1f : -1f;
        string prefix = blue ? "Blue_" : "Red_";

        // Distances are measured inward from the outside edge of each base.
        // Clamp both pads to fit the base, preserving aspect ratio.
        float margin = Mathf.Clamp(platformEdgeMargin, 0f, Mathf.Min(length, width) * 0.1f);
        float usable = length - 2f * margin;
        float pWidth = Mathf.Min(platformWidth, length * 0.56f, width * 0.55f);
        float sWidth = Mathf.Min(spawnWidth, length * 0.28f, width * 0.3f);
        pWidth = Mathf.Min(pWidth, (width - 2f * margin) * circularPlatform.bounds.size.x / circularPlatform.bounds.size.y);
        sWidth = Mathf.Min(sWidth, (width - 2f * margin) * spawnPlatform.bounds.size.x / spawnPlatform.bounds.size.y);
        float total = pWidth + sWidth;
        if (total > usable) { pWidth *= usable / total; sWidth *= usable / total; }
        float minGap = (pWidth + sWidth) * 0.5f;
        float s = Mathf.Clamp(spawnInset, margin + sWidth * 0.5f,
            length - margin - pWidth - sWidth * 0.5f);
        float p = Mathf.Clamp(s + Mathf.Max(platformDistance, minGap),
            s + minGap, length - margin - pWidth * 0.5f);
        float spawnHalfHeight = spawnPlatform.bounds.size.y / spawnPlatform.bounds.size.x * sWidth * 0.5f;
        float platformHalfHeight = circularPlatform.bounds.size.y / circularPlatform.bounds.size.x * pWidth * 0.5f;
        float sy = Mathf.Clamp(centerY + spawnOffset, bottom + margin + spawnHalfHeight, top - margin - spawnHalfHeight);
        float py = Mathf.Clamp(centerY + platformYOffset, bottom + margin + platformHalfHeight, top - margin - platformHalfHeight);
        Place(root, prefix + "Base_Platform", circularPlatform,
            outer + direction * p, py, pWidth, false, 51);
        Place(root, prefix + "Spawn_Platform", spawnPlatform,
            outer + direction * s, sy, sWidth, false, 52);
        if (blue && Mathf.Abs(p - s - platformDistance) > 0.01f)
            Debug.LogWarning("[ARAM] Platform distance clamped to " + (p - s).ToString("F2") + " to fit base.", this);

        // Gates and stairs are scenery at the far base edge, not lane blockers.
        float gateX = outer + direction * length * 0.5f;
        float gWidth = Mathf.Min(gateWidth, length * 0.85f);
        float gateY = top - gateInset;
        Place(root, prefix + "Ancient_Gate", ancientGate,
            gateX, gateY, gWidth, true, 60);
        if (generateStairs)
            Place(root, prefix + "Base_Stairs", baseStairs,
                gateX, gateY - stairsInset, Mathf.Min(stairsWidth, gWidth), true, 53, true);

        Sprite banner = blue ? blueBanner : redBanner;
        if (generateBanners && ancientGate != null)
        {
            float spread = gWidth * Mathf.Clamp(bannerSpread, 0f, 0.48f);
            float bannerY = gateY + bannerHeight;
            Place(root, prefix + "Banner_Left", banner,
                gateX - spread, bannerY, bannerWidth, true, 81);
            Place(root, prefix + "Banner_Right", banner,
                gateX + spread, bannerY, bannerWidth, true, 81);
        }
    }

    private void GenerateTowerPlatforms(Transform root)
    {
        if (towerDistances == null || towerDistances.Length == 0) return;
        float length = terrainGenerator.laneLength;
        float laneWidth = terrainGenerator.laneWidth;
        float bottom = -(terrainGenerator.laneWidth / 2);
        float centerY = bottom + laneWidth * 0.5f;
        float ratio = circularPlatform.bounds.size.y / circularPlatform.bounds.size.x;
        float width = Mathf.Min(towerPlatformWidth, laneWidth * 0.65f, laneWidth * 0.65f / ratio);
        if (width <= 0f) return;
        float half = width * 0.5f;
        float clearance = Mathf.Max(0f, towerClearance);
        float halfHeight = width * ratio * 0.5f;
        float y = Mathf.Clamp(centerY + towerYOffset, bottom + halfHeight, bottom + laneWidth - halfHeight);
        float emblemHalf = Mathf.Min(emblemWidth, laneWidth * 0.7f) * 0.5f;
        // Leave space for the centre emblem even if the lane is resized.
        float lastAllowed = length * 0.5f - Mathf.Max(emblemHalf, half) - half - clearance;
        var distances = (float[])towerDistances.Clone();
        System.Array.Sort(distances);
        var accepted = new System.Collections.Generic.List<float>();
        foreach (float d in distances)
        {
            if (float.IsNaN(d) || float.IsInfinity(d) || d < half + clearance || d > lastAllowed ||
                (accepted.Count > 0 && d - accepted[accepted.Count - 1] < width + clearance))
            {
                Debug.LogWarning("[ARAM] Skipping tower distance " + d + ": outside lane or overlapping another platform.", this);
                continue;
            }
            accepted.Add(d);
            int index = accepted.Count;
            PlaceTower(root, "Blue_Tower_Platform_" + index, d, y, width);
            PlaceTower(root, "Red_Tower_Platform_" + index, length - d, y, width);
        }
    }

    private void PlaceTower(Transform root, string name, float x, float y, float width)
    {
        Place(root, name, circularPlatform, x, y, width, false, 51);
        Transform pad = root.Find(name);
        if (pad == null) return;
        GameObject anchor = new GameObject("TowerAnchor");
        anchor.transform.SetParent(pad, false);
        anchor.transform.localPosition = circularPlatform.bounds.center;
        // Keep a unit-scale world anchor for future tower placement.
        float scale = pad.localScale.x;
        anchor.transform.localScale = new Vector3(1f / scale, 1f / scale, 1f);
    }

    // One inhibitor per team, on the lane between the base and the first tower.
    // Validate both sides before creating either, to keep the map symmetric.
    private void GenerateInhibitorPlatforms(Transform root)
    {
        float length = terrainGenerator.laneLength;
        float laneWidth = terrainGenerator.laneWidth;
        float bottom = -(terrainGenerator.laneWidth / 2);
        float clearance = Mathf.Max(0f, inhibitorClearance);
        float ratio = circularPlatform.bounds.size.y / circularPlatform.bounds.size.x;
        float width = inhibitorPlatformWidth;
        float height = width * ratio;
        float d = inhibitorDistance;
        if (float.IsNaN(width) || float.IsInfinity(width) || width <= 0f ||
            float.IsNaN(d) || float.IsInfinity(d) ||
            float.IsNaN(inhibitorYOffset) || float.IsInfinity(inhibitorYOffset) ||
            float.IsNaN(clearance) || float.IsInfinity(clearance) ||
            height + 2f * clearance > laneWidth || d < width * 0.5f + clearance ||
            d + width * 0.5f + clearance > length * 0.5f)
        {
            Debug.LogWarning("[ARAM] Inhibitor platforms skipped: invalid size or distance.", this);
            return;
        }
        float y = Mathf.Clamp(bottom + laneWidth * 0.5f + inhibitorYOffset,
            bottom + height * 0.5f + clearance,
            bottom + laneWidth - height * 0.5f - clearance);
        Vector3 origin = terrainGenerator.groundTilemap.CellToWorld(Vector3Int.zero);
        foreach (Transform child in root)
        {
            if (!child.name.Contains("_Tower_Platform_") &&
                !child.name.EndsWith("_Base_Platform") &&
                !child.name.EndsWith("_Spawn_Platform") &&
                child.name != "Center_Emblem") continue;
            SpriteRenderer renderer = child.GetComponent<SpriteRenderer>();
            if (renderer == null) continue;
            Bounds b = renderer.bounds;
            // Even if Y offsets differ, keep the inhibitor closer to its base
            // than every generated tower on the same side.
            if ((child.name.StartsWith("Blue_Tower_Platform_") &&
                    origin.x + d + width * 0.5f + clearance > b.min.x) ||
                (child.name.StartsWith("Red_Tower_Platform_") &&
                    origin.x + length - d - width * 0.5f - clearance < b.max.x) ||
                PlatformOverlaps(b, origin.x + d, origin.y + y, width, height, clearance) ||
                PlatformOverlaps(b, origin.x + length - d, origin.y + y, width, height, clearance))
            {
                Debug.LogWarning("[ARAM] Inhibitor platforms skipped: conflict with " + child.name +
                    ". Adjust Inhibitor Distance or Width.", this);
                return;
            }
        }
        if (length - 2f * d < width + clearance)
        {
            Debug.LogWarning("[ARAM] Inhibitor platforms skipped: overlap at lane center.", this);
            return;
        }
        PlaceInhibitor(root, "Blue_Inhibitor_Platform", d, y, width);
        PlaceInhibitor(root, "Red_Inhibitor_Platform", length - d, y, width);
    }

    private bool PlatformOverlaps(Bounds b, float x, float y,
        float width, float height, float clearance)
    {
        return x + width * 0.5f + clearance > b.min.x &&
            x - width * 0.5f - clearance < b.max.x &&
            y + height * 0.5f + clearance > b.min.y &&
            y - height * 0.5f - clearance < b.max.y;
    }

    private void PlaceInhibitor(Transform root, string name, float x, float y, float width)
    {
        Place(root, name, circularPlatform, x, y, width, false, 51);
        Transform pad = root.Find(name);
        if (pad == null) return;
        GameObject anchor = new GameObject("InhibitorAnchor");
        anchor.transform.SetParent(pad, false);
        anchor.transform.localPosition = circularPlatform.bounds.center;
        float scale = pad.localScale.x;
        anchor.transform.localScale = new Vector3(1f / scale, 1f / scale, 1f);
    }

    // Use the rendered wall/arch bounds rather than guessing their positions.
    // Re-run Generate Bases after regenerating or moving the bridge.
    private void GenerateExtraBanners(Transform root)
    {
        ARAMBridgeGenerator bridge = bridgeGenerator != null
            ? bridgeGenerator : GetComponent<ARAMBridgeGenerator>();
        if (bridge == null)
        {
            Debug.LogWarning("[ARAM] Assign Bridge Generator for extra banners.", this);
            return;
        }
        int modules = Mathf.CeilToInt(terrainGenerator.laneLength /
            Mathf.Max(0.5f, bridge.moduleWidth));
        int stride = Mathf.Max(1, bannerEveryModules);
        Transform wallRoot = bridge.walls != null
            ? bridge.walls.Find("GeneratedBridge") : null;
        Transform archRoot = bridge.bridgeArchitecture != null
            ? bridge.bridgeArchitecture.Find("GeneratedBridge") : null;
        if (wallRoot == null && archRoot == null)
        {
            Debug.LogWarning("[ARAM] Generate Bridge before generating banners.", this);
            return;
        }

        // Mirror the chosen segments around the centre of the lane.
        for (int i = 1; i < modules / 2; i += stride)
        {
            PlaceModuleBanners(root, wallRoot, archRoot, i, true);
            PlaceModuleBanners(root, wallRoot, archRoot, modules - 1 - i, false);
        }
    }

    private void PlaceModuleBanners(Transform root, Transform wallRoot,
        Transform archRoot, int index, bool blue)
    {
        Sprite banner = blue ? blueBannerSmall : redBannerSmall;
        if (banner == null) banner = blue ? blueBanner : redBanner;
        string team = blue ? "Blue_" : "Red_";
        if (bannerOnFarWall)
            HangBanner(root, wallRoot, "Wall_Top_" + index,
                team + "Banner_Far_" + index, banner, blue,
                bridgeBannerWidth, bridgeBannerDrop, 82);
        if (bannerOnNearWall)
            HangBanner(root, wallRoot, "Wall_Bottom_" + index,
                team + "Banner_Near_" + index, banner, blue,
                bridgeBannerWidth, bridgeBannerDrop, 82);
        if (bannerOnArches)
            HangBanner(root, archRoot, "Bridge_Arch_" + index,
                team + "Banner_Arch_" + index, banner, blue,
                archBannerWidth, archBannerDrop, 23);
    }

    private void HangBanner(Transform root, Transform hostRoot, string hostName,
        string name, Sprite banner, bool blue, float width, float drop, int order)
    {
        if (hostRoot == null || banner == null) return;
        Transform host = hostRoot.Find(hostName);
        if (host == null) return;
        SpriteRenderer hostRenderer = host.GetComponent<SpriteRenderer>();
        if (hostRenderer == null || hostRenderer.sprite == null) return;
        Bounds b = hostRenderer.bounds;
        float fraction = Mathf.Clamp(pillarInset, 0.05f, 0.45f);
        float x = Mathf.Lerp(b.min.x, b.max.x, blue ? fraction : 1f - fraction);
        float y = b.max.y - drop;
        Vector3 origin = terrainGenerator.groundTilemap.CellToWorld(Vector3Int.zero);
        Place(root, name, banner, x - origin.x, y - origin.y,
            Mathf.Min(width, b.size.x * 0.3f), false, order, true);
    }

    // bottomAnchor=true anchors at the bottom; topAnchor=true anchors at the top.
    // Otherwise center the sprite, independent of its import pivot.
    private void Place(Transform root, string name, Sprite sprite,
        float x, float y, float targetWidth, bool bottomAnchor, int order,
        bool topAnchor = false)
    {
        if (sprite == null || sprite.bounds.size.x <= 0f || targetWidth <= 0f) return;
        GameObject obj = new GameObject(name);
        obj.transform.SetParent(root, false);
        SpriteRenderer renderer = obj.AddComponent<SpriteRenderer>();
        renderer.sprite = sprite;
        renderer.sortingLayerName = "Default";
        renderer.sortingOrder = order;
        float scale = targetWidth / sprite.bounds.size.x;
        obj.transform.localScale = new Vector3(scale, scale, 1f);
        obj.transform.rotation = Quaternion.identity;
        Bounds b = sprite.bounds;
        Vector3 anchor = b.center;
        if (topAnchor) anchor.y = b.max.y;
        else if (bottomAnchor) anchor.y = b.min.y;
        Vector3 origin = terrainGenerator.groundTilemap.CellToWorld(Vector3Int.zero);
        obj.transform.position = origin + new Vector3(x, y, 0f)
            - obj.transform.TransformVector(anchor);
    }

    [ContextMenu("Clear Bases")]
    public void ClearBases()
    {
        if (basePlatforms == null) return;
        for (int i = basePlatforms.childCount - 1; i >= 0; i--)
        {
            Transform child = basePlatforms.GetChild(i);
            if (child.name != RootName) continue;
            if (Application.isPlaying)
            {
                child.gameObject.SetActive(false);
                child.SetParent(null);
                Destroy(child.gameObject);
            }
            else DestroyImmediate(child.gameObject);
        }
        MarkDirty();
    }

    private bool ValidateSettings()
    {
        if (terrainGenerator == null || terrainGenerator.groundTilemap == null || basePlatforms == null)
        {
            Debug.LogError("[ARAM] Assign Terrain Generator and Base Platforms.", this);
            return false;
        }
        if (terrainGenerator.baseLength <= 0 || terrainGenerator.baseWidth <= 0 ||
            terrainGenerator.laneLength <= 0 || terrainGenerator.laneWidth <= 0)
        {
            Debug.LogError("[ARAM] Invalid terrain dimensions.", this);
            return false;
        }
        if (circularPlatform == null || spawnPlatform == null || ancientGate == null || centerEmblem == null)
        {
            Debug.LogError("[ARAM] Assign Circular Platform, Spawn Platform, Ancient Gate and Center Emblem.", this);
            return false;
        }
        if (circularPlatform.bounds.size.x <= 0f || circularPlatform.bounds.size.y <= 0f ||
            spawnPlatform.bounds.size.x <= 0f || spawnPlatform.bounds.size.y <= 0f ||
            platformWidth <= 0f || spawnWidth <= 0f || towerPlatformWidth <= 0f)
        {
            Debug.LogError("[ARAM] Invalid platform dimensions.", this);
            return false;
        }
        return true;
    }

    private void MarkDirty()
    {
#if UNITY_EDITOR
        if (!Application.isPlaying)
            UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(gameObject.scene);
#endif
    }
}

#if UNITY_EDITOR
[UnityEditor.CustomEditor(typeof(ARAMBaseGenerator))]
public class ARAMBaseGeneratorEditor : UnityEditor.Editor
{
    public override void OnInspectorGUI()
    {
        DrawDefaultInspector();
        ARAMBaseGenerator generator = (ARAMBaseGenerator)target;
        GUILayout.Space(12);
        Color old = GUI.backgroundColor;
        GUI.backgroundColor = new Color(0.3f, 0.8f, 0.4f);
        if (GUILayout.Button("Generate Bases", GUILayout.Height(40))) generator.GenerateBases();
        GUI.backgroundColor = new Color(0.9f, 0.4f, 0.4f);
        if (GUILayout.Button("Clear Bases", GUILayout.Height(30))) generator.ClearBases();
        GUI.backgroundColor = old;
    }
}
#endif
