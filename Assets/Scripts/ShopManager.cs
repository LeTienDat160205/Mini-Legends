using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class ShopManager : MonoBehaviour
{
    [SerializeField] private GameObject shopPanel;
    [SerializeField] private Button shopButton;
    [SerializeField] private Transform content;
    [SerializeField] private GameObject itemCardPrefab;
    [SerializeField] private Transform itemView;
    [SerializeField] private List<ItemData> items = new List<ItemData>();
    private readonly List<ItemCard> cards = new List<ItemCard>();
    private ItemCard selectedCard;

    private void Awake()
    {
        if (shopButton != null) shopButton.onClick.AddListener(ToggleShop);
        if (shopPanel != null) shopPanel.SetActive(false);
    }

    private void Start() => BuildCards();

    private void OnDestroy()
    {
        if (shopButton != null) shopButton.onClick.RemoveListener(ToggleShop);
        foreach (ItemCard card in cards)
            if (card != null) card.Clicked -= OnCardClicked;
    }

    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.P)) ToggleShop();
    }

    public void ToggleShop()
    {
        if (shopPanel != null) shopPanel.SetActive(!shopPanel.activeSelf);
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
        ShowItem(card.Data);
    }

    private void ShowItem(ItemData item)
    {
        if (itemView == null || item == null) return;
        Image itemFrame = itemView.Find("ItemFrame")?.GetComponent<Image>();
        if (itemFrame != null)
        {
            itemFrame.sprite = item.icon;
            itemFrame.enabled = item.icon != null;
            itemFrame.preserveAspect = true;
        }
        Transform info = itemView.Find("ItemInfor");
        if (info != null)
        {
            string[] names = { "Attack", "AttackSpeed", "AttackRange", "Heart", "Defend", "Speed" };
            float[] values = { item.attack, item.attackSpeed, item.attackRange, item.heart, item.defend, item.speed };
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
                case "ItemName": field.text = item.DisplayName; break;
                case "ItemEffect": field.text = item.description + (item.canActive ? $"\nKích hoạt: {item.activeDescription}" : ""); break;
                case "ActiveEffect": field.text = item.canActive ? $"{item.activeEffect}: {item.activeDescription}\nGiá trị: {item.activeValue}, thời gian: {item.activeDuration}s, hồi: {item.activeCooldown}s" : "Không có kích hoạt"; break;
            }
        }
    }

}
