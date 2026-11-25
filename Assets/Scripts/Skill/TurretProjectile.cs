
using UnityEngine;

public class TurretProjectile : MonoBehaviour
{
    [SerializeField] private float speed = 10f;
    [SerializeField] private float lifeTime = 3f;
    [SerializeField] private float hitDistance = 0.2f;

    private Team ownerTeam;
    private Component target;
    private float damage;

    public void Init(Team team, Component enemy, float amount)
    {
        ownerTeam = team;
        target = enemy;
        damage = amount;

        Destroy(gameObject, lifeTime);
    }

    void Update()
    {
        if (target == null || !target.gameObject.activeInHierarchy)
        {
            Destroy(gameObject);
            return;
        }

        Minion minion = target as Minion;
        TeamMember member = target as TeamMember;

        // Khong ban dong doi hoac muc tieu da chet
        if (minion != null)
        {
            if (!minion.isActiveAndEnabled ||
                minion.Team == ownerTeam)
            {
                Destroy(gameObject);
                return;
            }
        }
        else if (member != null)
        {
            if (!member.isActiveAndEnabled ||
                member.CurrentHeart <= 0f ||
                member.team == ownerTeam)
            {
                Destroy(gameObject);
                return;
            }
        }

        Vector2 aimPoint = GetAimPoint(target.transform);
        Vector2 position = transform.position;
        Vector2 direction = aimPoint - position;

        if (direction.magnitude <= hitDistance)
        {
            HitTarget();
            return;
        }

        Vector2 next = Vector2.MoveTowards(
            position,
            aimPoint,
            speed * Time.deltaTime
        );

        transform.position = next;

        if (direction.sqrMagnitude > 0.001f)
            transform.right = direction.normalized;

        if (Vector2.Distance(next, aimPoint) <= hitDistance)
            HitTarget();
    }

    Vector2 GetAimPoint(Transform root)
    {
        Collider2D[] colliders =
            root.GetComponentsInChildren<Collider2D>();

        foreach (Collider2D c in colliders)
        {
            if (c.enabled && !c.isTrigger)
                return c.bounds.center;
        }

        return root.position;
    }

    void HitTarget()
    {
        Minion minion = target as Minion;
        TeamMember member = target as TeamMember;

        if (minion != null &&
            minion.isActiveAndEnabled &&
            minion.Team != ownerTeam)
        {
            minion.TakeDamage(damage);
        }
        else if (member != null &&
                 member.isActiveAndEnabled &&
                 member.CurrentHeart > 0f &&
                 member.team != ownerTeam)
        {
            member.TakeDamage(damage);
        }

        Destroy(gameObject);
    }
}
