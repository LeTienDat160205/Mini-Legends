using UnityEngine;
using System.Collections;

public class MinionSpawn : MonoBehaviour
{
    [SerializeField] private GameObject[] minionPrefabs;
    [SerializeField] private Transform spawnPoint;
    [SerializeField] private Team team = Team.Blue;
    [SerializeField, Min(0f)] private float spacing = 2f;
    [SerializeField, Min(0.1f)] private float groupDelay = 2f;

    private const int MinionsPerWave = 6;
    private const float WaveInterval = 10f;

    private IEnumerator Start()
    {
        yield return new WaitForSeconds(WaveInterval);
        while (true)
        {
            yield return SpawnWave();
            yield return new WaitForSeconds(Mathf.Max(0f, WaveInterval - groupDelay));
        }
    }

    private IEnumerator SpawnWave()
    {
        if (minionPrefabs == null || minionPrefabs.Length == 0)
        {
            Debug.LogWarning("Chưa gán prefab lính cho MInionSpawn.", this);
            yield break;
        }

        Transform origin = spawnPoint != null ? spawnPoint : transform;
        for (int i = 0; i < MinionsPerWave; i++)
        {
            if (i == 3) yield return new WaitForSeconds(groupDelay);
            GameObject prefab = minionPrefabs[i % minionPrefabs.Length];
            if (prefab == null) continue;
            Collider2D prefabCollider = prefab.GetComponent<Collider2D>();
            int row = i / 3;
            int column = i % 3;
            Vector3 position = origin.position +
                origin.right * ((column - 1f) * spacing) -
                origin.up * (row * spacing);
            Vector3 desiredPosition = position;
            if (!FindFreePosition(desiredPosition, origin, prefabCollider, out position))
            {
                Debug.LogWarning("Không còn chỗ trống để sinh lính; đang chờ vị trí an toàn.", this);
                do
                {
                    yield return new WaitForSeconds(0.25f);
                } while (!FindFreePosition(desiredPosition, origin, prefabCollider, out position));
            }
            GameObject minion = Instantiate(prefab, position, origin.rotation);
            TeamMember member = minion.GetComponent<TeamMember>();
            if (member != null) member.team = team;
            Minion behaviour = minion.GetComponent<Minion>();
            if (behaviour == null) behaviour = minion.AddComponent<Minion>();
            behaviour.Initialize(team);
        }
    }

    private bool FindFreePosition(Vector3 desired, Transform origin, Collider2D prefabCollider,
        out Vector3 result)
    {
        Physics2D.SyncTransforms();
        for (int ring = 0; ring <= 6; ring++)
        {
            int samples = ring == 0 ? 1 : 12;
            for (int sample = 0; sample < samples; sample++)
            {
                float angle = sample * Mathf.PI * 2f / samples;
                Vector3 candidate = desired + (origin.right * Mathf.Cos(angle) +
                    origin.up * Mathf.Sin(angle)) * (ring * 0.5f);
                Vector2 offset = prefabCollider != null ? prefabCollider.offset : Vector2.zero;
                Vector2 center = candidate + origin.rotation * offset;
                Collider2D[] hits;
                if (prefabCollider is CapsuleCollider2D capsule)
                    hits = Physics2D.OverlapCapsuleAll(center, capsule.size + Vector2.one * 0.1f,
                        capsule.direction, origin.eulerAngles.z);
                else if (prefabCollider is BoxCollider2D box)
                    hits = Physics2D.OverlapBoxAll(center, box.size + Vector2.one * 0.1f,
                        origin.eulerAngles.z);
                else if (prefabCollider is CircleCollider2D circle)
                    hits = Physics2D.OverlapCircleAll(center, circle.radius + 0.05f);
                else
                    hits = Physics2D.OverlapCircleAll(center, 0.5f);
                bool occupied = false;
                foreach (Collider2D hit in hits)
                {
                    if (!hit.enabled || hit.isTrigger) continue;
                    occupied = true;
                    break;
                }
                if (occupied) continue;
                result = candidate;
                return true;
            }
        }
        result = desired;
        return false;
    }
}
