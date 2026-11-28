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

    [Header("Cong trinh (Tru / Nexus)")]
    [Tooltip("Ve icon tru va nexus tren cung mot duong ngang (lay Y trung binh cua chung), tranh bi lech do pivot khac nhau")]
    [SerializeField] private bool alignStructuresToLane = true;
    [Tooltip("Dich icon cong trinh len/xuong (don vi the gioi). Dung de chinh tay neu can")]
    [SerializeField] private float structureYOffset = 0f;

    private Dictionary<MinimapEntity, RectTransform> activeIcons = new Dictionary<MinimapEntity, RectTransform>();
    private Vector2 worldMinBounds;
    private Vector2 worldMaxBounds;

    private bool laneYDirty = true;
    private float structureLaneY;

    // --- BIẾN TOÀN CỤC LƯU PHE CỦA PLAYER CHÍNH ---
    public static Team LocalPlayerTeam = Team.Blue;

    private void Awake()
    {
        Instance = this;
        SetupBounds();
    }

    private void Start()
    {
        // Entities enabled before the manager exists could not register in OnEnable.
        foreach (MinimapEntity entity in FindObjectsByType<MinimapEntity>(FindObjectsSortMode.None))
            RegisterEntity(entity);
    }

    private void SetupBounds()
    {
        if (arenaBounds != null)
        {
            Bounds b = arenaBounds.bounds;
            worldMinBounds = new Vector2(b.min.x, b.min.y);
            worldMaxBounds = new Vector2(b.max.x, b.max.y);
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

        GameObject newIcon = Instantiate(iconPrefab, iconsContainer);
        RectTransform iconRect = newIcon.GetComponent<RectTransform>();
        Image iconImage = newIcon.GetComponent<Image>();
        if (iconRect == null || iconImage == null)
        {
            Debug.LogError("Minimap icon prefab needs RectTransform and Image components.", newIcon);
            Destroy(newIcon);
            return;
        }

        switch (entity.entityType)
        {
            case MinimapEntityType.Player:
                iconRect.sizeDelta = new Vector2(20f, 20f);
                newIcon.transform.SetAsLastSibling();
                break;
            case MinimapEntityType.Tower:
                iconRect.sizeDelta = new Vector2(30f, 30f);
                break;
            case MinimapEntityType.Minion:
                iconRect.sizeDelta = new Vector2(14f, 14f);
                break;
        }

        if (entity.entityType == MinimapEntityType.Tower)
        {
            laneYDirty = true;
            WarnIfNexusTeamMismatch(entity);
        }

        iconImage.enabled = true;
        UpdateEntityColor(entity, iconImage);
        iconRect.anchoredPosition = WorldToMinimapPosition(GetIconWorldPosition(entity));
        activeIcons.Add(entity, iconRect);
    }

    public void UnregisterEntity(MinimapEntity entity)
    {
        if (!ReferenceEquals(entity, null) && activeIcons.TryGetValue(entity, out RectTransform iconRect))
        {
            if (iconRect != null) Destroy(iconRect.gameObject);
            activeIcons.Remove(entity);
            laneYDirty = true;
        }
    }

    private void LateUpdate()
    {
        // Only the human player defines the local team; minions have no TeamMember.
        foreach (var pair in activeIcons)
        {
            MinimapEntity entity = pair.Key;
            if (entity != null && entity.isLocalPlayer && entity.teamMember != null)
            {
                LocalPlayerTeam = entity.teamMember.team;
                break;
            }
        }

        foreach (var pair in activeIcons)
        {
            MinimapEntity entity = pair.Key;
            RectTransform iconRect = pair.Value;
            if (entity == null || iconRect == null) continue;
            iconRect.anchoredPosition = WorldToMinimapPosition(GetIconWorldPosition(entity));
            Image iconImage = iconRect.GetComponent<Image>();
            if (iconImage != null) UpdateEntityColor(entity, iconImage);
        }
    }

    // Vi tri the gioi dung de ve icon. Tru / Nexus duoc dua ve cung mot duong ngang.
    private Vector3 GetIconWorldPosition(MinimapEntity entity)
    {
        Vector3 position = entity.transform.position;

        if (alignStructuresToLane && entity.entityType == MinimapEntityType.Tower)
        {
            if (laneYDirty) RecomputeStructureLaneY();
            position.y = structureLaneY + structureYOffset;
        }

        return position;
    }

    private void RecomputeStructureLaneY()
    {
        float sum = 0f;
        int count = 0;

        foreach (var pair in activeIcons)
        {
            MinimapEntity entity = pair.Key;
            if (entity == null || entity.entityType != MinimapEntityType.Tower) continue;
            sum += entity.transform.position.y;
            count++;
        }

        if (count > 0) structureLaneY = sum / count;
        laneYDirty = false;
    }

    // Nexus co ten Red nhung truong Nexus Team lai de Blue (hoac nguoc lai) -> mau tren minimap,
    // thanh mau va man hinh Victory/Defeat deu sai. Bao ra Console de sua trong Inspector.
    private void WarnIfNexusTeamMismatch(MinimapEntity entity)
    {
        Nexus nexus = entity.GetComponent<Nexus>();
        if (nexus == null) nexus = entity.GetComponentInParent<Nexus>();
        if (nexus == null) return;

        string lowerName = nexus.name.ToLower();
        bool nameSaysRed = lowerName.Contains("red");
        bool nameSaysBlue = lowerName.Contains("blue");

        if ((nameSaysRed && nexus.NexusTeam != Team.Red) ||
            (nameSaysBlue && nexus.NexusTeam != Team.Blue))
        {
            Debug.LogWarning(
                $"[Minimap] {nexus.name} dang co Nexus Team = {nexus.NexusTeam}, khong khop voi ten object. " +
                "Hay chinh lai o component Nexus trong Inspector.", nexus);
        }
    }

    private void UpdateEntityColor(MinimapEntity entity, Image iconImage)
    {
        // 1. Nếu là nhân vật người chơi chính đang điều khiển -> Xanh lục
        if (entity.isLocalPlayer)
        {
            if (entity.teamMember != null)
            {
                LocalPlayerTeam = entity.teamMember.team;
            }
            iconImage.color = new Color(0.2f, 1f, 0.2f);
            return;
        }

        Team entityTeam = Team.Neutral;

        // 2. Lấy phe từ Minion hoặc Nexus
        // (Khong dung toan tu "??" voi GetComponent: trong Editor no co the khong coi "khong tim thay" la null.)
        if (entity.entityType == MinimapEntityType.Tower)
        {
            Nexus nexus = entity.GetComponent<Nexus>();
            if (nexus == null) nexus = entity.GetComponentInParent<Nexus>();
            if (nexus != null) entityTeam = nexus.NexusTeam;
        }
        else if (entity.entityType == MinimapEntityType.Minion)
        {
            Minion minion = entity.GetComponent<Minion>();
            if (minion == null) minion = entity.GetComponentInParent<Minion>();
            if (minion != null)
            {
                entityTeam = minion.Team;
                // Debug để xem lính đang có Team gì trên Console Unity
                // Debug.Log("Minion name: " + entity.name + " | Team: " + minion.Team);
            }
        }
        else
        {
            if (entity.teamMember != null) entityTeam = entity.teamMember.team;
        }

        // 3. Xử lý hiển thị màu dứt khoát
        // Nếu lính/trụ thuộc phe Blue hoặc tên chứa chữ Blue -> Tô màu xanh dương
        if (entityTeam == Team.Blue || entity.name.Contains("Blue") || entity.transform.root.name.Contains("Blue"))
        {
            iconImage.color = new Color(0.2f, 0.6f, 1f); // Xanh dương
        }
        else if (entityTeam == Team.Red || entity.name.Contains("Red") || entity.transform.root.name.Contains("Red"))
        {
            iconImage.color = new Color(1f, 0.3f, 0.3f); // Đỏ
        }
        else
        {
            // So sánh chuẩn với LocalPlayerTeam nếu các điều kiện trên không khớp
            if (entityTeam == LocalPlayerTeam && entityTeam != Team.Neutral)
            {
                iconImage.color = new Color(0.2f, 0.6f, 1f);
            }
            else
            {
                iconImage.color = new Color(1f, 0.3f, 0.3f);
            }
        }
    }

    private Vector2 WorldToMinimapPosition(Vector3 worldPos)
    {
        float normalizedX = Mathf.InverseLerp(worldMinBounds.x, worldMaxBounds.x, worldPos.x);
        float normalizedY = Mathf.InverseLerp(worldMinBounds.y, worldMaxBounds.y, worldPos.y);

        float uiX = (normalizedX - 0.5f) * minimapRect.rect.width;
        float uiY = (normalizedY - 0.5f) * minimapRect.rect.height;

        return new Vector2(uiX, uiY);
    }
}