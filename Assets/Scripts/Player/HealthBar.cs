using UnityEngine;
using UnityEngine.UI;
using TMPro;
public class HealthBar : MonoBehaviour
{
    public Image fillBar;
    public TextMeshProUGUI valueText;
    [SerializeField] private Transform target;
    [SerializeField] private Vector3 worldOffset = new Vector3(0f, 2f, 0f);

    private RectTransform rectTransform;
    private Canvas canvas;

    private void Awake()
    {
        rectTransform = transform as RectTransform;
        canvas = GetComponentInParent<Canvas>();
        if (target == null)
        {
            TeamMember owner = GetComponentInParent<TeamMember>();
            if (owner != null) target = owner.transform;
        }
    }

    private void LateUpdate()
    {
        if (target == null || canvas == null || rectTransform == null) return;
        if (canvas.renderMode == RenderMode.WorldSpace) return;

        Camera worldCamera = Camera.main;
        if (worldCamera == null) return;
        Vector3 screenPoint = worldCamera.WorldToScreenPoint(target.position + worldOffset);
        if (screenPoint.z <= 0f) return;

        RectTransform canvasRect = canvas.transform as RectTransform;
        Camera uiCamera = canvas.renderMode == RenderMode.ScreenSpaceOverlay ? null : canvas.worldCamera;
        if (canvasRect != null && RectTransformUtility.ScreenPointToLocalPointInRectangle(
                canvasRect, screenPoint, uiCamera, out Vector2 localPoint))
            rectTransform.anchoredPosition = localPoint;
    }

    public void SetTarget(Transform followTarget) => target = followTarget;

    public void UpdateBar(float currentValue, float maxValue)
    {
        if (fillBar != null)
            fillBar.fillAmount = maxValue > 0f ? Mathf.Clamp01(currentValue / maxValue) : 0f;
        if (valueText != null)
            valueText.text = $"{Mathf.CeilToInt(currentValue)} / {Mathf.CeilToInt(maxValue)}";
    }
}
