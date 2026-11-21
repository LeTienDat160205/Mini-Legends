using UnityEngine;

public class PlayerSpawner : MonoBehaviour
{
    [SerializeField] private Transform spawnPoint;
    [SerializeField] private GameObject fallbackPrefab;   // dùng khi chạy thử scene này riêng

    private void Start()
    {
        ChampionData picked = ChampSelectResult.PickedChampion;
        GameObject prefab = picked != null ? picked.playerPrefab : fallbackPrefab;
        if (prefab == null) return;

        GameObject player = Instantiate(prefab, spawnPoint != null ? spawnPoint.position : transform.position, Quaternion.identity);
        TeamMember member = player.GetComponent<TeamMember>();
        SpawnArea spawn = member != null ? SpawnArea.FindFor(member.team) : null;
        if (spawn != null) spawn.PlaceFeet(player.transform);
        else if (spawnPoint != null) player.transform.position = spawnPoint.position;
    }
}
