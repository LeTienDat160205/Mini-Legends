using UnityEngine;

/// <summary>
/// Positions champion SpawnAreas on the ancient platforms and minion spawn
/// Transforms outside each Nexus. No character/minion is instantiated here.
/// Runs before SpawnArea.Start() and MinionSpawn.Start().
/// </summary>
[DefaultExecutionOrder(-200)]
public class ARAMSpawnLayout : MonoBehaviour
{
    [Header("Champion spawn positions (platform centers)")]
    [SerializeField] private SpawnArea blueSpawnArea;
    [SerializeField] private SpawnArea redSpawnArea;
    [SerializeField] private Transform blueAncientSpawnPlatform;
    [SerializeField] private Transform redAncientSpawnPlatform;
    [SerializeField] private Vector2 blueChampionOffset;
    [SerializeField] private Vector2 redChampionOffset;

    [Header("Minion spawn points - user-adjustable")]
    [SerializeField] private Transform blueNexus;
    [SerializeField] private Transform redNexus;
    [SerializeField] private Transform blueMinionSpawnPoint;
    [SerializeField] private Transform redMinionSpawnPoint;
    [Tooltip("Moves minions toward the bridge, measured from Nexus center/edge. Blue is generally +X.")]
    [SerializeField, Min(0f)] private float blueLaneOffset = 2.5f;
    [Tooltip("Moves minions toward the bridge. Red is generally -X.")]
    [SerializeField, Min(0f)] private float redLaneOffset = 2.5f;
    [Tooltip("Minimum gap above the top of Nexus visuals/colliders. May need less if map top wall is nearby.")]
    [SerializeField, Min(0f)] private float minionAboveNexus = 0.75f;
    [SerializeField] private Vector2 blueMinionExtraOffset;
    [SerializeField] private Vector2 redMinionExtraOffset;

    [Header("Options")]
    [Tooltip("OFF = leave minion spawn points exactly where you place them in Scene. Recommended after manual positioning.")]
    [SerializeField] private bool autoPositionMinionSpawns = false;
    [SerializeField] private bool applyOnAwake = true;

    private void Awake()
    {
        if (applyOnAwake) ApplyLayout();
    }

    [ContextMenu("Apply Spawn Layout")]
    public void ApplyLayout()
    {
        MoveToPlatform(blueSpawnArea, blueAncientSpawnPlatform, blueChampionOffset, "Blue champion");
        MoveToPlatform(redSpawnArea, redAncientSpawnPlatform, redChampionOffset, "Red champion");

        if (autoPositionMinionSpawns)
        {
            PlaceMinionSpawn(blueMinionSpawnPoint, blueNexus, +1f, blueLaneOffset, blueMinionExtraOffset, "Blue minions");
            PlaceMinionSpawn(redMinionSpawnPoint, redNexus, -1f, redLaneOffset, redMinionExtraOffset, "Red minions");
        }
    }

    private void MoveToPlatform(SpawnArea spawn, Transform platform, Vector2 offset, string description)
    {
        if (spawn == null || platform == null)
        {
            Debug.LogWarning($"[SpawnLayout] Missing {description} reference.", this);
            return;
        }
        Vector3 p = spawn.transform.position;
        p.x = platform.position.x + offset.x;
        p.y = platform.position.y + offset.y;
        spawn.transform.position = p;
    }

    private void PlaceMinionSpawn(Transform spawn, Transform nexus, float laneSign, float laneOffset,
        Vector2 extra, string description)
    {
        if (spawn == null || nexus == null)
        {
            Debug.LogWarning($"[SpawnLayout] Missing {description} reference.", this);
            return;
        }
        // Avoid center + 2 units which can still be inside a large Nexus.
        float top = nexus.position.y;
        Collider2D[] colliders = nexus.GetComponentsInChildren<Collider2D>();
        for (int i = 0; i < colliders.Length; i++)
        {
            if (colliders[i] != null && colliders[i].enabled && !colliders[i].isTrigger)
                top = Mathf.Max(top, colliders[i].bounds.max.y);
        }
        SpriteRenderer[] sprites = nexus.GetComponentsInChildren<SpriteRenderer>();
        for (int i = 0; i < sprites.Length; i++)
        {
            if (sprites[i] != null && sprites[i].enabled)
                top = Mathf.Max(top, sprites[i].bounds.max.y);
        }
        Vector3 p = spawn.position;
        p.x = nexus.position.x + laneSign * laneOffset + extra.x;
        p.y = top + minionAboveNexus + extra.y;
        spawn.position = p;
    }
}
