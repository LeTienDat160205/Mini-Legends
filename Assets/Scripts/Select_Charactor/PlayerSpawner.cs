using UnityEngine;

public class PlayerSpawner : MonoBehaviour
{
    [SerializeField] private Transform spawnPoint;
    [SerializeField] private GameObject fallbackPrefab;   // dùng khi chạy thử scene này riêng

    private void Start()
    {
        ChampionData picked = ChampSelectResult.PickedChampion;
        GameObject prefab = picked != null ? picked.playerPrefab : fallbackPrefab;
        if (prefab != null)
            Instantiate(prefab, spawnPoint.position, Quaternion.identity);
    }
}
