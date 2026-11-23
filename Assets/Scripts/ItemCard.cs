using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class ItemCard : MonoBehaviour
{
    [SerializeField] private TMP_Text nameText;
    [SerializeField] private Image iconImage;
    [SerializeField] private Button button;
    [SerializeField] private GameObject selectedFrame;

    public ItemData Data { get; private set; }
    public int TierIndex { get; private set; }     // Phẩm đang hiển thị trên thẻ (0 = phẩm gốc)
    public event Action<ItemCard> Clicked;

    // Có thể gọi lại nhiều lần để đổi món mà không bị đăng ký click trùng.
    public void Setup(ItemData data, int tierIndex = 0)
    {
        Data = data;
        TierIndex = tierIndex;

        if (nameText == null) nameText = GetComponentInChildren<TMP_Text>(true);
        if (iconImage == null)
        {
            Transform imageObject = transform.Find("ItemCardImage");
            if (imageObject != null) iconImage = imageObject.GetComponent<Image>();
        }
        if (button == null) button = GetComponent<Button>();
        if (selectedFrame == null)
        {
            Transform frame = transform.Find("SelectedFrame");
            if (frame != null) selectedFrame = frame.gameObject;
        }

        ApplyVisual();
        SetSelected(false);
        if (button != null)
        {
            button.onClick.RemoveListener(OnClick);   // tránh đăng ký trùng khi Setup được gọi lại
            button.onClick.AddListener(OnClick);
        }
    }

    // Đổi sang phẩm khác của cùng món (giữ nguyên trạng thái chọn).
    public void SetTier(int tierIndex)
    {
        TierIndex = tierIndex;
        ApplyVisual();
    }

    private void ApplyVisual()
    {
        if (Data == null) return;

        ItemTier tier = Data.GetTier(TierIndex);
        if (nameText != null) nameText.text = Data.GetDisplayName(TierIndex);
        if (iconImage != null)
        {
            iconImage.sprite = tier.icon;
            iconImage.enabled = tier.icon != null;
        }
    }

    public void SetSelected(bool selected)
    {
        if (selectedFrame != null) selectedFrame.SetActive(selected);
    }

    private void OnClick() => Clicked?.Invoke(this);

    private void OnDestroy()
    {
        if (button != null) button.onClick.RemoveListener(OnClick);
    }
}