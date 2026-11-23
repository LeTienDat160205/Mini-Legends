using UnityEngine;

public class Minion : MonoBehaviour
{
    [SerializeField] private Team team = Team.Blue;
    public Team Team => team;
    [SerializeField, Min(0f)] private float detectionRadius = 3f;
    [SerializeField, Min(0f)] private float attackRange = 1.2f;
    [SerializeField, Min(0f)] private float moveSpeed = 1.5f;
    [SerializeField, Min(0f)] private float damage = 10f;
    [SerializeField, Min(0.01f)] private float attacksPerSecond = 1f;
    [SerializeField, Min(1f)] private float maxHealth = 100f;

    private Rigidbody2D body;
    private Collider2D bodyCollider;
    private Animator animator;
    private Stat stat;
    private HealthBar healthBar;
    private Transform enemyNexus;
    private float nextAttackTime;
    private float currentHealth;
    private Vector2 lastDirection = Vector2.down;
    private int avoidanceSide;
    private Vector2 lastProgressPosition;
    private float lastProgressTime;
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
        if (body != null) body.bodyType = RigidbodyType2D.Kinematic;
        animator = GetComponent<Animator>();
        stat = GetComponent<Stat>();
        if (stat != null && stat.Heart > 0f) maxHealth = stat.Heart;
        currentHealth = maxHealth;
        lastProgressPosition = transform.position;
        lastProgressTime = Time.time;
        healthBar = GetComponentInChildren<HealthBar>(true);
        if (healthBar != null)
        {
            healthBar.SetTarget(transform);
            RefreshHealthBar();
        }
        if (GetComponent<Collider2D>() == null)
        {
            CircleCollider2D collider = gameObject.AddComponent<CircleCollider2D>();
            collider.radius = 0.4f;
        }
        foreach (Collider2D candidate in GetComponents<Collider2D>())
            if (!candidate.isTrigger) { bodyCollider = candidate; break; }
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
                float hitDamage = stat != null ? stat.Attack : damage;
                Minion targetMinion = destination.GetComponent<Minion>();
                TeamMember targetMember = destination.GetComponent<TeamMember>();
                Nexus targetNexus = destination.GetComponent<Nexus>();
                if (targetMinion != null) targetMinion.TakeDamage(hitDamage, gameObject);
                else if (targetNexus != null) targetNexus.TakeDamage(hitDamage);
                else if (targetMember != null) targetMember.TakeDamage(hitDamage, gameObject);
                float speed = stat != null && stat.AttackSpeed > 0f ? stat.AttackSpeed : attacksPerSecond;
                nextAttackTime = Time.time + 1f / Mathf.Max(0.01f, speed);
            }
            return;
        }

        Vector2 direction = delta.normalized;
        float speedValue = stat != null && stat.Speed > 0f ? stat.Speed : moveSpeed;
        direction = FindTravelDirection(direction, speedValue);
        Vector2 step = MovementCollision2D.AllowedStep(bodyCollider, direction * speedValue * Time.fixedDeltaTime, this);
        Vector2 next = (body != null ? body.position : (Vector2)transform.position) + step;
        if (body != null) body.MovePosition(next);
        else transform.position = next;
        if (step.sqrMagnitude > 0.000001f) lastDirection = direction;
        Animate(step.sqrMagnitude > 0f ? direction : Vector2.zero);
    }

    private Vector2 FindTravelDirection(Vector2 desired, float speedValue)
    {
        if (bodyCollider == null) return desired;
        Vector2 currentPosition = body != null ? body.position : (Vector2)transform.position;
        if ((currentPosition - lastProgressPosition).sqrMagnitude > 0.04f)
        {
            lastProgressPosition = currentPosition;
            lastProgressTime = Time.time;
        }
        else if (avoidanceSide != 0 && Time.time - lastProgressTime > 1f)
        {
            avoidanceSide = -avoidanceSide;
            lastProgressTime = Time.time;
        }
        float probe = Mathf.Max(0.9f, speedValue * Time.fixedDeltaTime * 8f);
        float directClearance = MovementCollision2D.AllowedStep(bodyCollider, desired * probe, this).magnitude;
        if (directClearance >= probe - 0.02f)
        {
            avoidanceSide = 0;
            return desired;
        }

        if (avoidanceSide == 0)
        {
            Vector2 left = new Vector2(-desired.y, desired.x);
            float leftRoom = MovementCollision2D.AllowedStep(bodyCollider, left * probe, this).magnitude;
            float rightRoom = MovementCollision2D.AllowedStep(bodyCollider, -left * probe, this).magnitude;
            avoidanceSide = leftRoom >= rightRoom ? 1 : -1;
        }

        Vector2 best = desired;
        float bestScore = directClearance / probe;
        for (int i = 1; i <= 6; i++)
        {
            float angle = avoidanceSide * i * 30f;
            Vector2 candidate = Quaternion.Euler(0f, 0f, angle) * desired;
            float clearance = MovementCollision2D.AllowedStep(bodyCollider, candidate * probe, this).magnitude;
            float score = clearance / probe + Vector2.Dot(candidate, desired) * 0.2f;
            if (score <= bestScore) continue;
            bestScore = score;
            best = candidate;
        }
        return best;
    }

    public void TakeDamage(float amount)
    {
        TakeDamage(amount, null);
    }

    public void TakeDamage(float amount, GameObject attacker)
    {
        if (amount <= 0f || currentHealth <= 0f) return;
        currentHealth = Mathf.Max(0f, currentHealth - amount);
        RefreshHealthBar();
        if (currentHealth <= 0f)
        {
            if (attacker != null)
                attacker.GetComponent<Level_Gold>()?.GrantKillReward(gameObject);
            if (bodyCollider != null) bodyCollider.enabled = false;
            if (body != null) body.linearVelocity = Vector2.zero;
            float deathDuration = 1f;
            if (animator != null)
            {
                animator.ResetTrigger(Attack);
                animator.SetBool("IsDead", true);
                animator.Play("Dead", 0, 0f);
                if (animator.runtimeAnimatorController != null)
                    foreach (AnimationClip clip in animator.runtimeAnimatorController.animationClips)
                        if (clip.name.StartsWith("Death_"))
                            deathDuration = Mathf.Max(deathDuration, clip.length);
            }
            Destroy(gameObject, deathDuration + 0.1f);
            enabled = false;
        }
    }

    private void RefreshHealthBar()
    {
        if (healthBar != null) healthBar.UpdateBar(currentHealth, maxHealth);
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
