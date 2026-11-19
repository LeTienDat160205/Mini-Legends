using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class ChampSelectManager : MonoBehaviour
{
    [SerializeField] private List<ChampionData> allChampions;
    [SerializeField] private ChampionCard cardPrefab;
    [SerializeField] private Transform gridContent;
    [SerializeField] private Button lockInButton;
    [SerializeField] private TMP_InputField searchInput;
    [SerializeField] private string gameSceneName = "GameScene";
    [SerializeField] private PlayerSlot mySlot;   // Ô của người chơi (cột trái), hiện tướng đang chọn

    private readonly List<ChampionCard> cards = new List<ChampionCard>();
    private ChampionCard selected;

    private void Start()
    {
        foreach (ChampionData data in allChampions)
        {
            ChampionCard card = Instantiate(cardPrefab, gridContent);
            card.Setup(data);
            card.Clicked += OnCardClicked;
            cards.Add(card);
        }

        if (mySlot != null) mySlot.SetStatus("Picking...");

        lockInButton.interactable = false;
        lockInButton.onClick.AddListener(OnLockIn);
        searchInput.onValueChanged.AddListener(ApplyFilter);
    }

    private void OnCardClicked(ChampionCard card)
    {
        if (selected != null) selected.SetSelected(false);
        selected = card;
        selected.SetSelected(true);
        if (mySlot != null) mySlot.ShowChampion(card.Data);
        lockInButton.interactable = true;
    }

    private void OnLockIn()
    {
        if (selected == null) return;

        if (mySlot != null) mySlot.SetStatus("Locked in");
        ChampSelectResult.PickedChampion = selected.Data;
        SceneManager.LoadScene(gameSceneName);
    }

    private void ApplyFilter(string keyword)
    {
        string key = (keyword ?? "").ToLower();
        foreach (ChampionCard card in cards)
        {
            bool match = key.Length == 0 || card.Data.displayName.ToLower().Contains(key);
            card.gameObject.SetActive(match);
        }
    }
}
