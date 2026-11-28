using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

// Hiển thị các trang bị đã mua / nâng cấp vào các ô Item1..Item4 của HUD Inventory.
// Gắn script này vào object "Inventory" (HUD/Inventory). Script chỉ HIỂN THỊ, không cộng chỉ số.
public class InventoryHUD : MonoBehaviour
{
    [SerializeField] private ShopManager shop;   // Để trống: tự tìm trong scene

    [Tooltip("Để trống: tự lấy các object con tên Item1, Item2, Item3, Item4")]
    [SerializeField] private RectTransform[] slots;

    [Header("Hiển thị")]
    [SerializeField] private Vector2 iconPadding = new Vector2(6f, 6f);

    private class SlotView
    {
        public Image icon;
    }

    // Báo cho script khác (ví dụ ShopManager) biết túi đồ vừa thay đổi
    public event System.Action Changed;

    private readonly List<SlotView> views = new List<SlotView>();
    private readonly List<ItemData> ownedOrder = new List<ItemData>();           // Thứ tự món theo ô
    private readonly Dictionary<ItemData, int> ownedTier = new Dictionary<ItemData, int>();   // Phẩm hiện có của từng món

    private void Awake()
    {
        FindSlotsIfMissing();
        foreach (RectTransform slot in slots)
            if (slot != null) views.Add(BuildSlotView(slot));
        Refresh();
    }

    private void Start()
    {
        if (shop == null) shop = FindFirstObjectByType<ShopManager>(FindObjectsInactive.Include);
        if (shop != null) shop.ItemPurchased += OnItemPurchased;
        else Debug.LogWarning("[InventoryHUD] Không tìm thấy ShopManager trong scene.", this);
    }

    private void OnDestroy()
    {
        if (shop != null) shop.ItemPurchased -= OnItemPurchased;
    }

    // Phẩm hiện có của một món (-1 nếu chưa mua)
    public int GetOwnedTier(ItemData item)
    {
        return item != null && ownedTier.TryGetValue(item, out int tier) ? tier : -1;
    }

    // Còn ô trống hoặc đã sở hữu món này (để nâng cấp) thì mới mua được
    public bool HasRoomFor(ItemData item)
    {
        return ownedTier.ContainsKey(item) || ownedOrder.Count < views.Count;
    }

    private void OnItemPurchased(ItemData item, int boughtTier)
    {
        if (item == null) return;

        if (ownedTier.ContainsKey(item))
        {
            // Nâng cấp: giữ nguyên ô, chỉ đổi phẩm
            ownedTier[item] = boughtTier;
        }
        else if (ownedOrder.Count < views.Count)
        {
            ownedOrder.Add(item);
            ownedTier[item] = boughtTier;
        }
        else
        {
            Debug.LogWarning($"[InventoryHUD] Hết ô trống, không hiển thị được {item.DisplayName}.", this);
            return;
        }

        Refresh();
        Changed?.Invoke();
    }

    private void Refresh()
    {
        for (int i = 0; i < views.Count; i++)
        {
            SlotView view = views[i];
            if (i < ownedOrder.Count)
            {
                ItemData item = ownedOrder[i];
                int tier = ownedTier[item];
                Sprite sprite = item.GetTier(tier).icon;

                view.icon.sprite = sprite;
                view.icon.enabled = sprite != null;
            }
            else
            {
                view.icon.sprite = null;
                view.icon.enabled = false;
            }
        }
    }

    private void FindSlotsIfMissing()
    {
        if (slots != null && slots.Length > 0) return;

        List<RectTransform> found = new List<RectTransform>();
        for (int i = 1; i <= 4; i++)
        {
            Transform t = transform.Find("Item" + i);
            if (t == null)
            {
                foreach (Transform child in GetComponentsInChildren<Transform>(true))
                    if (child.name == "Item" + i) { t = child; break; }
            }
            if (t is RectTransform rect) found.Add(rect);
        }
        slots = found.ToArray();
    }

    // Tạo (hoặc tái sử dụng) object con chứa icon, để KHÔNG ghi đè khung ô gốc.
    private SlotView BuildSlotView(RectTransform slot)
    {
        SlotView view = new SlotView();

        Transform iconTransform = slot.Find("ItemIcon");
        if (iconTransform != null) view.icon = iconTransform.GetComponent<Image>();
        if (view.icon == null)
        {
            GameObject go = new GameObject("ItemIcon", typeof(RectTransform), typeof(Image));
            go.transform.SetParent(slot, false);
            RectTransform rect = (RectTransform)go.transform;
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = iconPadding;
            rect.offsetMax = -iconPadding;
            view.icon = go.GetComponent<Image>();
        }
        view.icon.raycastTarget = false;
        view.icon.preserveAspect = true;
        view.icon.enabled = false;

        return view;
    }
}