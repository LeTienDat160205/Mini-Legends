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
    public event Action<ItemCard> Clicked;

    public void Setup(ItemData data)
    {
        Data = data;
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
        if (nameText != null) nameText.text = data.DisplayName;
        if (iconImage != null) { iconImage.sprite = data.icon; iconImage.enabled = data.icon != null; }
        SetSelected(false);
        if (button != null) button.onClick.AddListener(OnClick);
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
