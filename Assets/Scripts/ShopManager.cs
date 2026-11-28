using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

public class ShopManager : MonoBehaviour
{
    [SerializeField] private GameObject shopPanel;
    [SerializeField] private Button shopButton;
    [SerializeField] private Transform content;
    [SerializeField] private GameObject itemCardPrefab;
    [SerializeField] private Transform itemView;
    [SerializeField] private List<ItemData> items = new List<ItemData>();
    [Header("Buy")]
    [SerializeField] private Button buyButton;
    [SerializeField] private TMP_Text buyButtonText;     // Chữ trên nút Mua (tự tìm nếu để trống)
    [SerializeField] private string buyLabel = "Mua";
    [SerializeField] private string upgradeLabel = "Nâng cấp";
    public UnityEvent<ItemData> onItemPurchased;          // Nối với kho đồ / cộng chỉ số cho người chơi
    public event System.Action<ItemData, int> ItemPurchased;   // (món, phẩm vừa mua) cho script khác đăng ký
    [Header("Audio")]
    [SerializeField] private AudioSource uiAudioSource;
    [SerializeField] private AudioClip shopToggleSound;
    [SerializeField] private AudioClip itemPurchaseSound;
    private readonly List<ItemCard> cards = new List<ItemCard>();
    private ItemCard selectedCard;
    private int lastBuyFrame = -1;
    private readonly HashSet<ItemData> ownedItems = new HashSet<ItemData>();   // Món đã mua ít nhất một lần
    private readonly HashSet<ItemData> maxedItems = new HashSet<ItemData>();   // Món đã mua đến phẩm cao nhất

    private void Awake()
    {
        if (shopButton != null) shopButton.onClick.AddListener(ToggleShop);
        if (buyButton != null)
        {
            buyButton.onClick.AddListener(BuyItem);
            if (buyButtonText == null) buyButtonText = buyButton.GetComponentInChildren<TMP_Text>(true);
        }
        if (shopPanel != null) shopPanel.SetActive(false);
    }

    private void Start()
    {
        BuildCards();
        RefreshBuyButton();
    }

    private void OnDestroy()
    {
        if (shopButton != null) shopButton.onClick.RemoveListener(ToggleShop);
        if (buyButton != null) buyButton.onClick.RemoveListener(BuyItem);
        foreach (ItemCard card in cards)
            if (card != null) card.Clicked -= OnCardClicked;
    }

    private void Update() { 
   
if (HotkeysSettings.Pressed(HotkeysSettings.Shop))
    ToggleShop();

}

public void ToggleShop()
    {
        if (shopPanel == null) return;
        shopPanel.SetActive(!shopPanel.activeSelf);
        PlayUiSound(shopToggleSound);
    }

    // Chỉ đóng (không mở lại nếu đang đóng): dùng cho nút Thoát.
    public void CloseShop()
    {
        if (shopPanel == null || !shopPanel.activeSelf) return;
        shopPanel.SetActive(false);
        PlayUiSound(shopToggleSound);
    }

    // ------------------------------------------------------------------
    // Mua đồ
    // ------------------------------------------------------------------

    // Gắn vào Buy Button (script tự đăng ký click, không cần gắn OnClick trong Inspector).
    // Chưa tính tiền. Mua xong, thẻ đang chọn chuyển sang phẩm kế tiếp của chính món đó.
    public void BuyItem()
    {
        if (selectedCard == null || selectedCard.Data == null) return;

        ItemData item = selectedCard.Data;
        if (maxedItems.Contains(item)) return;   // Đã nâng đến cấp cao nhất

        // Chặn gọi đôi: nếu hàm bị gọi hai lần trong cùng một frame thì bỏ qua lần sau.
        if (lastBuyFrame == Time.frameCount) return;
        lastBuyFrame = Time.frameCount;

        int boughtTier = selectedCard.TierIndex;
        PlayItemPurchaseSound();
        ownedItems.Add(item);   // Đã mua món này: các lần sau là Nâng cấp
        onItemPurchased?.Invoke(item);
        ItemPurchased?.Invoke(item, boughtTier);

        if (boughtTier < item.MaxTierIndex)
        {
            selectedCard.SetTier(boughtTier + 1);
            ShowItem(item, boughtTier + 1);
        }
        else
        {
            maxedItems.Add(item);
                                // Vừa mua phẩm cao nhất: khoá nút
        }

        RefreshBuyButton();
    }

    // Bật nút khi đã chọn món; khoá khi món đã đến phẩm cao nhất; đổi chữ Mua / Nâng cấp.
    private void RefreshBuyButton()
    {
        if (buyButton == null) return;

        bool hasItem = selectedCard != null && selectedCard.Data != null;
        bool maxed = hasItem && maxedItems.Contains(selectedCard.Data);
        buyButton.gameObject.SetActive(!maxed);
        buyButton.interactable = hasItem && !maxed;


        if (buyButtonText != null)
        {
            bool owned = hasItem && ownedItems.Contains(selectedCard.Data);
            buyButtonText.text = owned ? upgradeLabel : buyLabel;
        }
    }

    // Call this after a purchase has actually succeeded.
    public void PlayItemPurchaseSound() => PlayUiSound(itemPurchaseSound);

    private void PlayUiSound(AudioClip clip)
    {
        if (uiAudioSource != null && clip != null) uiAudioSource.PlayOneShot(clip);
    }

    private void BuildCards()
    {
        if (content == null || itemCardPrefab == null) return;
        GridLayoutGroup grid = content.GetComponent<GridLayoutGroup>();
        if (grid == null) grid = content.gameObject.AddComponent<GridLayoutGroup>();
        RectTransform prefabRect = itemCardPrefab.transform as RectTransform;
        grid.cellSize = prefabRect != null ? prefabRect.rect.size : new Vector2(160f, 200f);
        grid.spacing = new Vector2(12f, 12f);
        grid.childAlignment = TextAnchor.UpperLeft;
        grid.constraint = GridLayoutGroup.Constraint.FixedColumnCount;
        grid.constraintCount = Mathf.Max(1, Mathf.FloorToInt((content as RectTransform).rect.width / (grid.cellSize.x + grid.spacing.x)));
        ContentSizeFitter fitter = content.GetComponent<ContentSizeFitter>();
        if (fitter == null) fitter = content.gameObject.AddComponent<ContentSizeFitter>();
        fitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;
        foreach (ItemData item in items)
        {
            if (item == null) continue;
            GameObject cardObject = Instantiate(itemCardPrefab, content);
            ItemCard card = cardObject.GetComponent<ItemCard>();
            if (card == null) card = cardObject.AddComponent<ItemCard>();
            card.Setup(item);
            card.Clicked += OnCardClicked;
            cards.Add(card);
        }
        if (cards.Count > 0) OnCardClicked(cards[0]);
    }

    private void OnCardClicked(ItemCard card)
    {
        if (selectedCard != null) selectedCard.SetSelected(false);
        selectedCard = card;
        selectedCard.SetSelected(true);
        ShowItem(card.Data, card.TierIndex);
        RefreshBuyButton();
    }

    private void ShowItem(ItemData item, int tierIndex)
    {
        if (itemView == null || item == null) return;
        ItemTier tier = item.GetTier(tierIndex);

        Image itemFrame = itemView.Find("ItemFrame")?.GetComponent<Image>();
        if (itemFrame != null)
        {
            itemFrame.sprite = tier.icon;
            itemFrame.enabled = tier.icon != null;
            itemFrame.preserveAspect = true;
        }
        Transform info = itemView.Find("ItemInfor");
        if (info != null)
        {
            string[] names = { "Attack", "AttackSpeed", "AttackRange", "Heart", "Defend", "Speed" };
            float[] values = { tier.attack, tier.attackSpeed, tier.attackRange, tier.heart, tier.defend, tier.speed };
            int visible = 0;
            for (int i = 0; i < names.Length; i++)
            {
                Transform row = info.Find(names[i]);
                TMP_Text label = row != null ? row.GetComponentInChildren<TMP_Text>(true) : null;
                bool show = values[i] > 0f;
                if (row != null) row.gameObject.SetActive(show);
                if (label != null)
                {
                    label.gameObject.SetActive(show);
                    label.text = $"+{values[i]:0.##}";
                }
                if (!show) continue;
                int column = visible / 3;
                int line = visible % 3;
                if (row is RectTransform rowRect)
                    rowRect.anchoredPosition = new Vector2(column == 0 ? -75f : 75f, 100f - line * 40f);
                visible++;
            }
        }
        foreach (TMP_Text field in itemView.GetComponentsInChildren<TMP_Text>(true))
        {
            switch (field.name)
            {
                case "ItemName": field.text = item.GetDisplayName(tierIndex); break;
                case "ItemEffect": field.text = tier.description + (tier.canActive ? $"\nKích hoạt: {tier.activeDescription}" : ""); break;
                case "ActiveEffect": field.text = tier.canActive ? $"{tier.activeEffect}: {tier.activeDescription}\nGiá trị: {tier.activeValue}, thời gian: {tier.activeDuration}s, hồi: {tier.activeCooldown}s" : "Không có kích hoạt"; break;
            }
        }
    }

}