using UnityEngine;
using UnityEngine.UI;

public class Teleport : MonoBehaviour
{
    [SerializeField] private TeamMember teamMember;
    [SerializeField] private Rigidbody2D body;
    [SerializeField] private Slider teleportProcessBar;
    [SerializeField] private float castDuration = 5f;
    [SerializeField] private float barOffsetBelowFeet = 0.35f;
    private Coroutine castRoutine;
    private Vector3 castStartPosition;

    private void Awake()
    {
        if (teamMember == null) teamMember = GetComponent<TeamMember>();
        if (body == null) body = GetComponent<Rigidbody2D>();
        if (teleportProcessBar == null)
        {
            foreach (Slider slider in GetComponentsInChildren<Slider>(true))
                if (slider.name == "TeleportProcessBar") { teleportProcessBar = slider; break; }
        }
        if (teleportProcessBar == null)
        {
            foreach (Slider slider in FindObjectsByType<Slider>(FindObjectsInactive.Include, FindObjectsSortMode.None))
                if (slider.name == "TeleportProcessBar") { teleportProcessBar = slider; break; }
        }
        ResetBar();
    }

    private void OnEnable()
    {
        if (teamMember == null) teamMember = GetComponent<TeamMember>();
        if (teamMember != null) teamMember.Damaged += OnDamaged;
    }

    private void OnDisable()
    {
        if (teamMember != null) teamMember.Damaged -= OnDamaged;
        CancelTeleport();
    }

    private void Update()
    {
        if (castRoutine != null && (Input.GetAxisRaw("Horizontal") != 0f ||
            Input.GetAxisRaw("Vertical") != 0f ||
            (transform.position - castStartPosition).sqrMagnitude > 0.0001f))
            CancelTeleport();

        if (HotkeysSettings.Pressed(HotkeysSettings.Recall))
            TeleportToSpawn();

    }

    private void LateUpdate()
    {
        if (teleportProcessBar == null) return;
        Canvas canvas = teleportProcessBar.GetComponentInParent<Canvas>();
        if (canvas == null) return;

        Collider2D collider = GetComponent<Collider2D>();
        SpriteRenderer sprite = GetComponent<SpriteRenderer>();
        float feetY = collider != null ? collider.bounds.min.y :
            sprite != null ? sprite.bounds.min.y : transform.position.y;
        Vector3 worldPoint = new Vector3(transform.position.x, feetY - barOffsetBelowFeet, transform.position.z);

        if (canvas.renderMode == RenderMode.WorldSpace)
        {
            teleportProcessBar.transform.position = worldPoint;
            return;
        }

        Camera camera = Camera.main;
        RectTransform barRect = teleportProcessBar.transform as RectTransform;
        RectTransform canvasRect = canvas.transform as RectTransform;
        if (camera == null || barRect == null || canvasRect == null) return;
        Vector3 screenPoint = camera.WorldToScreenPoint(worldPoint);
        if (screenPoint.z <= 0f) return;
        Camera uiCamera = canvas.renderMode == RenderMode.ScreenSpaceOverlay ? null : canvas.worldCamera;
        if (RectTransformUtility.ScreenPointToLocalPointInRectangle(canvasRect, screenPoint, uiCamera, out Vector2 point))
            barRect.position = canvasRect.TransformPoint(point);
    }

    public void TeleportToSpawn()
    {
        if (castRoutine != null || teamMember == null || teamMember.team == Team.Neutral || teamMember.CurrentHeart <= 0f) return;

        SpawnArea spawn = SpawnArea.FindFor(teamMember.team);
        if (spawn != null) { castStartPosition = transform.position; castRoutine = StartCoroutine(CastTeleport(spawn)); return; }

        Debug.LogWarning($"Không tìm thấy SpawnArea cho team {teamMember.team}.", this);
    }

    private System.Collections.IEnumerator CastTeleport(SpawnArea spawn)
    {
        float elapsed = 0f;
        if (teleportProcessBar != null)
        {
            teleportProcessBar.minValue = 0f;
            teleportProcessBar.maxValue = 1f;
            teleportProcessBar.value = 0f;
            teleportProcessBar.gameObject.SetActive(true);
        }
        while (elapsed < castDuration)
        {
            elapsed += Time.deltaTime;
            if (teleportProcessBar != null)
                teleportProcessBar.value = Mathf.Clamp01(elapsed / Mathf.Max(0.01f, castDuration));
            yield return null;
        }
        castRoutine = null;
        ResetBar();
        if (spawn != null) spawn.PlaceFeet(transform);
    }

    private void OnDamaged(float damage) => CancelTeleport();

    private void CancelTeleport()
    {
        if (castRoutine != null) StopCoroutine(castRoutine);
        castRoutine = null;
        ResetBar();
    }

    private void ResetBar()
    {
        if (teleportProcessBar == null) return;
        teleportProcessBar.value = 0f;
        teleportProcessBar.gameObject.SetActive(false);
    }
}
