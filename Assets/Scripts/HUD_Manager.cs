using UnityEngine;
using TMPro;
using UnityEngine.UI;

public class HUD_Manager : MonoBehaviour
{
    [SerializeField] private TeamMember player;
    [SerializeField] private Transform playerStatus;
    [SerializeField] private Slider playerHeartBar;
    [SerializeField] private TMP_Text playerHeartBarText;
    [SerializeField] private TMP_Text mathTime;
    [SerializeField] private TMP_Text goldText;

    private Stat playerStat;
    private Level_Gold playerProgress;
    private float matchSeconds;
    private int displayedSecond = -1;

    private void Awake()
    {
        if (playerStatus == null) playerStatus = transform.Find("PlayerInfor/PlayerStatus");
        if (playerHeartBar == null)
            playerHeartBar = transform.Find("PlayerInfor/PlayerHeartBar")?.GetComponent<Slider>();
        if (playerHeartBarText == null && playerHeartBar != null)
            playerHeartBarText = playerHeartBar.GetComponentInChildren<TMP_Text>(true);
        if (mathTime == null)
            mathTime = transform.Find("MathInfor/MathTime")?.GetComponent<TMP_Text>();
        if (goldText == null)
            goldText = transform.Find("Inventory/Gold/GoldText")?.GetComponent<TMP_Text>();
    }

    private void Update()
    {
        matchSeconds += Time.deltaTime;
        int seconds = Mathf.FloorToInt(matchSeconds);
        if (mathTime != null && seconds != displayedSecond)
        {
            displayedSecond = seconds;
            mathTime.text = $"{seconds / 60:00}:{seconds % 60:00}";
        }

        if (player == null)
        {
            PlayerMovement movement = FindFirstObjectByType<PlayerMovement>();
            if (movement != null) player = movement.GetComponent<TeamMember>();
        }
        if (player == null) return;
        if (playerStat == null) playerStat = player.GetComponent<Stat>();
        if (playerProgress == null) playerProgress = player.GetComponent<Level_Gold>();
        if (goldText != null && playerProgress != null)
            goldText.text = playerProgress.CurrentGold.ToString();
        if (playerStat == null) return;

        SetStatus("AttackText", playerStat.Attack);
        SetStatus("AttackSpeedText", playerStat.AttackSpeed);
        SetStatus("AttackRangeText", playerStat.AttackRange);
        SetStatus("HeartText", playerStat.Heart);
        SetStatus("DefendText", playerStat.Defend);
        SetStatus("SpeedText", playerStat.Speed);

        if (playerHeartBar != null)
        {
            playerHeartBar.minValue = 0f;
            playerHeartBar.maxValue = Mathf.Max(1f, playerStat.Heart);
            playerHeartBar.value = player.CurrentHeart;
        }
        if (playerHeartBarText != null)
            playerHeartBarText.text = $"{Mathf.CeilToInt(player.CurrentHeart)} / {Mathf.CeilToInt(playerStat.Heart)}";
    }

    private void SetStatus(string textName, float value)
    {
        if (playerStatus == null) return;
        foreach (TMP_Text text in playerStatus.GetComponentsInChildren<TMP_Text>(true))
            if (text.name == textName) { text.text = value.ToString("0.##"); return; }
    }
}
