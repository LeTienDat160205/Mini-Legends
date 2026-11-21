using UnityEngine;

public class Minion : MonoBehaviour
{
    [SerializeField] private Team team = Team.Blue;
    [SerializeField, Min(0f)] private float detectionRadius = 3f;
    [SerializeField, Min(0f)] private float attackRange = 1.2f;
    [SerializeField, Min(0f)] private float moveSpeed = 1.5f;
    [SerializeField, Min(0f)] private float damage = 10f;
    [SerializeField, Min(0.01f)] private float attacksPerSecond = 1f;
    [SerializeField, Min(1f)] private float maxHealth = 100f;

    private Rigidbody2D body;
    private Animator animator;
    private Stat stat;
    private Transform enemyNexus;
    private float nextAttackTime;
    private float currentHealth;
    private Vector2 lastDirection = Vector2.down;
    private static readonly int MoveX = Animator.StringToHash("MoveX");
    private static readonly int MoveY = Animator.StringToHash("MoveY");
    private static readonly int MoveMagnitude = Animator.StringToHash("MoveMagnitude");
    private static readonly int LastMoveX = Animator.StringToHash("LastMoveX");
    private static readonly int LastMoveY = Animator.StringToHash("LastMoveY");
    private static readonly int AttackX = Animator.StringToHash("AttackX");
    private static readonly int AttackY = Animator.StringToHash("AttackY");
    private static readonly int Attack = Animator.StringToHash("Attack");

    private void Awake()
    {
        body = GetComponent<Rigidbody2D>();
        animator = GetComponent<Animator>();
        stat = GetComponent<Stat>();
        currentHealth = stat != null && stat.Heart > 0f ? stat.Heart : maxHealth;
        if (GetComponent<Collider2D>() == null)
        {
            CircleCollider2D collider = gameObject.AddComponent<CircleCollider2D>();
            collider.radius = 0.4f;
        }
    }

    public void Initialize(Team minionTeam)
    {
        team = minionTeam;
        TeamMember member = GetComponent<TeamMember>();
        if (member != null) member.team = team;
        FindEnemyNexus();
    }

    private void FindEnemyNexus()
    {
        enemyNexus = null;
        foreach (GameObject nexus in GameObject.FindGameObjectsWithTag("Nexus"))
        {
            TeamMember member = nexus.GetComponent<TeamMember>();
            bool enemy = member != null ? member.team != team :
                team == Team.Blue ? nexus.name.Contains("Red") : nexus.name.Contains("Blue");
            if (enemy) { enemyNexus = nexus.transform; break; }
        }
    }

    private void FixedUpdate()
    {
        if (enemyNexus == null) FindEnemyNexus();
        Transform target = FindNearbyEnemy();
        Transform destination = target != null ? target : enemyNexus;
        if (destination == null) { Animate(Vector2.zero); return; }

        Vector2 delta = (Vector2)(destination.position - transform.position);
        float range = stat != null && stat.AttackRange > 0f ? stat.AttackRange : attackRange;
        if (delta.magnitude <= range)
        {
            Animate(Vector2.zero);
            if (delta.sqrMagnitude > 0.001f) lastDirection = delta.normalized;
            if (Time.time >= nextAttackTime)
            {
                if (animator != null)
                {
                    animator.SetFloat(AttackX, lastDirection.x);
                    animator.SetFloat(AttackY, lastDirection.y);
                    animator.SetTrigger(Attack);
                }
                destination.SendMessageUpwards("TakeDamage", stat != null ? stat.Attack : damage,
                    SendMessageOptions.DontRequireReceiver);
                float speed = stat != null && stat.AttackSpeed > 0f ? stat.AttackSpeed : attacksPerSecond;
                nextAttackTime = Time.time + 1f / Mathf.Max(0.01f, speed);
            }
            return;
        }

        Vector2 direction = delta.normalized;
        float speedValue = stat != null && stat.Speed > 0f ? stat.Speed : moveSpeed;
        Vector2 next = (Vector2)transform.position + direction * speedValue * Time.fixedDeltaTime;
        if (body != null) body.MovePosition(next);
        else transform.position = next;
        lastDirection = direction;
        Animate(direction);
    }

    public void TakeDamage(float amount)
    {
        if (amount <= 0f || currentHealth <= 0f) return;
        currentHealth -= amount;
        if (currentHealth <= 0f)
        {
            if (animator != null) animator.SetBool("IsDead", true);
            Destroy(gameObject, 0.5f);
            enabled = false;
        }
    }

    private Transform FindNearbyEnemy()
    {
        Transform nearest = null;
        float nearestDistance = detectionRadius * detectionRadius;
        foreach (Collider2D hit in Physics2D.OverlapCircleAll(transform.position, detectionRadius))
        {
            Minion minion = hit.GetComponentInParent<Minion>();
            if (minion != null && minion != this && minion.team != team && minion.currentHealth > 0f)
            {
                float minionDistance = ((Vector2)(minion.transform.position - transform.position)).sqrMagnitude;
                if (minionDistance < nearestDistance) { nearest = minion.transform; nearestDistance = minionDistance; }
                continue;
            }
            TeamMember candidate = hit.GetComponentInParent<TeamMember>();
            if (candidate == null || !candidate.isActiveAndEnabled || candidate.CurrentHeart <= 0f ||
                candidate.team == team || candidate.gameObject == gameObject) continue;
            float distance = ((Vector2)(candidate.transform.position - transform.position)).sqrMagnitude;
            if (distance < nearestDistance) { nearest = candidate.transform; nearestDistance = distance; }
        }
        return nearest;
    }

    private void Animate(Vector2 direction)
    {
        if (animator == null) return;
        animator.SetFloat(MoveX, direction.x);
        animator.SetFloat(MoveY, direction.y);
        animator.SetFloat(MoveMagnitude, direction.magnitude);
        animator.SetFloat(LastMoveX, lastDirection.x);
        animator.SetFloat(LastMoveY, lastDirection.y);
    }
}
