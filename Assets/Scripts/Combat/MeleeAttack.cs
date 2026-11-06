using System.Collections.Generic;
using UnityEngine;

public class MeleeAttack : MonoBehaviour
{
    [SerializeField] private Collider2D hitbox;
    [SerializeField] private Color hitboxColor = new Color(1f, 0.1f, 0.1f, 0.55f);
    [SerializeField] private float hitboxLineWidth = 0.06f;
    [SerializeField] private float attackRange = 2f;
    private readonly HashSet<GameObject> hitTargets = new HashSet<GameObject>();
    private LineRenderer hitboxRenderer;

    private void Awake()
    {
        if (hitbox == null) hitbox = GetComponent<Collider2D>();
        CreateHitboxRenderer();
        DisableHitbox();
    }

    private void CreateHitboxRenderer()
    {
        hitboxRenderer = GetComponent<LineRenderer>();
        if (hitboxRenderer == null) hitboxRenderer = gameObject.AddComponent<LineRenderer>();

        hitboxRenderer.useWorldSpace = false;
        hitboxRenderer.loop = true;
        hitboxRenderer.positionCount = 3;
        hitboxRenderer.SetPosition(0, new Vector3(0f, 0f, -0.05f));
        UpdateRangeShape();
        hitboxRenderer.startWidth = hitboxLineWidth;
        hitboxRenderer.endWidth = hitboxLineWidth;
        hitboxRenderer.startColor = hitboxColor;
        hitboxRenderer.endColor = hitboxColor;
        hitboxRenderer.sortingOrder = 20;

        Shader shader = Shader.Find("Sprites/Default");
        if (shader != null)
            hitboxRenderer.material = new Material(shader);

        // Luôn hiển thị vùng đánh trong giai đoạn test.
        hitboxRenderer.enabled = true;
    }

    public void SetAttackRange(float range)
    {
        attackRange = Mathf.Max(0.1f, range);
        UpdateRangeShape();
    }

    private void UpdateRangeShape()
    {
        float halfWidth = attackRange * 0.4f;
        if (hitboxRenderer != null)
        {
            hitboxRenderer.SetPosition(0, new Vector3(0f, 0f, -0.05f));
            hitboxRenderer.SetPosition(1, new Vector3(-halfWidth, attackRange, -0.05f));
            hitboxRenderer.SetPosition(2, new Vector3(halfWidth, attackRange, -0.05f));
        }

        PolygonCollider2D polygon = hitbox as PolygonCollider2D;
        if (polygon != null)
        {
            polygon.points = new[]
            {
                Vector2.zero,
                new Vector2(-halfWidth, attackRange),
                new Vector2(halfWidth, attackRange)
            };
        }
    }

    public void EnableHitbox()
    {
        hitTargets.Clear();
        if (hitbox != null) hitbox.enabled = true;
        if (hitboxRenderer != null) hitboxRenderer.enabled = true;
    }

    public void ShowAttackRange()
    {
        if (hitboxRenderer != null) hitboxRenderer.enabled = true;
    }

    public void DisableHitbox()
    {
        if (hitbox != null) hitbox.enabled = false;
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (!enabled || !other.CompareTag("Enemy")) return;
        if (!hitTargets.Add(other.gameObject)) return;

        // Tạm thời gọi theo quy ước Enemy.TakeDamage(float); có thể thay bằng IDamageable sau.
        other.SendMessageUpwards("TakeDamage", 1f, SendMessageOptions.DontRequireReceiver);
    }
}
