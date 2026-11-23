using UnityEngine;

public enum MinimapEntityType { Player, Minion, Tower }

public class MinimapEntity : MonoBehaviour
{
    public MinimapEntityType entityType;

    [Header("Chỉnh tay nếu là nhân vật chính bạn đang điều khiển")]
    public bool isLocalPlayerOverride = false; // Biến này giúp SpawnArea nhận diện

    [HideInInspector] public TeamMember teamMember;
    [HideInInspector] public bool isLocalPlayer => isLocalPlayerOverride || (entityType == MinimapEntityType.Player && CompareTag("Player"));

    private void Awake()
    {
        teamMember = GetComponent<TeamMember>();
    }

    private void Start()
    {
        RegisterToManager();
    }

    private void OnEnable() => RegisterToManager();
    private void OnDisable() => UnregisterFromManager();
    private void OnDestroy() => UnregisterFromManager();

    private void RegisterToManager()
    {
        if (MinimapManager.Instance != null)
        {
            MinimapManager.Instance.RegisterEntity(this);
        }
    }

    private void UnregisterFromManager()
    {
        if (MinimapManager.Instance != null)
        {
            MinimapManager.Instance.UnregisterEntity(this);
        }
    }
}