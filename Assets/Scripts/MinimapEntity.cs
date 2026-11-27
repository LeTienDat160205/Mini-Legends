using UnityEngine;

public enum MinimapEntityType { Player, Minion, Tower }

public class MinimapEntity : MonoBehaviour
{
    public MinimapEntityType entityType = MinimapEntityType.Player;

    [Header("Only true on the human-controlled champion")]
    public bool isLocalPlayerOverride = false;

    [HideInInspector] public TeamMember teamMember;

    // Do NOT use CompareTag("Player"): AI prefabs can inherit the same tag.
    public bool isLocalPlayer
    {
        get
        {
            if (isLocalPlayerOverride) return true;
            if (entityType != MinimapEntityType.Player) return false;
            PlayerMovement movement = GetComponent<PlayerMovement>();
            EnemyMovement ai = GetComponent<EnemyMovement>();
            return movement != null && movement.enabled && (ai == null || !ai.enabled);
        }
    }

    private void Awake()
    {
        teamMember = GetComponent<TeamMember>();
    }

    private void Start() => RegisterToManager();
    private void OnEnable() => RegisterToManager();
    private void OnDisable() => UnregisterFromManager();
    private void OnDestroy() => UnregisterFromManager();

    private void RegisterToManager()
    {
        if (teamMember == null) teamMember = GetComponent<TeamMember>();
        if (MinimapManager.Instance != null)
            MinimapManager.Instance.RegisterEntity(this);
    }

    private void UnregisterFromManager()
    {
        if (MinimapManager.Instance != null)
            MinimapManager.Instance.UnregisterEntity(this);
    }
}
