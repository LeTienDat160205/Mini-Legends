
using UnityEngine;

public class CombatDrone : MonoBehaviour
{
    [Header("Thong tin tru")]
    [SerializeField] private Stat stat;
    [SerializeField] private TeamMember teamMember;
    [SerializeField] private Animator animator;

    [Header("Vung tan cong")]
    [SerializeField] private CircleCollider2D visibleShotArea;
    [SerializeField] private CircleCollider2D shotArea;
    [SerializeField] private SpriteRenderer shotAreaVisual;

    private float nextAttackTime;

    private readonly Collider2D[] hits = new Collider2D[64];
    private ContactFilter2D filter;

    void Awake()
    {
        if (stat == null)
            stat = GetComponent<Stat>();

        if (teamMember == null)
            teamMember = GetComponent<TeamMember>();

        if (animator == null)
            animator = GetComponent<Animator>();

       
        if (visibleShotArea == null)
            visibleShotArea = transform.Find("VisibleShotArea")
                ?.GetComponent<CircleCollider2D>();

        if (shotArea == null)
            shotArea = transform.Find("ShotArea")
                ?.GetComponent<CircleCollider2D>();

       
        if (shotAreaVisual == null)
            shotAreaVisual = transform
                .Find("ShotArea/ShotAreaVisual")
                ?.GetComponent<SpriteRenderer>();

        filter = new ContactFilter2D();
        filter.useTriggers = true;

       
        if (shotAreaVisual != null)
            shotAreaVisual.enabled = false;

        SetDirection(Vector2.down);
    }

    void Update()
    {
        
        if (shotAreaVisual != null)
            shotAreaVisual.enabled = HasPlayerNearby();

       
        if (Time.time >= nextAttackTime)
            Attack();
    }

    bool HasPlayerNearby()
    {
        if (visibleShotArea == null)
            return false;

        int count = visibleShotArea.Overlap(filter, hits);

        for (int i = 0; i < count; i++)
        {
            if (hits[i] == null) continue;

            PlayerMovement player =
                hits[i].GetComponentInParent<PlayerMovement>();

            if (player != null && player.isActiveAndEnabled)
                return true;
        }

        return false;
    }

    TeamMember FindEnemy()
    {
        if (shotArea == null || teamMember == null)
            return null;

        int count = shotArea.Overlap(filter, hits);

        TeamMember nearest = null;
        float minDistance = float.MaxValue;

        for (int i = 0; i < count; i++)
        {
            if (hits[i] == null) continue;

            TeamMember target =
                hits[i].GetComponentInParent<TeamMember>();

            
            if (target == null || target == teamMember)
                continue;

            if (!target.isActiveAndEnabled)
                continue;

            if (!teamMember.IsEnemy(target))
                continue;

            float distance = Vector2.Distance(
                transform.position,
                target.transform.position
            );

            if (distance < minDistance)
            {
                minDistance = distance;
                nearest = target;
            }
        }

        return nearest;
    }

    public void Attack()
    {
        if (Time.time < nextAttackTime)
            return;

        TeamMember enemy = FindEnemy();

        if (enemy == null)
            return;

        Vector2 direction =
            enemy.transform.position - transform.position;

        SetDirection(direction);

       
        if (animator != null)
            animator.SetTrigger("Attack");

       
        float damage = stat != null ? stat.Attack : 10f;

        
        enemy.SendMessageUpwards(
            "TakeDamage",
            damage,
            SendMessageOptions.DontRequireReceiver
        );

        Debug.Log("CombatDrone danh " + enemy.name
            + " - Sat thuong: " + damage);

        
        float speed = stat != null ? stat.AttackSpeed : 1f;

        nextAttackTime =
            Time.time + 1f / Mathf.Max(0.01f, speed);
    }

    void SetDirection(Vector2 direction)
    {
        if (animator == null) return;

        if (direction.sqrMagnitude < 0.001f)
            direction = Vector2.down;

        direction.Normalize();

        animator.SetFloat("AttackX", direction.x);
        animator.SetFloat("AttackY", direction.y);
        animator.SetFloat("LastMoveX", direction.x);
        animator.SetFloat("LastMoveY", direction.y);
    }
}
