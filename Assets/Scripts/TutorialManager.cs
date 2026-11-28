using System;
using System.Collections;
using System.Reflection;
using TMPro;
using Unity.Cinemachine;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

/// <summary>
/// Điều khiển TutorialScene: hiện panel hướng dẫn theo từng bước.
///  - Mỗi bước: panel to giữa màn hình + dừng thời gian + nút OK.
///  - Bấm OK: panel thu nhỏ sang bên phải, thời gian chạy lại, chờ người chơi làm đúng yêu cầu.
///  - Xong yêu cầu: panel to lại ở giữa cho bước kế tiếp.
/// Các bước: Di chuyển -> (dịch chuyển giữa map + dummy Goblin) -> Đánh thường -> E -> Q -> Minimap -> Shop -> Phá Nexus.
/// ESC bất cứ lúc nào: quay về menu. Phá Nexus: script Nexus có sẵn của ARAM tự kết thúc trận và quay về menu.
/// </summary>
[DefaultExecutionOrder(-1000)]
public class TutorialManager : MonoBehaviour
{
    [Header("Prefab (kéo 3 champion prefab giống ARAM3v3MatchManager)")]
    [Tooltip("Tự chọn prefab có BladeAssasin làm người chơi, prefab có tên chứa 'Goblin' làm dummy.")]
    [SerializeField] private GameObject[] championPrefabs;
    [SerializeField] private string dummyNameContains = "Goblin";

    [Header("Scene")]
    [SerializeField] private string menuSceneName = "ModeMenu_Styled";
    [SerializeField] private string mapCenterObjectName = "Center_Emblem";
    [SerializeField] private string enemyNexusObjectName = "Nexus_Red";
    [SerializeField] private string endGamePanelName = "EndGamePanel";
    [SerializeField] private Vector2 dummyOffset = new Vector2(1.6f, 0f);
    [SerializeField] private float moveGoalDistance = 6f;

    [Header("Panel")]
    [SerializeField] private Vector2 centerSize = new Vector2(780f, 440f);
    [SerializeField] private Vector2 dockSize = new Vector2(380f, 210f);
    [SerializeField] private Vector2 dockOffset = new Vector2(-24f, -60f);
    [SerializeField] private float animSeconds = 0.35f;

    [Header("Dummy: tên component (chứa chuỗi này) sẽ bị tắt để dummy đứng yên")]
    [SerializeField] private string[] dummyDisableKeywords = { "Movement", "Controller", "Brain", "Chase", "Patrol", "Wander" };

    private class Step
    {
        public string title, body, hint;
        public string[] highlight;
        public float minDock;
        public Func<bool> done;
        public Action onDone;
    }

    // ---- gameplay refs
    private GameObject blade, dummy, bladePrefab, dummyPrefab;
    private BladeAssasin bladeScript;
    private PlayerMovement bladeMove;
    private ARAMCombatStructure nexusStructure;
    private float dummyStartHeart;
    private bool dummyDamaged;

    // ---- UI
    private TMP_FontAsset font;
    private RectTransform canvasRt, panel, highlightRt;
    private CanvasGroup okGroup, bodyGroup, hintGroup;
    private Image dim, fade, highlightImg;
    private TextMeshProUGUI titleText, bodyText, hintText, stepText;
    private Button okButton;
    private bool okClicked, ending, tutorialRunning;
    private RectTransform highlightTarget;
    private string[] highlightNames;
    private float panelT; // 0 = thu nhỏ bên phải, 1 = to giữa màn hình

    private static readonly Color Gold = new Color(1f, 0.78f, 0.25f, 1f);
    private static readonly Color PanelBg = new Color(0.07f, 0.09f, 0.15f, 0.97f);

    // =====================================================================
    private void Awake()
    {
        Time.timeScale = 1f;
        PickPrefabs();
        ForceBladeAssassinSpawn();
        BuildUi();
    }

    private void Start()
    {
        GameObject nexus = FindByName(enemyNexusObjectName);
        if (nexus != null) nexusStructure = nexus.GetComponent<ARAMCombatStructure>();
        StartCoroutine(RunTutorial());
    }

    private void OnDestroy()
    {
        Time.timeScale = 1f;
    }

    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.Escape)) ExitToMenu();
        UpdateHighlight();
    }

    public void ExitToMenu()
    {
        Time.timeScale = 1f;
        SceneManager.LoadScene(menuSceneName);
    }

    // =====================================================================
    //  Chuẩn bị prefab / người chơi
    // =====================================================================
    private void PickPrefabs()
    {
        if (championPrefabs == null) return;
        foreach (GameObject p in championPrefabs)
        {
            if (p == null) continue;
            if (bladePrefab == null && p.GetComponent<BladeAssasin>() != null) bladePrefab = p;
        }
        foreach (GameObject p in championPrefabs)
        {
            if (p == null || p == bladePrefab) continue;
            if (p.name.IndexOf(dummyNameContains, StringComparison.OrdinalIgnoreCase) >= 0) { dummyPrefab = p; break; }
        }
        if (dummyPrefab == null)
            foreach (GameObject p in championPrefabs)
                if (p != null && p != bladePrefab && p.name.IndexOf("Soldier", StringComparison.OrdinalIgnoreCase) < 0) { dummyPrefab = p; break; }
        if (bladePrefab == null) Debug.LogError("[Tutorial] Không tìm thấy prefab có BladeAssasin trong championPrefabs.", this);
        if (dummyPrefab == null) Debug.LogError("[Tutorial] Không tìm thấy prefab GoblinScout trong championPrefabs.", this);
    }

    // SpawnArea chạy sau (execution order -1000 của mình chạy trước) nên ép prefab người chơi thành Blade Assassin.
    private void ForceBladeAssassinSpawn()
    {
        if (bladePrefab == null) return;
        foreach (SpawnArea area in FindObjectsByType<SpawnArea>(FindObjectsInactive.Include, FindObjectsSortMode.None))
        {
            FieldInfo f = typeof(SpawnArea).GetField("fallbackPlayerPrefab",
                BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
            if (f != null && f.GetValue(area) != null) f.SetValue(area, bladePrefab);
        }
    }

    private bool RefreshPlayer()
    {
        GameObject p = SpawnArea.SpawnedPlayer;
        if (p == null) return false;
        if (p != blade)
        {
            blade = p;
            bladeScript = p.GetComponent<BladeAssasin>();
            bladeMove = p.GetComponent<PlayerMovement>();
            if (bladeScript == null)
                Debug.LogError("[Tutorial] Người chơi spawn ra không phải Blade Assassin. Hãy kiểm tra SpawnArea (team Blue) / lựa chọn nhân vật từ menu.", p);
        }
        return true;
    }

    private void LockPlayer(bool locked)
    {
        if (bladeScript != null) bladeScript.SetPlayerInput(!locked);
        if (bladeMove != null) bladeMove.enabled = !locked;
    }

    private void Pause(bool paused) { Time.timeScale = paused ? 0f : 1f; }

    // =====================================================================
    //  Luồng hướng dẫn
    // =====================================================================
    private IEnumerator RunTutorial()
    {
        tutorialRunning = true;
        while (!RefreshPlayer()) yield return null;
        yield return null;

        Step[] steps = BuildSteps();
        for (int i = 0; i < steps.Length; i++)
        {
            if (nexusStructure != null && nexusStructure.isDestroyed) break;
            yield return RunStep(steps[i], i + 1, steps.Length);
        }

        // Kết thúc: Nexus có sẵn của ARAM tự hiện bảng kết quả và quay về menu.
        tutorialRunning = false;
        ending = true;
        Pause(false);
        LockPlayer(false);
        highlightNames = null;
        yield return AnimatePanel(panelT, 0f);
        if (panel != null) panel.gameObject.SetActive(false);
    }

    private Step[] BuildSteps()
    {
        float moved = 0f;
        Vector2 lastPos = Vector2.zero;
        bool lastPosSet = false;
        bool sawDash = false;
        bool qHeld = false, qReleased = false;
        int qReleaseFrame = 0;
        bool shopWasOpen = false;
        GameObject shopPanel = FindByName("ShopPanel");

        return new[]
        {
            new Step {
                title = "Di chuyển",
                body = "Chào mừng đến với đấu trường! Bạn điều khiển Blade Assassin.\n\nDùng các phím W A S D (hoặc phím mũi tên) để di chuyển nhân vật. Hãy thử đi một đoạn nhé.",
                hint = "Di chuyển bằng W A S D / phím mũi tên.",
                done = () => {
                    Vector2 p = blade != null ? (Vector2)blade.transform.position : Vector2.zero;
                    if (lastPosSet) { float d = Vector2.Distance(p, lastPos); if (d < 2f) moved += d; }
                    lastPos = p; lastPosSet = true;
                    return moved >= moveGoalDistance;
                },
                onDone = () => StartCoroutine(TeleportToCenterRoutine())
            },
            new Step {
                title = "Đánh thường",
                body = "Bạn đã được đưa ra giữa bản đồ. Một mục tiêu giả (Goblin Scout) đứng bên cạnh.\n\nNhấp CHUỘT TRÁI để tấn công kẻ địch trong tầm đánh. Giữ chuột trái để hiện vòng tầm đánh.",
                hint = "Nhấp chuột trái để đánh Goblin Scout.",
                done = () => { EnsureDummy(); return DummyWasHit(); }
            },
            new Step {
                title = "Kỹ năng E - Lướt",
                body = "GIỮ phím E để hiện vùng lướt, di chuột để chọn hướng, rồi THẢ E để lướt và gây sát thương lên kẻ địch trên đường đi.\n\nNhấp chuột trái khi đang giữ E để hủy kỹ năng.",
                hint = "Giữ E, chọn hướng bằng chuột, thả E để lướt.",
                highlight = new[] { "SkillE" },
                done = () => { EnsureDummy(); NearDummy(); if (bladeScript != null && bladeScript.IsDashing) sawDash = true; return sawDash; }
            },
            new Step {
                title = "Kỹ năng Q - Vùng sát thương",
                body = "GIỮ phím Q để hiện vùng tác dụng, THẢ Q để tạo vùng sát thương kéo dài 3 giây, gây sát thương liên tục lên kẻ địch đứng trong vùng.\n\nHãy đứng gần Goblin Scout. Nhấp chuột trái khi đang giữ Q để hủy.",
                hint = "Giữ Q rồi thả Q khi Goblin ở gần bạn.",
                highlight = new[] { "SkillQ" },
                done = () => {
                    EnsureDummy(); NearDummy();
                    if (HotkeysSettings.Pressed(HotkeysSettings.SkillQ)) { qHeld = true; qReleased = false; }
                    if (qHeld && HotkeysSettings.Released(HotkeysSettings.SkillQ)) { qReleased = true; qHeld = false; qReleaseFrame = Time.frameCount; }
                    // Q thật sự được tung ra (không bị hủy bằng chuột) nếu sau khi thả vẫn đang dùng kỹ năng.
                    if (qReleased && Time.frameCount > qReleaseFrame + 1)
                    {
                        if (bladeScript != null && bladeScript.IsUsingSkill) return true;
                        qReleased = false;
                    }
                    return false;
                }
            },
            new Step {
                title = "Bản đồ nhỏ (Minimap)",
                body = "Bản đồ nhỏ cho bạn thấy toàn bộ đấu trường: vị trí của bạn, đồng đội, kẻ địch, các trụ phòng thủ và Nexus.\n\nHãy liếc nhìn minimap thường xuyên để biết đường đi và nguy hiểm xung quanh.",
                hint = "Quan sát khung Minimap được đánh dấu.",
                highlight = new[] { "MinimapImage", "Minimap" },
                minDock = 4f
            },
            new Step {
                title = "Cửa hàng - Mở shop",
                body = "Cửa hàng giúp bạn mua trang bị để mạnh hơn.\n\nNhấp nút CỬA HÀNG (được đánh dấu) hoặc dùng phím tắt cửa hàng để mở.",
                hint = "Mở cửa hàng bằng nút được đánh dấu.",
                highlight = new[] { "ShopButton" },
                done = () => shopPanel != null && shopPanel.activeInHierarchy
            },
            new Step {
                title = "Cửa hàng - Mua đồ",
                body = "Nhấp vào một vật phẩm để xem chỉ số, rồi bấm MUA. Cần đủ vàng; vàng có được khi hạ kẻ địch và lính. Túi đồ có số ô giới hạn.\n\nMua thử nếu bạn muốn, rồi ĐÓNG cửa hàng (nút X hoặc phím tắt) để tiếp tục.",
                hint = "Đóng cửa hàng khi xem xong.",
                done = () => {
                    if (shopPanel == null) return true;
                    if (shopPanel.activeInHierarchy) shopWasOpen = true;
                    return shopWasOpen && !shopPanel.activeInHierarchy;
                }
            },
            new Step {
                title = "Phá Nexus!",
                body = "Mục tiêu cuối cùng: đi về phía căn cứ địch (bên phải bản đồ) và phá hủy NEXUS để kết thúc trận.\n\nTrụ phòng thủ đã được tắt trong bài hướng dẫn. Bạn có thể nhấn ESC bất cứ lúc nào để thoát về màn hình chọn chế độ.",
                hint = "Đến căn cứ địch và phá Nexus. ESC để thoát.",
                done = () => NexusFinished()
            },
        };
    }

    private IEnumerator RunStep(Step s, int index, int total)
    {
        // Không dừng thời gian giữa chừng một kỹ năng (để HUD hồi chiêu chạy đúng).
        float guard = Time.unscaledTime + 8f;
        while (bladeScript != null && bladeScript.IsUsingSkill && Time.unscaledTime < guard) yield return null;
        RefreshPlayer();

        // 1) Panel to giữa màn hình, dừng thời gian, chờ OK
        titleText.text = s.title;
        bodyText.text = s.body;
        hintText.text = s.hint ?? "";
        stepText.text = "Bước " + index + "/" + total;
        highlightNames = s.highlight;
        highlightTarget = null;
        okClicked = false;
        panel.gameObject.SetActive(true);
        LockPlayer(true);
        Pause(true);
        yield return AnimatePanel(0f, 1f);
        while (!okClicked && !Input.GetKeyDown(KeyCode.Return) && !Input.GetKeyDown(KeyCode.KeypadEnter)) yield return null;

        // 2) Thu nhỏ sang bên phải, chạy lại thời gian
        yield return AnimatePanel(1f, 0f);
        Pause(false);
        yield return null; // bỏ qua 1 frame để cú click OK không biến thành đòn đánh
        LockPlayer(false);

        // 3) Chờ người chơi hoàn thành yêu cầu
        float minEnd = Time.unscaledTime + s.minDock;
        while (Time.unscaledTime < minEnd) yield return null;
        if (s.done != null)
        {
            while (!s.done()) yield return null;
        }
        s.onDone?.Invoke();
        if (s.done != null) yield return new WaitForSecondsRealtime(0.6f);
    }

    // =====================================================================
    //  Dịch chuyển + dummy
    // =====================================================================
    private IEnumerator TeleportToCenterRoutine()
    {
        yield return Fade(0f, 1f, 0.25f);
        dummyDamaged = false;
        Vector2 center = GetMapCenter();
        MovePlayer(center);
        SpawnDummy(center + dummyOffset);
        yield return null;
        yield return Fade(1f, 0f, 0.25f);
    }

    private Vector2 GetMapCenter()
    {
        GameObject c = FindByName(mapCenterObjectName);
        if (c != null) return c.transform.position;
        GameObject a = FindByName("Nexus_Blue"), b = FindByName(enemyNexusObjectName);
        if (a != null && b != null) return (a.transform.position + b.transform.position) * 0.5f;
        return Vector2.zero;
    }

    private void MovePlayer(Vector2 pos)
    {
        if (blade == null) return;
        Vector3 old = blade.transform.position;
        Vector3 target = new Vector3(pos.x, pos.y, old.z);
        Rigidbody2D rb = blade.GetComponent<Rigidbody2D>();
        blade.transform.position = target;
        if (rb != null) { rb.position = pos; rb.linearVelocity = Vector2.zero; }
        Physics2D.SyncTransforms();
        CinemachineCamera cm = FindFirstObjectByType<CinemachineCamera>();
        if (cm != null) cm.OnTargetObjectWarped(blade.transform, target - old);
    }

    private void SpawnDummy(Vector2 pos)
    {
        if (dummy != null) Destroy(dummy);
        if (dummyPrefab == null) return;
        dummy = Instantiate(dummyPrefab, new Vector3(pos.x, pos.y, blade != null ? blade.transform.position.z : 0f), Quaternion.identity);
        dummy.name = "TutorialDummy";
        ConfigureDummy(dummy);
        TeamMember tm = dummy.GetComponentInChildren<TeamMember>();
        dummyStartHeart = tm != null ? tm.CurrentHeart : 0f;
    }

    // Dummy: đứng yên, thuộc đội đỏ (đối địch với người chơi), không nhận input người chơi.
    private void ConfigureDummy(GameObject d)
    {
        foreach (MonoBehaviour mb in d.GetComponentsInChildren<MonoBehaviour>(true))
        {
            if (mb == null) continue;
            Type type = mb.GetType();
            MethodInfo setInput = type.GetMethod("SetPlayerInput", BindingFlags.Instance | BindingFlags.Public, null, new[] { typeof(bool) }, null);
            if (setInput != null) setInput.Invoke(mb, new object[] { false });
            if (mb is PlayerMovement) { mb.enabled = false; continue; }
            if (IsAiLike(type.Name)) mb.enabled = false;
        }
        foreach (Rigidbody2D rb in d.GetComponentsInChildren<Rigidbody2D>(true))
        {
            rb.linearVelocity = Vector2.zero;
            rb.constraints = RigidbodyConstraints2D.FreezeAll;
        }
        foreach (TeamMember tm in d.GetComponentsInChildren<TeamMember>(true)) SetTeam(tm, 2); // 2 = Red (theo ARAM.unity)
    }

    private bool IsAiLike(string typeName)
    {
        if (typeName == "TeamMember" || typeName == "Stat" || typeName == "HealthBar") return false;
        foreach (string k in dummyDisableKeywords)
            if (!string.IsNullOrEmpty(k) && typeName.IndexOf(k, StringComparison.OrdinalIgnoreCase) >= 0) return true;
        // Tên kết thúc/bắt đầu bằng "AI" (ví dụ ChampionAI, AIController)
        return typeName.EndsWith("AI") || typeName.StartsWith("AI");
    }

    private static void SetTeam(TeamMember tm, int teamValue)
    {
        const BindingFlags f = BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic;
        Type t = tm.GetType();
        FieldInfo field = t.GetField("team", f);
        if (field != null) { field.SetValue(tm, Enum.ToObject(field.FieldType, teamValue)); return; }
        PropertyInfo prop = t.GetProperty("team", f) ?? t.GetProperty("Team", f);
        if (prop != null && prop.CanWrite) prop.SetValue(tm, Enum.ToObject(prop.PropertyType, teamValue));
    }

    // Nếu dummy chết/biến mất thì tạo lại cạnh người chơi để bước sau vẫn có mục tiêu.
    private void EnsureDummy()
    {
        TeamMember tm = dummy != null ? dummy.GetComponentInChildren<TeamMember>() : null;
        if (dummy == null || !dummy.activeInHierarchy || tm == null || tm.CurrentHeart <= 0f)
        {
            dummyDamaged = true; // dummy đã bị hạ => chắc chắn đã trúng đòn
            if (blade != null) SpawnDummy((Vector2)blade.transform.position + dummyOffset);
        }
    }

    // Giữ dummy trong tầm với khi người chơi đã lướt đi xa.
    private void NearDummy()
    {
        if (dummy == null || blade == null) return;
        if (Vector2.Distance(dummy.transform.position, blade.transform.position) > 3.5f && !(bladeScript != null && bladeScript.IsUsingSkill))
            dummy.transform.position = blade.transform.position + (Vector3)dummyOffset;
    }

    private bool DummyWasHit()
    {
        TeamMember tm = dummy != null ? dummy.GetComponentInChildren<TeamMember>() : null;
        if (tm != null && tm.CurrentHeart < dummyStartHeart - 0.01f) dummyDamaged = true;
        return dummyDamaged;
    }

    private bool NexusFinished()
    {
        if (nexusStructure == null || nexusStructure.isDestroyed) return true;
        GameObject end = FindByName(endGamePanelName);
        return end != null && end.activeInHierarchy;
    }

    // =====================================================================
    //  UI
    // =====================================================================
    private void BuildUi()
    {
        TMP_Text existing = FindFirstObjectByType<TMP_Text>(FindObjectsInactive.Include);
        font = existing != null ? existing.font : null;

        GameObject canvasGo = new GameObject("TutorialCanvas", typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
        canvasGo.transform.SetParent(transform, false);
        Canvas canvas = canvasGo.GetComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 500;
        CanvasScaler scaler = canvasGo.GetComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920f, 1080f);
        scaler.matchWidthOrHeight = 0.5f;
        canvasRt = canvasGo.GetComponent<RectTransform>();

        dim = MakeImage("Dim", canvasRt, new Color(0f, 0f, 0f, 0f));
        Stretch(dim.rectTransform, 0, 0, 0, 0);

        highlightImg = MakeImage("Highlight", canvasRt, new Color(1f, 0.78f, 0.25f, 0.12f));
        highlightImg.raycastTarget = false;
        highlightRt = highlightImg.rectTransform;
        highlightRt.anchorMin = highlightRt.anchorMax = highlightRt.pivot = new Vector2(0.5f, 0.5f);
        Outline hl = highlightImg.gameObject.AddComponent<Outline>();
        hl.effectColor = Gold;
        hl.effectDistance = new Vector2(4f, -4f);
        highlightImg.gameObject.SetActive(false);

        Image panelImg = MakeImage("TutorialPanel", canvasRt, PanelBg);
        panel = panelImg.rectTransform;
        Outline border = panelImg.gameObject.AddComponent<Outline>();
        border.effectColor = Gold;
        border.effectDistance = new Vector2(3f, -3f);

        stepText = MakeText("Step", panel, 14, 24, FontStyles.Normal, new Color(1f, 1f, 1f, 0.6f), TextAlignmentOptions.TopLeft);
        Stretch(stepText.rectTransform, 24, 0, 24, 14);
        stepText.rectTransform.anchorMin = new Vector2(0f, 1f);
        stepText.rectTransform.offsetMin = new Vector2(24f, -44f);

        titleText = MakeText("Title", panel, 20, 44, FontStyles.Bold, Gold, TextAlignmentOptions.Left);
        titleText.rectTransform.anchorMin = new Vector2(0f, 1f);
        titleText.rectTransform.anchorMax = new Vector2(1f, 1f);
        titleText.rectTransform.offsetMin = new Vector2(24f, -112f);
        titleText.rectTransform.offsetMax = new Vector2(-24f, -44f);

        bodyText = MakeText("Body", panel, 16, 30, FontStyles.Normal, Color.white, TextAlignmentOptions.TopLeft);
        Stretch(bodyText.rectTransform, 24, 96, 24, 120);
        bodyGroup = bodyText.gameObject.AddComponent<CanvasGroup>();

        hintText = MakeText("Hint", panel, 14, 26, FontStyles.Normal, Color.white, TextAlignmentOptions.TopLeft);
        Stretch(hintText.rectTransform, 24, 16, 24, 120);
        hintGroup = hintText.gameObject.AddComponent<CanvasGroup>();

        Image okImg = MakeImage("OkButton", panel, Gold);
        okButton = okImg.gameObject.AddComponent<Button>();
        okButton.targetGraphic = okImg;
        okButton.onClick.AddListener(() => okClicked = true);
        RectTransform okRt = okImg.rectTransform;
        okRt.anchorMin = okRt.anchorMax = new Vector2(0.5f, 0f);
        okRt.pivot = new Vector2(0.5f, 0f);
        okRt.sizeDelta = new Vector2(220f, 60f);
        okRt.anchoredPosition = new Vector2(0f, 24f);
        TextMeshProUGUI okLabel = MakeText("Label", okRt, 20, 30, FontStyles.Bold, new Color(0.1f, 0.1f, 0.1f, 1f), TextAlignmentOptions.Center);
        Stretch(okLabel.rectTransform, 0, 0, 0, 0);
        okLabel.text = "OK";
        okGroup = okImg.gameObject.AddComponent<CanvasGroup>();

        TextMeshProUGUI escHint = MakeText("EscHint", canvasRt, 16, 22, FontStyles.Normal, new Color(1f, 1f, 1f, 0.75f), TextAlignmentOptions.BottomLeft);
        escHint.text = "ESC: thoát về menu";
        escHint.rectTransform.anchorMin = new Vector2(0f, 0f);
        escHint.rectTransform.anchorMax = new Vector2(0f, 0f);
        escHint.rectTransform.pivot = new Vector2(0f, 0f);
        escHint.rectTransform.sizeDelta = new Vector2(320f, 36f);
        escHint.rectTransform.anchoredPosition = new Vector2(20f, 16f);

        fade = MakeImage("Fade", canvasRt, new Color(0f, 0f, 0f, 0f));
        Stretch(fade.rectTransform, 0, 0, 0, 0);
        fade.raycastTarget = false;

        ApplyPanel(0f);
        panel.gameObject.SetActive(false);
    }

    private IEnumerator AnimatePanel(float from, float to)
    {
        float t = 0f;
        while (t < animSeconds)
        {
            t += Time.unscaledDeltaTime;
            float k = Mathf.SmoothStep(0f, 1f, Mathf.Clamp01(t / animSeconds));
            ApplyPanel(Mathf.Lerp(from, to, k));
            yield return null;
        }
        ApplyPanel(to);
    }

    // t = 0: thu nhỏ bên phải (dock). t = 1: to giữa màn hình.
    private void ApplyPanel(float t)
    {
        panelT = t;
        Vector2 aMin = Vector2.Lerp(new Vector2(1f, 0.5f), new Vector2(0.5f, 0.5f), t);
        panel.anchorMin = panel.anchorMax = aMin;
        panel.pivot = aMin;
        panel.anchoredPosition = Vector2.Lerp(dockOffset, Vector2.zero, t);
        panel.sizeDelta = Vector2.Lerp(dockSize, centerSize, t);

        dim.color = new Color(0f, 0f, 0f, 0.6f * t);
        dim.raycastTarget = t > 0.5f;
        bodyGroup.alpha = Mathf.Clamp01((t - 0.3f) / 0.7f);
        hintGroup.alpha = Mathf.Clamp01((0.7f - t) / 0.7f);
        okGroup.alpha = Mathf.Clamp01((t - 0.5f) / 0.5f);
        okGroup.interactable = okGroup.blocksRaycasts = t > 0.9f;
    }

    private IEnumerator Fade(float from, float to, float seconds)
    {
        float t = 0f;
        while (t < seconds)
        {
            t += Time.unscaledDeltaTime;
            fade.color = new Color(0f, 0f, 0f, Mathf.Lerp(from, to, Mathf.Clamp01(t / seconds)));
            yield return null;
        }
        fade.color = new Color(0f, 0f, 0f, to);
    }

    // Khung vàng nhấp nháy quanh phần UI cần chú ý (skill, minimap, shop...).
    private void UpdateHighlight()
    {
        if (highlightImg == null) return;
        if (highlightNames == null || highlightNames.Length == 0) { highlightImg.gameObject.SetActive(false); return; }
        if (highlightTarget == null || !highlightTarget.gameObject.activeInHierarchy)
        {
            highlightTarget = null;
            foreach (string n in highlightNames)
            {
                foreach (RectTransform rt in FindObjectsByType<RectTransform>(FindObjectsInactive.Exclude, FindObjectsSortMode.None))
                    if (rt.name == n && rt.GetComponentInParent<Canvas>() != null && rt.GetComponentInParent<Canvas>().gameObject != canvasRt.gameObject)
                    { highlightTarget = rt; break; }
                if (highlightTarget != null) break;
            }
        }
        if (highlightTarget == null) { highlightImg.gameObject.SetActive(false); return; }

        Canvas targetCanvas = highlightTarget.GetComponentInParent<Canvas>().rootCanvas;
        Camera cam = targetCanvas.renderMode == RenderMode.ScreenSpaceOverlay ? null : (targetCanvas.worldCamera != null ? targetCanvas.worldCamera : Camera.main);
        Vector3[] corners = new Vector3[4];
        highlightTarget.GetWorldCorners(corners);
        Vector2 min = Vector2.positiveInfinity, max = Vector2.negativeInfinity;
        for (int i = 0; i < 4; i++)
        {
            Vector2 sp = RectTransformUtility.WorldToScreenPoint(cam, corners[i]);
            RectTransformUtility.ScreenPointToLocalPointInRectangle(canvasRt, sp, null, out Vector2 local);
            min = Vector2.Min(min, local);
            max = Vector2.Max(max, local);
        }
        const float pad = 8f;
        highlightRt.anchoredPosition = (min + max) * 0.5f;
        highlightRt.sizeDelta = (max - min) + new Vector2(pad * 2f, pad * 2f);
        float pulse = 0.5f + 0.5f * Mathf.Sin(Time.unscaledTime * 5f);
        highlightImg.color = new Color(1f, 0.78f, 0.25f, 0.06f + 0.14f * pulse);
        highlightImg.gameObject.SetActive(true);
    }

    // =====================================================================
    //  Helper
    // =====================================================================
    private static GameObject FindByName(string objectName)
    {
        foreach (Transform t in FindObjectsByType<Transform>(FindObjectsInactive.Include, FindObjectsSortMode.None))
            if (t.name == objectName) return t.gameObject;
        return null;
    }

    private static Image MakeImage(string name, Transform parent, Color color)
    {
        GameObject go = new GameObject(name, typeof(RectTransform), typeof(Image));
        go.transform.SetParent(parent, false);
        Image img = go.GetComponent<Image>();
        img.color = color;
        return img;
    }

    private TextMeshProUGUI MakeText(string name, Transform parent, float minSize, float maxSize, FontStyles style, Color color, TextAlignmentOptions align)
    {
        GameObject go = new GameObject(name, typeof(RectTransform));
        go.transform.SetParent(parent, false);
        TextMeshProUGUI t = go.AddComponent<TextMeshProUGUI>();
        if (font != null) t.font = font;
        t.enableAutoSizing = true;
        t.fontSizeMin = minSize;
        t.fontSizeMax = maxSize;
        t.fontStyle = style;
        t.color = color;
        t.alignment = align;
        t.raycastTarget = false;
        return t;
    }

    private static void Stretch(RectTransform rt, float left, float bottom, float right, float top)
    {
        rt.anchorMin = Vector2.zero;
        rt.anchorMax = Vector2.one;
        rt.offsetMin = new Vector2(left, bottom);
        rt.offsetMax = new Vector2(-right, -top);
    }
}
