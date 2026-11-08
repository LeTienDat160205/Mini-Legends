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
    private MeshRenderer rangeRenderer;
    private Mesh rangeMesh;

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
        hitboxRenderer.sortingOrder = 100;
        hitboxRenderer.sortingLayerName = "Character";

        Shader shader = Shader.Find("Sprites/Default");
        if (shader != null)
        {
            hitboxRenderer.material = new Material(shader);
            hitboxRenderer.material.color = new Color(0.75f, 0.85f, 1f, 0.8f);
        }

        CreateFilledRange(shader);

        // Luôn hiển thị vùng đánh trong giai đoạn test.
        hitboxRenderer.enabled = false;
        SetPreviewColor(new Color(0.35f, 0.7f, 1f, 0.8f));
    }

    private void CreateFilledRange(Shader shader)
    {
        MeshFilter meshFilter = GetComponent<MeshFilter>();
        if (meshFilter == null) meshFilter = gameObject.AddComponent<MeshFilter>();
        rangeRenderer = GetComponent<MeshRenderer>();
        if (rangeRenderer == null) rangeRenderer = gameObject.AddComponent<MeshRenderer>();

        rangeMesh = new Mesh { name = "AttackRangePreview" };
        meshFilter.sharedMesh = rangeMesh;
        rangeRenderer.sortingOrder = 99;
        rangeRenderer.sortingLayerName = "Character";
        if (shader != null)
        {
            rangeRenderer.material = new Material(shader);
            rangeRenderer.material.color = new Color(0.55f, 0.75f, 1f, 0.22f);
        }
        UpdateRangeMesh();

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

        UpdateRangeMesh();

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

    private void UpdateRangeMesh()
    {
        if (rangeMesh == null) return;
        float halfWidth = attackRange * 0.4f;
        rangeMesh.Clear();
        rangeMesh.vertices = new[]
        {
            new Vector3(0f, 0f, -0.04f),
            new Vector3(-halfWidth, attackRange, -0.04f),
            new Vector3(halfWidth, attackRange, -0.04f)
        };
        rangeMesh.triangles = new[] { 0, 1, 2 };
        rangeMesh.RecalculateBounds();
    }

    private void OnDrawGizmos()
    {
        float range = Mathf.Max(0.1f, attackRange);
        float halfWidth = range * 0.4f;
        Vector3 left = transform.TransformPoint(new Vector3(-halfWidth, range, 0f));
        Vector3 right = transform.TransformPoint(new Vector3(halfWidth, range, 0f));
        Vector3 origin = transform.position;
        Gizmos.color = new Color(0.35f, 0.7f, 1f, 0.35f);
        Gizmos.DrawLine(origin, left);
        Gizmos.DrawLine(left, right);
        Gizmos.DrawLine(right, origin);
    }

    public void EnableHitbox()
    {
        hitTargets.Clear();
        if (hitbox != null) hitbox.enabled = true;
        if (hitboxRenderer != null) hitboxRenderer.enabled = false;
        SetPreviewColor(hitboxColor);
    }

    public void ShowAttackRange()
    {
        if (hitboxRenderer != null) hitboxRenderer.enabled = false;
    }

    public void DisableHitbox()
    {
        if (hitbox != null) hitbox.enabled = false;
        SetPreviewColor(new Color(0.35f, 0.7f, 1f, 0.8f));
    }

    private void SetPreviewColor(Color color)
    {
        if (hitboxRenderer != null && hitboxRenderer.material != null)
            hitboxRenderer.material.color = color;
        if (rangeRenderer != null && rangeRenderer.material != null)
            rangeRenderer.material.color = new Color(color.r, color.g, color.b, 0.28f);
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (!enabled || !other.CompareTag("Enemy")) return;
        if (!hitTargets.Add(other.gameObject)) return;

        // Tạm thời gọi theo quy ước Enemy.TakeDamage(float); có thể thay bằng IDamageable sau.
        other.SendMessageUpwards("TakeDamage", 1f, SendMessageOptions.DontRequireReceiver);
    }
}
