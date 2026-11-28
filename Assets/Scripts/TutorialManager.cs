using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public enum TutorialCondition
{
    NextButton,     // Chờ người chơi bấm nút "Tiếp tục"
    MoveDistance,   // Đi đủ quãng đường moveDistance (không phụ thuộc cách điều khiển)
    LeftClick,      // Click chuột trái
    KeyUp,          // Thả phím `key`
    DashStarted,    // Blade Assasin bắt đầu lướt (kỹ năng E)
    ShopOpened,     // Bảng cửa hàng đang mở
    DummyHit        // Gây sát thương lên một bia tập
}

[System.Serializable]
public class TutorialStep
{
    public string title;
    [TextArea(2, 5)] public string body;
    public TutorialCondition condition;
    [Tooltip("MoveDistance: quãng đường (đơn vị) cần đi")]
    public float moveDistance = 3f;
    [Tooltip("KeyUp: phím cần thả")]
    public KeyCode key = KeyCode.Q;
    [Tooltip("Vùng UI được khoanh sáng (để trống = không khoanh)")]
    public RectTransform highlight;
}

/// <summary>
/// Chạy hướng dẫn chơi theo từng bước trên một panel, đồng thời:
/// - ép SpawnArea sinh Blade Assasin (nhân vật dự phòng) thay vì tướng đã chọn trước đó,
/// - nhấn ESC để thoát về menu,
/// - khi Nexus bị phá (trận kết thúc) thì hiện thông báo rồi về menu.
/// </summary>
public class TutorialManager : MonoBehaviour
{
    [SerializeField] private List<TutorialStep> steps = new List<TutorialStep>();

    [Header("UI")]
    [SerializeField] private GameObject panel;
    [SerializeField] private TMP_Text titleText;
    [SerializeField] private TMP_Text bodyText;
    [SerializeField] private TMP_Text counterText;
    [SerializeField] private TMP_Text hintText;
    [SerializeField] private Button nextButton;
    [SerializeField] private Button skipButton;
    [SerializeField] private RectTransform highlightFrame;
    [SerializeField] private Canvas canvas;
    [SerializeField] private float highlightPadding = 8f;

    [Header("Điều kiện hoàn thành bước")]
    [SerializeField] private GameObject shopPanel;     // Bảng cửa hàng (cho bước ShopOpened)
    [SerializeField] private List<TeamMember> dummies = new List<TeamMember>();   // Bia tập (cho bước DummyHit)
    [SerializeField] private float stepCompleteDelay = 0.8f;

    [Header("Thoát / kết thúc trận")]
    [SerializeField] private string menuSceneName = "Mode Menu_Styled";
    [SerializeField] private KeyCode exitKey = KeyCode.Escape;
    [SerializeField] private float matchEndDelay = 3f;
    [SerializeField] private Nexus[] nexuses;          // Để trống sẽ tự tìm trong scene

    private BladeAssasin player;
    private ChampionData previousPick;
    private int index = -1;
    private float completedAt = -1f;
    private bool startCaptured;
    private bool dummyHit;
    private bool finished;
    private bool matchEnding;
    private bool leaving;
    private float leaveAt;
    private Vector2 stepStartPos;
    private TMP_Text nextLabel;
    private readonly Vector3[] corners = new Vector3[4];

    private void Awake()
    {
        // SpawnArea chỉ dùng prefab dự phòng (Blade Assasin) khi không có tướng nào được chọn.
        previousPick = ChampSelectResult.PickedChampion;
        ChampSelectResult.PickedChampion = null;
    }

    private void Start()
    {
        if (nextButton != null)
        {
            nextButton.onClick.AddListener(OnNextClicked);
            nextLabel = nextButton.GetComponentInChildren<TMP_Text>(true);
        }
        if (skipButton != null) skipButton.onClick.AddListener(Finish);

        foreach (TeamMember dummy in dummies)
            if (dummy != null) dummy.Damaged += OnDummyDamaged;

        if (nexuses == null || nexuses.Length == 0)
            nexuses = FindObjectsByType<Nexus>(FindObjectsSortMode.None);

        ShowStep(0);
    }

    private void OnDestroy()
    {
        if (nextButton != null) nextButton.onClick.RemoveListener(OnNextClicked);
        if (skipButton != null) skipButton.onClick.RemoveListener(Finish);
        foreach (TeamMember dummy in dummies)
            if (dummy != null) dummy.Damaged -= OnDummyDamaged;
    }

    private void OnDummyDamaged(float damage) => dummyHit = true;

    private void Update()
    {
        // ESC thoát về menu bất cứ lúc nào.
        if (Input.GetKeyDown(exitKey))
        {
            LeaveToMenu();
            return;
        }

        // Đang đếm ngược sau khi trận kết thúc (dùng thời gian thật, phòng khi game đóng băng timeScale).
        if (matchEnding)
        {
            if (Time.unscaledTime >= leaveAt) LeaveToMenu();
            return;
        }

        if (player == null)
        {
            GameObject spawned = SpawnArea.SpawnedPlayer;
            if (spawned != null) player = spawned.GetComponent<BladeAssasin>();
        }

        if (IsNexusDestroyed())
        {
            BeginMatchEnd();
            return;
        }

        if (finished || index < 0 || index >= steps.Count) return;

        // Ghi lại vị trí bắt đầu của bước (cho bước đi bộ), kể cả khi nhân vật xuất hiện muộn.
        if (!startCaptured && player != null)
        {
            stepStartPos = player.transform.position;
            startCaptured = true;
        }

        UpdateHighlight();

        // Đã hoàn thành bước: chờ một chút cho người chơi kịp thấy rồi sang bước kế.
        if (completedAt >= 0f)
        {
            if (Time.unscaledTime - completedAt >= stepCompleteDelay) ShowStep(index + 1);
            return;
        }

        if (IsStepDone(steps[index]))
        {
            completedAt = Time.unscaledTime;
            if (hintText != null) hintText.text = "Tốt lắm!";
        }
    }

    private bool IsStepDone(TutorialStep step)
    {
        switch (step.condition)
        {
            case TutorialCondition.MoveDistance:
                return startCaptured && player != null &&
                       Vector2.Distance(player.transform.position, stepStartPos) >= step.moveDistance;
            case TutorialCondition.LeftClick:
                return Input.GetMouseButtonDown(0);
            case TutorialCondition.KeyUp:
                return Input.GetKeyUp(step.key);
            case TutorialCondition.DashStarted:
                return player != null && player.IsDashing;
            case TutorialCondition.ShopOpened:
                return shopPanel != null && shopPanel.activeInHierarchy;
            case TutorialCondition.DummyHit:
                return dummyHit;
            default:
                return false;   // NextButton: chờ bấm nút
        }
    }

    private void ShowStep(int i)
    {
        if (i >= steps.Count)
        {
            Finish();
            return;
        }

        index = i;
        completedAt = -1f;
        startCaptured = false;
        dummyHit = false;

        TutorialStep step = steps[i];
        bool needsNext = step.condition == TutorialCondition.NextButton;

        if (panel != null) panel.SetActive(true);
        if (titleText != null) titleText.text = step.title;
        if (bodyText != null) bodyText.text = step.body;
        if (counterText != null) counterText.text = (i + 1) + "/" + steps.Count;
        if (hintText != null) hintText.text = needsNext ? "" : "Hãy làm theo hướng dẫn để tiếp tục.";

        if (nextButton != null) nextButton.gameObject.SetActive(needsNext);
        if (nextLabel != null) nextLabel.text = i == steps.Count - 1 ? "Hoàn thành" : "Tiếp tục";

        UpdateHighlight();
    }

    private void OnNextClicked()
    {
        if (!matchEnding) ShowStep(index + 1);
    }

    // Kết thúc hướng dẫn: chỉ ẩn panel, người chơi vẫn chơi tiếp. Thoát bằng ESC hoặc khi phá Nexus.
    private void Finish()
    {
        finished = true;
        index = -1;
        if (panel != null) panel.SetActive(false);
        if (highlightFrame != null) highlightFrame.gameObject.SetActive(false);
    }

    // ------------------------------------------------------------------
    // Kết thúc trận / thoát
    // ------------------------------------------------------------------
    private bool IsNexusDestroyed()
    {
        if (nexuses == null) return false;

        foreach (Nexus nexus in nexuses)
        {
            if (nexus == null) return true;   // đã bị Destroy

            ARAMCombatStructure structure = nexus.GetComponentInParent<ARAMCombatStructure>(true);
            if (structure != null && structure.isDestroyed) return true;
            if (!nexus.gameObject.activeInHierarchy) return true;
        }
        return false;
    }

    private void BeginMatchEnd()
    {
        matchEnding = true;
        leaveAt = Time.unscaledTime + matchEndDelay;

        if (panel != null) panel.SetActive(true);
        if (titleText != null) titleText.text = "Trận đấu kết thúc!";
        if (bodyText != null)
            bodyText.text = "Nexus đã bị phá. Bạn sẽ quay về menu sau " + Mathf.CeilToInt(matchEndDelay) +
                            " giây (hoặc nhấn ESC để về ngay).";
        if (counterText != null) counterText.text = "";
        if (hintText != null) hintText.text = "";
        if (nextButton != null) nextButton.gameObject.SetActive(false);
        if (skipButton != null) skipButton.gameObject.SetActive(false);
        if (highlightFrame != null) highlightFrame.gameObject.SetActive(false);
    }

    private void LeaveToMenu()
    {
        if (leaving) return;
        leaving = true;

        Time.timeScale = 1f;
        ChampSelectResult.PickedChampion = previousPick;   // trả lại lựa chọn tướng trước đó

        if (string.IsNullOrEmpty(menuSceneName))
        {
            Debug.LogError("TutorialManager: chưa đặt tên scene menu.", this);
            return;
        }
        SceneManager.LoadScene(menuSceneName);
    }

    // Khoanh sáng vùng UI của bước hiện tại. Dùng cho Canvas kiểu Screen Space - Overlay.
    private void UpdateHighlight()
    {
        if (highlightFrame == null) return;

        RectTransform target = index >= 0 && index < steps.Count ? steps[index].highlight : null;
        if (target == null || !target.gameObject.activeInHierarchy)
        {
            highlightFrame.gameObject.SetActive(false);
            return;
        }

        target.GetWorldCorners(corners);
        Vector3 min = corners[0];
        Vector3 max = corners[2];
        float scale = canvas != null && canvas.scaleFactor > 0f ? canvas.scaleFactor : 1f;

        highlightFrame.gameObject.SetActive(true);
        highlightFrame.position = (min + max) * 0.5f;
        highlightFrame.sizeDelta = new Vector2(
            (max.x - min.x) / scale + highlightPadding * 2f,
            (max.y - min.y) / scale + highlightPadding * 2f);
    }
}
