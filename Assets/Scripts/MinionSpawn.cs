using UnityEngine;
using System.Collections;

public class MinionSpawn : MonoBehaviour
{
    [SerializeField] private GameObject[] minionPrefabs;
    [SerializeField] private Transform spawnPoint;
    [SerializeField] private Team team = Team.Blue;
    [SerializeField, Min(0f)] private float spacing = 2f;

    private const int MinionsPerWave = 6;
    private const float WaveInterval = 10f;

    private IEnumerator Start()
    {
        yield return new WaitForSeconds(WaveInterval);
        while (true)
        {
            SpawnWave();
            yield return new WaitForSeconds(WaveInterval);
        }
    }

    private void SpawnWave()
    {
        if (minionPrefabs == null || minionPrefabs.Length == 0)
        {
            Debug.LogWarning("Chưa gán prefab lính cho MInionSpawn.", this);
            return;
        }

        Transform origin = spawnPoint != null ? spawnPoint : transform;
        for (int i = 0; i < MinionsPerWave; i++)
        {
            GameObject prefab = minionPrefabs[i % minionPrefabs.Length];
            if (prefab == null) continue;
            int row = i / 2;
            int column = i % 2;
            Vector3 position = origin.position +
                origin.right * ((column - 0.5f) * spacing) -
                origin.up * (row * spacing);
            GameObject minion = Instantiate(prefab, position, origin.rotation);
            TeamMember member = minion.GetComponent<TeamMember>();
            if (member != null) member.team = team;
            Minion behaviour = minion.GetComponent<Minion>();
            if (behaviour == null) behaviour = minion.AddComponent<Minion>();
            behaviour.Initialize(team);
        }
    }
}
