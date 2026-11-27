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

    private Dictionary<MinimapEntity, RectTransform> activeIcons = new Dictionary<MinimapEntity, RectTransform>();
    private Vector2 worldMinBounds;
    private Vector2 worldMaxBounds;

    // --- BIẾN TOÀN CỤC LƯU PHE CỦA PLAYER CHÍNH ---
    public static Team LocalPlayerTeam = Team.Blue;

    private void Awake()
    {
        Instance = this;
        SetupBounds();
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

        switch (entity.entityType)
        {
            case MinimapEntityType.Player:
                iconRect.sizeDelta = new Vector2(24f, 24f);
                newIcon.transform.SetAsLastSibling();
                break;
            case MinimapEntityType.Tower:
                iconRect.sizeDelta = new Vector2(40f, 40f);
                break;
            case MinimapEntityType.Minion:
                iconRect.sizeDelta = new Vector2(14f, 14f);
                break;
        }

        activeIcons.Add(entity, iconRect);
    }

    public void UnregisterEntity(MinimapEntity entity)
    {
        if (!ReferenceEquals(entity, null) && activeIcons.TryGetValue(entity, out RectTransform iconRect))
        {
            if (iconRect != null) Destroy(iconRect.gameObject);
            activeIcons.Remove(entity);
        }
    }

    private void LateUpdate()
    {
        foreach (var pair in activeIcons)
        {
            MinimapEntity entity = pair.Key;
            RectTransform iconRect = pair.Value;

            if (entity != null && iconRect != null)
            {
                LocalPlayerTeam = entity.teamMember.team;
            }
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
        if (entity.entityType == MinimapEntityType.Tower)
        {
            Nexus nexus = entity.GetComponent<Nexus>() ?? entity.GetComponentInParent<Nexus>();
            if (nexus != null) entityTeam = nexus.NexusTeam;
        }
        else if (entity.entityType == MinimapEntityType.Minion)
        {
            Minion minion = entity.GetComponent<Minion>() ?? entity.GetComponentInParent<Minion>();
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
