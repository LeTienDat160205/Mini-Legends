using System.Collections;
using UnityEngine;

/// <summary>
/// Spawns configurable waves of minions, safely away from solid structures.
/// Each team's MinionSpawn GameObject itself can serve as spawn point.
/// Based on ARAM_3v3_SpawnFix; does not change Minion.cs.
/// </summary>
public class MinionSpawn : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private GameObject[] minionPrefabs;
    [Tooltip("Leave empty to spawn at this MinionSpawn GameObject position.")]
    [SerializeField] private Transform spawnPoint;
    [SerializeField] private Team team = Team.Blue;

    [Header("Wave Balance")]
    [SerializeField, Min(1)] private int minionsPerWave = 3;
    [SerializeField, Min(1f)] private float waveInterval = 20f;
    [SerializeField, Min(0f)] private float firstWaveDelay = 10f;
    [SerializeField, Min(0f)] private float perMinionDelay = 0.2f;

    [Header("Formation & Safe Spawn")]
    [SerializeField, Min(0.1f)] private float spacing = 1.5f;
    [Tooltip("Pause before starting a second group of three, if wave size > 3.")]
    [SerializeField, Min(0f)] private float groupDelay = 2f;
    [SerializeField, Min(0f)] private float extraClearance = 0.15f;
    [SerializeField, Min(0f)] private float maxSpawnWait = 3f;
    [SerializeField, Min(1)] private int searchRings = 8;

    private IEnumerator Start()
    {
        if (firstWaveDelay > 0f)
            yield return new WaitForSeconds(firstWaveDelay);

        while (true)
        {
            float startTime = Time.time;
            yield return SpawnWave();
            float elapsed = Time.time - startTime;
            yield return new WaitForSeconds(Mathf.Max(0.1f, waveInterval - elapsed));
        }
    }

    private IEnumerator SpawnWave()
    {
        if (minionPrefabs == null || minionPrefabs.Length == 0)
        {
            Debug.LogWarning("[MinionSpawn] No minion prefabs assigned.", this);
            yield break;
        }

        Transform origin = spawnPoint != null ? spawnPoint : transform;
        Vector3 forward = team == Team.Red ? Vector3.left : Vector3.right;
        Vector3 lateral = Vector3.up;

        for (int i = 0; i < minionsPerWave; i++)
        {
            if (i > 0)
            {
                if (i % 3 == 0 && groupDelay > 0f)
                    yield return new WaitForSeconds(groupDelay);
                else if (perMinionDelay > 0f)
                    yield return new WaitForSeconds(perMinionDelay);
            }

            GameObject prefab = minionPrefabs[i % minionPrefabs.Length];
            if (prefab == null) continue;

            int row = i / 3;
            int column = i % 3;
            // Fill a row vertically; additional rows advance toward mid-lane.
            // For 1 or 2 minions, keep the row centered.
            int thisRowCount = Mathf.Min(3, minionsPerWave - row * 3);
            float centeredColumn = column - (thisRowCount - 1) * 0.5f;
            Vector3 desired = origin.position + forward * (row * spacing) + lateral * (centeredColumn * spacing);

            Collider2D prefabCollider = prefab.GetComponent<Collider2D>();
            Vector3 safePosition;
            bool found = FindFreePosition(desired, forward, prefabCollider, out safePosition);
            float deadline = Time.time + maxSpawnWait;
            while (!found && Time.time < deadline)
            {
                yield return new WaitForSeconds(0.25f);
                found = FindFreePosition(desired, forward, prefabCollider, out safePosition);
            }

            if (!found)
            {
                Debug.LogWarning($"[MinionSpawn] Skipped blocked slot {i}. Move MinionSpawn away from Nexus/walls.", this);
                continue;
            }

            GameObject minion = Instantiate(prefab, safePosition, Quaternion.identity);
            TeamMember member = minion.GetComponent<TeamMember>();
            if (member != null) member.team = team;
            Minion behaviour = minion.GetComponent<Minion>();
            if (behaviour == null) behaviour = minion.AddComponent<Minion>();
            behaviour.Initialize(team);
        }
    }

    private bool FindFreePosition(Vector3 desired, Vector3 forward, Collider2D prefabCollider, out Vector3 result)
    {
        Physics2D.SyncTransforms();
        for (int ring = 0; ring <= searchRings; ring++)
        {
            int samples = ring == 0 ? 1 : 12;
            for (int sample = 0; sample < samples; sample++)
            {
                float angle = sample * Mathf.PI * 2f / samples;
                Vector3 candidate = desired +
                    (forward * (0.5f + Mathf.Abs(Mathf.Cos(angle))) +
                     Vector3.up * Mathf.Sin(angle)) * (ring * 0.5f);
                if (IsOccupied(candidate, prefabCollider)) continue;
                result = candidate;
                return true;
            }
        }
        result = desired;
        return false;
    }

    private bool IsOccupied(Vector3 position, Collider2D prefabCollider)
    {
        Collider2D[] hits;
        Vector2 offset = prefabCollider != null ? prefabCollider.offset : Vector2.zero;
        Vector2 center = (Vector2)position + offset;
        Vector2 scale = prefabCollider != null ? prefabCollider.transform.lossyScale : Vector2.one;
        float sx = Mathf.Abs(scale.x);
        float sy = Mathf.Abs(scale.y);

        if (prefabCollider is CapsuleCollider2D capsule)
            hits = Physics2D.OverlapCapsuleAll(center,
                Vector2.Scale(capsule.size, new Vector2(sx, sy)) + Vector2.one * extraClearance,
                capsule.direction, 0f);
        else if (prefabCollider is BoxCollider2D box)
            hits = Physics2D.OverlapBoxAll(center,
                Vector2.Scale(box.size, new Vector2(sx, sy)) + Vector2.one * extraClearance, 0f);
        else if (prefabCollider is CircleCollider2D circle)
            hits = Physics2D.OverlapCircleAll(center,
                circle.radius * Mathf.Max(sx, sy) + extraClearance);
        else
            hits = Physics2D.OverlapCircleAll(center, 0.5f + extraClearance);

        foreach (Collider2D hit in hits)
            if (hit != null && hit.enabled && !hit.isTrigger)
                return true;
        return false;
    }
}
