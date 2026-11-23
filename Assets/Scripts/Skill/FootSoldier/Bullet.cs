using UnityEngine;

/// <summary>
/// Viên đạn dùng chung cho đánh thường, E (đạn nhỏ) và Q (đạn to).
/// Prefab cần: Collider2D (Is Trigger) + Rigidbody2D (Kinematic).
/// Đạn to của Q chỉ là prefab khác (scale lớn hơn) dùng cùng script này.
/// </summary>
public class Bullet : MonoBehaviour
{
    [SerializeField] private float speed = 10f;
    [SerializeField] private float lifeTime = 3f;
    [SerializeField] private bool destroyOnHit = true;

    private float damage;
    private TeamMember owner;
    private Transform target;           // null = bay thẳng theo direction
    private Collider2D targetCollider;  // Collider của mục tiêu, dùng làm điểm nhắm
    private Vector2 direction;

    // Điểm nhắm = tâm collider của mục tiêu. Không có collider thì dùng gốc object.
    public static Vector2 GetAimPoint(Transform t)
    {
        if (t == null) return Vector2.zero;

        Collider2D c = t.GetComponentInChildren<Collider2D>();
        return c != null ? (Vector2)c.bounds.center : (Vector2)t.position;
    }

    public void Init(TeamMember owner, float damage, Vector2 direction, Transform target = null)
    {
        this.owner = owner;
        this.damage = damage;
        this.direction = direction.sqrMagnitude > 0.0001f ? direction.normalized : Vector2.right;
        this.target = target;
        targetCollider = target != null ? target.GetComponentInChildren<Collider2D>() : null;

        transform.right = this.direction;
        Destroy(gameObject, lifeTime);
    }

    private void Update()
    {
        // Nếu mục tiêu đã chết (bị Destroy) thì target == null -> bay tiếp theo hướng cuối cùng.
        if (target != null)
        {
            Vector2 aimPoint = targetCollider != null ? (Vector2)targetCollider.bounds.center : (Vector2)target.position;
            Vector2 toTarget = aimPoint - (Vector2)transform.position;
            if (toTarget.sqrMagnitude > 0.0001f)
                direction = toTarget.normalized;
        }

        transform.position += (Vector3)(direction * speed * Time.deltaTime);
        transform.right = direction;
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (owner == null) return;

        TeamMember hit = other.GetComponentInParent<TeamMember>();
        if (hit == null || hit == owner || !owner.IsEnemy(hit)) return;

        // Nếu đạn có mục tiêu cụ thể (đạn đuổi) thì chỉ trúng đúng mục tiêu đó.
        if (target != null && hit.transform != target && !hit.transform.IsChildOf(target)) return;

        hit.TakeDamage(damage, owner.gameObject);

        if (destroyOnHit)
            Destroy(gameObject);
    }
}
