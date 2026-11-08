using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public sealed class GameHUD : MonoBehaviour
{
    [Header("Player")]
    [SerializeField] private Slider hpBar;
    [SerializeField] private TMP_Text hpText;
    [SerializeField] private TMP_Text levelText;

    [Header("Match")]
    [SerializeField] private TMP_Text matchTimer;
    [SerializeField] private TMP_Text allyKillsText;
    [SerializeField] private TMP_Text enemyKillsText;

    [Header("Utility")]
    [SerializeField] private GameObject castingBarPanel;
    [SerializeField] private Slider recallProgress;
    [SerializeField] private TMP_Text progressText;
    [SerializeField] private GameObject scoreboardPanel;

    [Header("System")]
    [SerializeField] private TMP_Text fpsPingText;
    [SerializeField] private Button settingsButton;
    [SerializeField] private Button shopFastButton;

    private float matchSeconds;
    private float currentHp = 2500f;
    private float maxHp = 2500f;
    private float recallRemaining;
    private float fpsTimer;
    private int frames;
    private bool scoreboardHeld;

    private void Awake()
    {
        if (castingBarPanel != null) castingBarPanel.SetActive(false);
        if (scoreboardPanel != null) scoreboardPanel.SetActive(false);
        if (settingsButton != null) settingsButton.onClick.AddListener(OpenSettings);
        if (shopFastButton != null) shopFastButton.onClick.AddListener(OpenShop);
        RefreshHp();
    }

    private void Update()
    {
        matchSeconds += Time.unscaledDeltaTime;
        if (matchTimer != null) matchTimer.text = $"{Mathf.FloorToInt(matchSeconds / 60f):00}:{Mathf.FloorToInt(matchSeconds % 60f):00}";

        frames++;
        fpsTimer += Time.unscaledDeltaTime;
        if (fpsTimer >= 0.5f)
        {
            float fps = frames / fpsTimer;
            if (fpsPingText != null) fpsPingText.text = $"FPS {fps:0}  |  PING 32ms";
            frames = 0;
            fpsTimer = 0f;
        }

        if (Input.GetKeyDown(KeyCode.B)) BeginRecall();
        if (Input.GetKeyDown(KeyCode.P)) OpenShop();
        bool tab = Input.GetKey(KeyCode.Tab);
        if (tab != scoreboardHeld && scoreboardPanel != null) scoreboardPanel.SetActive(tab);
        scoreboardHeld = tab;

        if (recallRemaining > 0f)
        {
            recallRemaining -= Time.unscaledDeltaTime;
            if (recallProgress != null) recallProgress.value = 1f - Mathf.Clamp01(recallRemaining / 3f);
            if (progressText != null) progressText.text = "Đang biến về...";
            if (recallRemaining <= 0f && castingBarPanel != null) castingBarPanel.SetActive(false);
        }
    }

    public void SetHealth(float current, float max)
    {
        currentHp = Mathf.Max(0f, current);
        maxHp = Mathf.Max(1f, max);
        RefreshHp();
    }

    public void SetScores(int ally, int enemy)
    {
        if (allyKillsText != null) allyKillsText.text = ally.ToString();
        if (enemyKillsText != null) enemyKillsText.text = enemy.ToString();
    }

    public void SetLevel(int level)
    {
        if (levelText != null) levelText.text = level.ToString();
    }

    public void BeginRecall()
    {
        recallRemaining = 3f;
        if (castingBarPanel != null) castingBarPanel.SetActive(true);
        if (recallProgress != null) recallProgress.value = 0f;
    }

    private void RefreshHp()
    {
        if (hpBar != null) { hpBar.maxValue = maxHp; hpBar.value = currentHp; }
        if (hpText != null) hpText.text = $"{Mathf.CeilToInt(currentHp)} / {Mathf.CeilToInt(maxHp)}";
    }

    private void OpenSettings() { Debug.Log("HUD: mở menu cài đặt"); }
    private void OpenShop() { Debug.Log("HUD: mở cửa hàng"); }
}
