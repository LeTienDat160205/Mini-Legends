using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class HealthBar : MonoBehaviour
{
    [Header("UI References")]
    public Image fillBar;
    public TextMeshProUGUI valueText;

    [Header("Bar Sprites")]
    [SerializeField] private Sprite blueBarSprite; // Để trống nếu muốn dùng sprite gốc của FillBar (xanh)
    [SerializeField] private Sprite redbarSprite;  // Kéo Redbar vào đây

    [Header("Target & Offset")]
    [SerializeField] private Transform target;
    [SerializeField] private Vector3 worldOffset = new Vector3(0f, 2f, 0f);

    [Header("Team Colors (Fallback)")]
    [SerializeField] private Color controlledPlayerColor = Color.green;
    [SerializeField] private Color allyColor = Color.blue;
    [SerializeField] private Color enemyColor = Color.red;

    [Header("Debug")]
    [SerializeField] private bool debugLog = false;   // Bật để xem thanh máu chọn nhánh nào trong Console

    private RectTransform rectTransform;
    private Canvas canvas;
    private TeamMember ownerTeamMember;
    private Minion ownerMinion;
    private MinimapEntity cachedMinimapEntity;
    private bool colorInitialized = false;
    private bool lastWasLocalPlayer = false;
    private Team lastPlayerTeam;

    // Sprite gốc của FillBar (xanh), lưu lại để trả về khi là đồng minh
    private Sprite defaultFillSprite;

    private void Awake()
    {
        rectTransform = transform as RectTransform;
        canvas = GetComponentInParent<Canvas>();

        if (fillBar != null)
            defaultFillSprite = fillBar.sprite;

        FindOwnerComponents();
    }

    private void Start()
    {
        SetupHealthBarVisual();
    }

    private void FindOwnerComponents()
    {
        // Tự động tìm ngược lên các object cha cho đến khi tìm thấy component cần thiết hoặc chạm gốc
        Transform current = transform;
        while (current != null)
        {
            if (ownerMinion == null) ownerMinion = current.GetComponent<Minion>();
            if (ownerTeamMember == null) ownerTeamMember = current.GetComponent<TeamMember>();

            if (ownerMinion != null || ownerTeamMember != null)
            {
                if (target == null) target = current;
                break;
            }
            current = current.parent;
        }

        if (target == null && transform.parent != null)
            target = transform.parent;
    }

    // Tìm component trên target, trên cha của target, hoặc trên con của target.
    // Không dùng toán tử "??" với GetComponent vì trong Editor nó có thể không coi "không tìm thấy" là null.
    private static T FindOnTarget<T>(Transform t) where T : Component
    {
        if (t == null) return null;

        T found = t.GetComponentInParent<T>();
        if (found == null) found = t.GetComponentInChildren<T>(true);
        return found;
    }

    private void LateUpdate()
    {
        // Tự cập nhật lại màu thanh máu khi trạng thái "người chơi đang điều khiển"
        // hoặc team của người chơi thay đổi sau lúc khởi tạo (ví dụ do script khác gán muộn).
        bool nowLocal = cachedMinimapEntity != null && cachedMinimapEntity.isLocalPlayer;
        if (!colorInitialized ||
            nowLocal != lastWasLocalPlayer ||
            lastPlayerTeam != MinimapManager.LocalPlayerTeam)
        {
            SetupHealthBarVisual();
        }

        if (target == null || canvas == null || rectTransform == null)
            return;

        if (canvas.renderMode == RenderMode.WorldSpace)
            return;

        Camera worldCamera = Camera.main;
        if (worldCamera == null)
            return;

        Vector3 screenPoint = worldCamera.WorldToScreenPoint(
            target.position + worldOffset
        );

        if (screenPoint.z <= 0f)
            return;

        RectTransform canvasRect = canvas.transform as RectTransform;
        Camera uiCamera = canvas.renderMode == RenderMode.ScreenSpaceOverlay
            ? null
            : canvas.worldCamera;

        if (canvasRect != null &&
            RectTransformUtility.ScreenPointToLocalPointInRectangle(
                canvasRect,
                screenPoint,
                uiCamera,
                out Vector2 localPoint))
        {
            rectTransform.anchoredPosition = localPoint;
        }
    }

    public void SetTarget(Transform followTarget)
    {
        target = followTarget;
        FindOwnerComponents();
        SetupHealthBarVisual();
    }

    // Sprite dùng cho đồng minh / người chơi: ưu tiên blueBarSprite, nếu trống thì dùng sprite gốc của FillBar
    private Sprite GetAllySprite()
    {
        return blueBarSprite != null ? blueBarSprite : defaultFillSprite;
    }

    private void SetupHealthBarVisual()
    {
        if (fillBar == null)
            return;

        FindOwnerComponents();

        Team playerTeam = MinimapManager.LocalPlayerTeam;
        lastPlayerTeam = playerTeam;

        // 1. Kiểm tra nếu là nhân vật người chơi chính đang điều khiển -> Xanh lá
        cachedMinimapEntity = FindOnTarget<MinimapEntity>(target != null ? target : transform);
        bool isLocal = cachedMinimapEntity != null && cachedMinimapEntity.isLocalPlayer;
        lastWasLocalPlayer = isLocal;

        if (isLocal)
        {
            // Reset sprite về xanh gốc để không bị dính sprite đỏ từ trước
            Sprite allySprite = GetAllySprite();
            if (allySprite != null) fillBar.sprite = allySprite;

            fillBar.color = controlledPlayerColor;
            colorInitialized = true;
            if (debugLog) Debug.Log($"[HealthBar] {name}: LOCAL PLAYER -> xanh lá", this);
            return;
        }

        bool isEnemy = false;

        // 2. ƯU TIÊN SỐ 1: Kiểm tra trực tiếp qua biến Team trên script Minion hoặc TeamMember
        if (ownerMinion != null)
        {
            // Nếu Team của con sói khác với Team của Player -> Chắc chắn là Địch
            isEnemy = (ownerMinion.Team != playerTeam);
        }
        else if (ownerTeamMember != null)
        {
            isEnemy = (ownerTeamMember.team != playerTeam);
        }
        else
        {
            // Kiểm tra qua Nexus nếu có
            Nexus nexus = FindOnTarget<Nexus>(target != null ? target : transform);
            if (nexus != null)
            {
                isEnemy = (nexus.NexusTeam != playerTeam);
            }
            else
            {
                // Dùng tên làm phương án dự phòng cuối cùng nếu không tìm thấy script Minion/TeamMember
                string fullHierarchyName = "";
                Transform t = target != null ? target : transform;
                while (t != null)
                {
                    fullHierarchyName += " " + t.name.ToLower();
                    t = t.parent;
                }

                if (fullHierarchyName.Contains("red")) isEnemy = true;
                else if (fullHierarchyName.Contains("blue")) isEnemy = false;
            }
        }

        // 3. Áp dụng Sprite tương ứng (Địch = Redbar, Đồng minh = Bluebar hoặc sprite gốc của FillBar)
        if (isEnemy)
        {
            if (redbarSprite != null) fillBar.sprite = redbarSprite;
            fillBar.color = Color.white;
        }
        else
        {
            Sprite allySprite = GetAllySprite();
            if (allySprite != null) fillBar.sprite = allySprite;
            fillBar.color = Color.white;
        }

        if (debugLog)
            Debug.Log($"[HealthBar] {name}: không phải local player (minimapEntity={(cachedMinimapEntity != null ? "có" : "KHÔNG CÓ")}) -> {(isEnemy ? "địch" : "đồng minh")}", this);

        colorInitialized = true;
    }

    public void UpdateBar(float currentValue, float maxValue)
    {
        SetupHealthBarVisual();

        if (fillBar != null)
        {
            fillBar.fillAmount = maxValue > 0f
                ? Mathf.Clamp01(currentValue / maxValue)
                : 0f;
        }

        if (valueText != null)
        {
            valueText.text =
                $"{Mathf.CeilToInt(currentValue)} / {Mathf.CeilToInt(maxValue)}";
        }
    }
}
