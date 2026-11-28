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
    [SerializeField] private TMP_Text buyButtonText;
    [SerializeField] private string buyLabel = "Mua";
    [SerializeField] private string upgradeLabel = "Nâng cấp";
    [Header("Túi đồ")]
    [Tooltip("Để trống: tự tìm InventoryHUD trong scene")]
    [SerializeField] private InventoryHUD inventory;
    [SerializeField] private string inventoryFullLabel = "Túi đồ đầy";
    public UnityEvent<ItemData> onItemPurchased;
    public event System.Action<ItemData, int> ItemPurchased;

    [Header("Giá tiền")]
    [Tooltip("Tuỳ chọn: ô chữ hiện giá của phẩm đang chọn (để trống nếu không cần)")]
    [SerializeField] private TMP_Text priceText;
    [SerializeField] private string priceFormat = "Giá: {0}";
    [SerializeField] private Color affordableColor = Color.white;
    [SerializeField] private Color notAffordableColor = new Color(1f, 0.3f, 0.3f);

    [Header("Audio")]
    [SerializeField] private AudioSource uiAudioSource;
    [SerializeField] private AudioClip shopToggleSound;
    [SerializeField] private AudioClip itemPurchaseSound;
    [SerializeField] private AudioClip cannotBuySound;

    private readonly List<ItemCard> cards = new List<ItemCard>();
    private ItemCard selectedCard;
    private int lastBuyFrame = -1;
    private readonly HashSet<ItemData> ownedItems = new HashSet<ItemData>();
    private readonly HashSet<ItemData> maxedItems = new HashSet<ItemData>();

    private Level_Gold wallet;

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
        TryBindWallet();

        if (inventory == null) inventory = FindFirstObjectByType<InventoryHUD>(FindObjectsInactive.Include);
        if (inventory != null) inventory.Changed += RefreshBuyButton;
        else Debug.LogWarning("[ShopManager] Không tìm thấy InventoryHUD nên không chặn được khi hết ô.", this);

        RefreshBuyButton();
    }

    private void OnDestroy()
    {
        if (shopButton != null) shopButton.onClick.RemoveListener(ToggleShop);
        if (buyButton != null) buyButton.onClick.RemoveListener(BuyItem);
        foreach (ItemCard card in cards)
            if (card != null) card.Clicked -= OnCardClicked;
        if (wallet != null) wallet.ProgressChanged -= OnGoldChanged;
        if (inventory != null) inventory.Changed -= RefreshBuyButton;
    }

    private void Update()
    {
        if (HotkeysSettings.Pressed(HotkeysSettings.Shop))
            ToggleShop();

        if (wallet == null || (SpawnArea.SpawnedPlayer != null && wallet.gameObject != SpawnArea.SpawnedPlayer))
            TryBindWallet();
    }

    private void TryBindWallet()
    {
        GameObject player = SpawnArea.SpawnedPlayer;
        if (player == null) return;

        Level_Gold found = player.GetComponent<Level_Gold>();
        if (found == null || found == wallet) return;

        if (wallet != null) wallet.ProgressChanged -= OnGoldChanged;
        wallet = found;
        wallet.ProgressChanged += OnGoldChanged;
        RefreshBuyButton();
    }

    private void OnGoldChanged()
    {
        RefreshBuyButton();
    }

    public void ToggleShop()
    {
        if (shopPanel == null) return;
        shopPanel.SetActive(!shopPanel.activeSelf);
        PlayUiSound(shopToggleSound);
    }

    public void CloseShop()
    {
        if (shopPanel == null || !shopPanel.activeSelf) return;
        shopPanel.SetActive(false);
        PlayUiSound(shopToggleSound);
    }

    private int GetCurrentPrice()
    {
        if (selectedCard == null || selectedCard.Data == null) return 0;
        return selectedCard.Data.GetTier(selectedCard.TierIndex).gold;
    }

    private bool CanAfford(int price)
    {
        if (price <= 0) return true;
        return wallet != null && wallet.CurrentGold >= price;
    }

    public void BuyItem()
    {
        if (selectedCard == null || selectedCard.Data == null) return;

        ItemData item = selectedCard.Data;
        if (maxedItems.Contains(item)) return;

        if (lastBuyFrame == Time.frameCount) return;
        lastBuyFrame = Time.frameCount;

        int boughtTier = selectedCard.TierIndex;
        int price = item.GetTier(boughtTier).gold;

        if (inventory != null && !inventory.HasRoomFor(item))
        {
            PlayUiSound(cannotBuySound);
            RefreshBuyButton();
            return;
        }

        if (price > 0)
        {
            if (wallet == null) TryBindWallet();
            if (wallet == null)
            {
                Debug.LogWarning("[ShopManager] Chưa tìm thấy Level_Gold của người chơi nên không thể mua.", this);
                PlayUiSound(cannotBuySound);
                return;
            }
            if (!wallet.TrySpendGold(price))
            {
                PlayUiSound(cannotBuySound);
                RefreshBuyButton();
                return;
            }
        }

        PlayItemPurchaseSound();
        ownedItems.Add(item);
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
        }

        RefreshBuyButton();
    }

    private void RefreshBuyButton()
    {
        bool hasItem = selectedCard != null && selectedCard.Data != null;
        bool maxed = hasItem && maxedItems.Contains(selectedCard.Data);
        int price = hasItem && !maxed ? GetCurrentPrice() : 0;
        bool affordable = CanAfford(price);
        bool hasRoom = !hasItem || inventory == null || inventory.HasRoomFor(selectedCard.Data);

        if (buyButton != null)
        {
            buyButton.gameObject.SetActive(!maxed);
            buyButton.interactable = hasItem && !maxed && affordable && hasRoom;

            if (buyButtonText != null)
            {
                bool owned = hasItem && ownedItems.Contains(selectedCard.Data);
                buyButtonText.text = !hasRoom ? inventoryFullLabel : (owned ? upgradeLabel : buyLabel);
            }
        }

        if (priceText != null)
        {
            priceText.gameObject.SetActive(hasItem && !maxed);
            priceText.text = string.Format(priceFormat, price);
            priceText.color = affordable ? affordableColor : notAffordableColor;
        }
    }

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