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
        if (activeIcons.ContainsKey(entity)) return;

        GameObject newIcon = Instantiate(iconPrefab, iconsContainer);
        RectTransform iconRect = newIcon.GetComponent<RectTransform>();
        Image iconImage = newIcon.GetComponent<Image>();

        // Thiết lập màu ban đầu
        UpdateEntityColor(entity, iconImage);

        // KÍCH THƯỚC PIXEL CHO TỪNG LOẠI
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
        if (activeIcons.TryGetValue(entity, out RectTransform iconRect))
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
                // Cập nhật vị trí tọa độ liên tục theo thời gian thực
                iconRect.anchoredPosition = WorldToMinimapPosition(entity.transform.position);

                // Cập nhật màu liên tục để khắc phục triệt để lỗi thứ tự Instantiate
                Image iconImage = iconRect.GetComponent<Image>();
                if (iconImage != null)
                {
                    UpdateEntityColor(entity, iconImage);
                }
            }
        }
    }

    private void UpdateEntityColor(MinimapEntity entity, Image iconImage)
    {
        if (entity.isLocalPlayer)
        {
            if (entity.teamMember != null) LocalPlayerTeam = entity.teamMember.team;
            iconImage.color = new Color(0.2f, 1f, 0.2f); // Xanh lục (Player chính)
        }
        else
        {
            Team entityTeam = Team.Neutral;
            if (entity.teamMember != null)
            {
                entityTeam = entity.teamMember.team;
            }
            else if (entity.name.Contains("Blue") || entity.transform.root.name.Contains("Blue"))
            {
                entityTeam = Team.Blue;
            }
            else if (entity.name.Contains("Red") || entity.transform.root.name.Contains("Red"))
            {
                entityTeam = Team.Red;
            }

            if (entityTeam == LocalPlayerTeam && entityTeam != Team.Neutral)
            {
                iconImage.color = new Color(0.2f, 0.6f, 1f);  // Xanh dương (Đồng minh, Lính nhà)
            }
            else
            {
                iconImage.color = new Color(1f, 0.3f, 0.3f); // Đỏ (Địch, Lính địch)
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