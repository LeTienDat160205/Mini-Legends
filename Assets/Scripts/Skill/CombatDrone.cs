
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

    [Header("Dan cua tru")]
    [SerializeField] private TurretProjectile bulletPrefab;
    [SerializeField] private Transform firePoint;

    private float nextAttackTime;

    private Component currentTarget;
    private bool warnedMissingBullet;

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

        if (firePoint == null)
            firePoint = transform.Find("FirePoint");

        filter = new ContactFilter2D();
        filter.useTriggers = true;

        if (shotAreaVisual != null)
            shotAreaVisual.enabled = false;
    }

    void Update()
    {
        if (shotAreaVisual != null)
            shotAreaVisual.enabled = HasPlayerNearby();

        if (Time.time >= nextAttackTime)
            Attack();
    }

    // Hien vung ban khi co tuong dich (Player hoac AI) trong vung quan sat.
    bool HasPlayerNearby()
    {
        if (visibleShotArea == null || teamMember == null)
            return false;

        int count = visibleShotArea.Overlap(filter, hits);
        for (int i = 0; i < count; i++)
        {
            Collider2D hit = hits[i];
            if (hit == null || !hit.enabled) continue;

            TeamMember member = hit.GetComponentInParent<TeamMember>();
            if (IsEnemyChampion(member)) return true;
        }
        return false;
    }

    // Chi nhan tuong co bo dieu khien, tranh nham tru/cac cong trinh
    // co TeamMember thanh tuong.
    bool IsEnemyChampion(TeamMember member)
    {
        if (member == null || member == teamMember ||
            !member.isActiveAndEnabled || member.CurrentHeart <= 0f ||
            member.team == Team.Neutral || member.team == teamMember.team)
            return false;

        PlayerMovement human = member.GetComponent<PlayerMovement>();
        EnemyMovement bot = member.GetComponent<EnemyMovement>();
        return (human != null && human.isActiveAndEnabled) ||
               (bot != null && bot.isActiveAndEnabled);
    }

    // Tim ca linh Minion va nguoi choi phe dich
    Component FindEnemy()
    {
        if (shotArea == null || teamMember == null)
            return null;

        Team myTeam = teamMember.team;

        if (myTeam != Team.Blue && myTeam != Team.Red)
            return null;

        Team enemyTeam = myTeam == Team.Blue
            ? Team.Red : Team.Blue;

        int count = shotArea.Overlap(filter, hits);

        Component nearest = null;
        float minDistance = float.MaxValue;

        for (int i = 0; i < count; i++)
        {
            Collider2D hit = hits[i];

            if (hit == null || !hit.enabled || hit.isTrigger)
                continue;

            Component candidate = null;

            // Uu tien kiem tra linh
            Minion minion =
                hit.GetComponentInParent<Minion>();

            if (minion != null)
            {
                if (minion.isActiveAndEnabled &&
                    minion.Team == enemyTeam)
                {
                    candidate = minion;
                }
            }
            else
            {
                // Nhan ca Player va AI, khong nham cong trinh.
                TeamMember member = hit.GetComponentInParent<TeamMember>();
                if (IsEnemyChampion(member))
                    candidate = member;
            }

            if (candidate == null)
                continue;

            // Chi chon muc tieu thuc su trong vung ban
            Vector2 targetPos = candidate.transform.position;

            if (!shotArea.OverlapPoint(targetPos))
                continue;

            float distance = Vector2.Distance(
                transform.position,
                targetPos
            );

            if (distance < minDistance)
            {
                minDistance = distance;
                nearest = candidate;
            }
        }

        return nearest;
    }

    bool IsTargetAlive(Component target)
    {
        // Muc tieu da bi huy
        if (target == null)
            return false;

        if (!target.gameObject.activeInHierarchy)
            return false;

        if (teamMember == null)
            return false;

        // Xac dinh phe dich
        Team enemyTeam = teamMember.team == Team.Blue
            ? Team.Red
            : Team.Blue;

        // Neu muc tieu la linh
        Minion minion = target as Minion;

        if (minion != null)
        {
            // Minion bi disable khi chet
            return minion.isActiveAndEnabled &&
                   minion.Team == enemyTeam;
        }

        // Neu muc tieu la nguoi choi
        TeamMember player = target as TeamMember;

        if (player != null)
        {
            return player.isActiveAndEnabled &&
                   player.CurrentHeart > 0f &&
                   player.team == enemyTeam;
        }

        return false;
    }



    public void Attack()
    {
        // 1. Kiem tra cooldown
        if (Time.time < nextAttackTime)
            return;

        if (teamMember == null || shotArea == null)
            return;

        // 2. Neu muc tieu cu da chet thi bo khoa
        if (currentTarget != null &&
            !IsTargetAlive(currentTarget))
        {
            Debug.Log(name + " muc tieu da chet");

            currentTarget = null;
        }

        // 3. Chi tim ke dich moi khi chua co muc tieu
        if (currentTarget == null)
        {
            currentTarget = FindEnemy();

            if (currentTarget == null)
                return;

            Debug.Log(
                name + " khoa muc tieu: " +
                currentTarget.name
            );
        }

        // 4. Muc tieu con song thi giu nguyen
        if (!IsTargetAlive(currentTarget))
        {
            currentTarget = null;
            return;
        }

        // 5. Neu muc tieu ra ngoai tam ban
        // thi cho no quay lai, khong doi sang linh khac
        Vector2 targetPos = currentTarget.transform.position;

        if (!shotArea.OverlapPoint(targetPos))
            return;

        // 6. Kiem tra prefab dan
        if (bulletPrefab == null)
        {
            Debug.LogWarning(name + " chua gan dan!");
            return;
        }

        // 7. Vi tri ban
        Vector3 spawnPos = firePoint != null
            ? firePoint.position
            : transform.position;

        // 8. Huong ban
        Vector2 direction =
            (Vector2)currentTarget.transform.position -
            (Vector2)spawnPos;

        if (direction.sqrMagnitude < 0.001f)
            direction = Vector2.right;

        SetDirection(direction);

        // 9. Animation
        if (animator != null)
            animator.SetTrigger("Attack");

        // 10. Sat thuong
        float damage = stat != null
            ? stat.Attack
            : 10f;

        // 11. Tao dan
        TurretProjectile bullet = Instantiate(
            bulletPrefab,
            spawnPos,
            Quaternion.identity
        );

        // 12. Cho dan bay theo muc tieu dang khoa
        bullet.Init(
            teamMember.team,
            currentTarget,
            damage
        );

        Debug.Log(
            name + " dang ban: " +
            currentTarget.name +
            " | Sat thuong: " + damage
        );

        // 13. Tinh cooldown
        float speed = stat != null
            ? stat.AttackSpeed
            : 1f;

        nextAttackTime =
            Time.time + 1f / Mathf.Max(0.01f, speed);
    }

    void SetDirection(Vector2 direction)
    {
        if (animator == null)
            return;

        if (direction.sqrMagnitude < 0.001f)
            direction = Vector2.down;

        direction.Normalize();

        animator.SetFloat("AttackX", direction.x);
        animator.SetFloat("AttackY", direction.y);
        animator.SetFloat("LastMoveX", direction.x);
        animator.SetFloat("LastMoveY", direction.y);
    }
}
