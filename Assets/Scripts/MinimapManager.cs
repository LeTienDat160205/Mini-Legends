using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class MinimapManager : MonoBehaviour
{
    public static MinimapManager Instance;

    [Header("UI References")]
    public RectTransform minimapRect;
    public RectTransform iconsContainer;
    public GameObject iconPrefab;

    [Header("Bounds Source")]
    [SerializeField] private BoxCollider2D arenaBounds;

    [Header("Champion marker recovery")]
    [SerializeField] private bool autoAddMissingChampionEntities = true;
    [SerializeField, Min(0.1f)] private float rescanInterval = 0.5f;
    [SerializeField] private bool logChampionMarkerCount = false;

    private readonly Dictionary<MinimapEntity, RectTransform> activeIcons =
        new Dictionary<MinimapEntity, RectTransform>();
    private Vector2 worldMinBounds;
    private Vector2 worldMaxBounds;
    private float nextScanTime;
    private int lastReportedChampionCount = -1;

    public static Team LocalPlayerTeam = Team.Blue;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Debug.LogWarning("More than one MinimapManager found. Disabling duplicate.", this);
            enabled = false;
            return;
        }

        Instance = this;
        SetupBounds();
    }

    private void Start() => ReconcileChampionMarkers();

    private void OnDestroy()
    {
        if (Instance == this) Instance = null;
    }

    private void SetupBounds()
    {
        if (arenaBounds != null)
        {
            Bounds b = arenaBounds.bounds;
            worldMinBounds = new Vector2(b.min.x, b.min.y);
            worldMaxBounds = new Vector2(b.max.x, b.max.y);
        }
        else
        {
            Debug.LogWarning("MinimapManager: Arena Bounds (BoxCollider2D) is not assigned.", this);
        }
    }

    public void RegisterEntity(MinimapEntity entity)
    {
        if (entity == null || !entity.isActiveAndEnabled || activeIcons.ContainsKey(entity)) return;
        if (iconPrefab == null || iconsContainer == null || minimapRect == null)
        {
            Debug.LogError("MinimapManager: assign Minimap Rect, Icons Container and Icon Prefab.", this);
            return;
        }

        GameObject icon = Instantiate(iconPrefab, iconsContainer);
        RectTransform rect = icon.GetComponent<RectTransform>();
        Image image = icon.GetComponent<Image>();
        if (rect == null || image == null)
        {
            Debug.LogError("Minimap icon prefab needs RectTransform and Image components.", icon);
            Destroy(icon);
            return;
        }

        switch (entity.entityType)
        {
            case MinimapEntityType.Player:
                rect.sizeDelta = new Vector2(24f, 24f);
                icon.transform.SetAsLastSibling();
                break;
            case MinimapEntityType.Tower:
                rect.sizeDelta = new Vector2(40f, 40f);
                break;
            case MinimapEntityType.Minion:
                rect.sizeDelta = new Vector2(14f, 14f);
                break;
        }

        // Ensure icons aren't nested under prefab visual effects that might hide them.
        image.enabled = true;
        UpdateEntityColor(entity, image);
        activeIcons.Add(entity, rect);
    }

    public void UnregisterEntity(MinimapEntity entity)
    {
        if (ReferenceEquals(entity, null)) return;
        if (activeIcons.TryGetValue(entity, out RectTransform iconRect))
        {
            if (iconRect != null) Destroy(iconRect.gameObject);
            activeIcons.Remove(entity);
        }
    }

    private void LateUpdate()
    {
        if (Time.unscaledTime >= nextScanTime)
        {
            nextScanTime = Time.unscaledTime + Mathf.Max(0.1f, rescanInterval);
            ReconcileChampionMarkers();
        }

        // Prefer human player first when determining the local team.
        foreach (var item in activeIcons)
        {
            MinimapEntity entity = item.Key;
            if (entity != null && entity.isLocalPlayer && entity.teamMember != null)
            {
                LocalPlayerTeam = entity.teamMember.team;
                break;
            }
        }

        foreach (var item in activeIcons)
        {
            MinimapEntity entity = item.Key;
            RectTransform iconRect = item.Value;
            if (entity == null || iconRect == null) continue;

            iconRect.anchoredPosition = WorldToMinimapPosition(entity.transform.position);
            Image iconImage = iconRect.GetComponent<Image>();
            if (iconImage != null) UpdateEntityColor(entity, iconImage);
        }
    }

    private void ReconcileChampionMarkers()
    {
        // Every actual champion instance must have its own MinimapEntity.
        TeamMember[] members = FindObjectsByType<TeamMember>(FindObjectsSortMode.None);
        int championCount = 0;
        int registeredCount = 0;

        foreach (TeamMember member in members)
        {
            if (member == null || !member.gameObject.activeInHierarchy) continue;
            GameObject champion = member.gameObject;
            bool isChampion = champion.GetComponent<FootSoldier>() != null ||
                              champion.GetComponent<GoblinScout>() != null ||
                              champion.GetComponent<BladeAssasin>() != null;
            if (!isChampion) continue;
            championCount++;

            MinimapEntity marker = champion.GetComponent<MinimapEntity>();
            if (marker == null && autoAddMissingChampionEntities)
            {
                marker = champion.AddComponent<MinimapEntity>();
                marker.entityType = MinimapEntityType.Player;
                Debug.LogWarning("Added missing MinimapEntity to champion: " + champion.name, champion);
            }

            if (marker != null && marker.isActiveAndEnabled)
            {
                marker.entityType = MinimapEntityType.Player;
                marker.teamMember = member;
                RegisterEntity(marker);
                if (activeIcons.ContainsKey(marker)) registeredCount++;
            }
            else if (marker != null)
            {
                Debug.LogWarning("MinimapEntity disabled on champion: " + champion.name, champion);
            }
        }

        if (logChampionMarkerCount && lastReportedChampionCount != registeredCount)
        {
            Debug.Log($"Minimap champions: {registeredCount}/{championCount} registered", this);
            lastReportedChampionCount = registeredCount;
        }
    }

    private void UpdateEntityColor(MinimapEntity entity, Image iconImage)
    {
        if (entity.isLocalPlayer)
        {
            iconImage.color = new Color(0.2f, 1f, 0.2f);
            return;
        }

        Team entityTeam = Team.Neutral;
        if (entity.teamMember != null) entityTeam = entity.teamMember.team;
        else if (entity.name.Contains("Blue") || entity.transform.root.name.Contains("Blue")) entityTeam = Team.Blue;
        else if (entity.name.Contains("Red") || entity.transform.root.name.Contains("Red")) entityTeam = Team.Red;

        iconImage.color = entityTeam == LocalPlayerTeam && entityTeam != Team.Neutral
            ? new Color(0.2f, 0.6f, 1f)
            : new Color(1f, 0.3f, 0.3f);
    }

    private Vector2 WorldToMinimapPosition(Vector3 position)
    {
        float nx = Mathf.InverseLerp(worldMinBounds.x, worldMaxBounds.x, position.x);
        float ny = Mathf.InverseLerp(worldMinBounds.y, worldMaxBounds.y, position.y);
        return new Vector2((nx - 0.5f) * minimapRect.rect.width,
                           (ny - 0.5f) * minimapRect.rect.height);
    }
}
