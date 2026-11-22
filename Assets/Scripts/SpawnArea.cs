using UnityEngine;
using Unity.Cinemachine;

public class SpawnArea : MonoBehaviour
{
    [SerializeField] private GameObject fallbackPlayerPrefab;
    [SerializeField] private CinemachineCamera cmCamera;

    private void Start()
    {
        TeamMember spawnTeam = GetComponent<TeamMember>();
        if (spawnTeam == null) return;

        ChampionData picked = ChampSelectResult.PickedChampion;
        if (picked != null && spawnTeam.team != ChampSelectResult.PickedTeam) return;
        if (picked == null && spawnTeam.team != Team.Blue) return;

        GameObject prefab = picked != null ? picked.playerPrefab : fallbackPlayerPrefab;
        if (prefab == null)
        {
            Debug.LogError("SpawnArea chưa có prefab của tướng đã chọn hoặc prefab dự phòng.", this);
            return;
        }

        GameObject player = Instantiate(prefab, transform.position, Quaternion.identity);
        TeamMember member = player.GetComponent<TeamMember>();
        if (member != null) member.team = spawnTeam.team;
        if (player.GetComponent<PlayerMovement>() == null)
            player.AddComponent<PlayerMovement>();
        PlaceFeet(player.transform);
        if (cmCamera != null) cmCamera.Follow = player.transform;
    }

    public static SpawnArea FindFor(Team team)
    {
        foreach (SpawnArea spawn in FindObjectsByType<SpawnArea>(FindObjectsSortMode.None))
        {
            TeamMember member = spawn.GetComponent<TeamMember>();
            if (member != null && member.team == team) return spawn;
        }
        return null;
    }

    public void PlaceFeet(Transform character)
    {
        if (character == null) return;
        Rigidbody2D body = character.GetComponent<Rigidbody2D>();
        if (body != null) body.linearVelocity = Vector2.zero;

        SpriteRenderer sprite = character.GetComponent<SpriteRenderer>();
        Collider2D collider = character.GetComponent<Collider2D>();
        float feetY = collider != null ? collider.bounds.min.y :
            sprite != null ? sprite.bounds.min.y : character.position.y;
        Vector3 destination = character.position;
        destination.x += transform.position.x - character.position.x;
        destination.y += transform.position.y - feetY;
        character.position = destination;
        if (body != null) body.position = destination;
    }
}
