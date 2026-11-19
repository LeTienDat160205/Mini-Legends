using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class ChampionCard : MonoBehaviour
{
    [SerializeField] private Image portrait;
    [SerializeField] private TMP_Text nameText;
    [SerializeField] private GameObject selectedFrame;
    [SerializeField] private Button button;

    public ChampionData Data { get; private set; }
    public event Action<ChampionCard> Clicked;

    public void Setup(ChampionData data)
    {
        Data = data;
        portrait.sprite = data.portrait;
        nameText.text = data.displayName;
        SetSelected(false);
        button.onClick.AddListener(() => Clicked?.Invoke(this));
    }

    public void SetSelected(bool selected) => selectedFrame.SetActive(selected);
}
