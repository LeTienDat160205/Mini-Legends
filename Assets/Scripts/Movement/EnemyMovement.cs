using UnityEngine;

/// <summary>
/// Stage 3: attack hostile minions and champions BEFORE pushing eligible structures.
/// Same shared lane waypoints, same local obstacle steering, same Q/E vs champions.
/// </summary>
[RequireComponent(typeof(Rigidbody2D), typeof(TeamMember), typeof(Stat))]
public class EnemyMovement : MonoBehaviour
{
    [Header("AI Movement (keep current route)")]
    [SerializeField] private float speedMultiplier = 1f;
    [SerializeField] private float defaultSpeed = 1.5f;
    [SerializeField] private float waypointReachedDistance = 0.5f;
    [SerializeField] private Transform[] waypoints;

    [Header("AI Combat")]
    [SerializeField] private bool enableBasicAttacks = true;
    [SerializeField, Min(1f)] private float detectionRange = 7f;
    [SerializeField, Min(1f)] private float disengageRange = 10f;
    [SerializeField, Min(0.1f)] private float decisionInterval = 0.25f;
    [SerializeField] private LayerMask targetLayers = ~0;

    [Header("Stage 3: Lane clear and objectives")]
    [SerializeField] private bool attackMinions = true;
    [SerializeField] private bool attackChampions = true;
    [SerializeField] private bool attackStructures = true;
    [Tooltip("Minions and champions in this radius prevent pushing towers")]
    [SerializeField, Min(1f)] private float laneClearRadius = 9f;
    [SerializeField, Min(1f)] private float structureDetectionRange = 8f;
    [SerializeField, Range(0.5f, 1.5f)] private float minionPriorityFactor = 0.9f;
    [SerializeField] private bool showDebugTarget = false;

    [Header("AI skills - Q / E (champions only)")]
    [SerializeField] private bool enableSkills = true;
    [SerializeField, Min(0.2f)] private float skillDecisionInterval = 0.65f;
    [SerializeField, Range(0f, 1f)] private float skillUseChance = 0.7f;
    [SerializeField] private bool prioritizeQ = true;

    [Header("Anti-stuck (chống kẹt)")]
    [Tooltip("Cứ mỗi khoảng này thì đo xem AI có thật sự di chuyển được không")]
    [SerializeField, Min(0.5f)] private float stuckCheckInterval = 1f;
    [Tooltip("Kẹt liên tục quá số giây này thì bỏ mục tiêu / bỏ waypoint hiện tại")]
    [SerializeField, Min(1f)] private float giveUpSeconds = 3f;
    [Tooltip("Mục tiêu bị bỏ sẽ bị bỏ qua trong khoảng thời gian này")]
    [SerializeField, Min(1f)] private float ignoreTargetSeconds = 4f;
    [SerializeField] private bool showStuckDebug = false;

    [Header("Diagnostics")]
    [SerializeField] private bool showGizmos = true;

    private Rigidbody2D rb;
    private AIObstacleSteering2D obstacleSteering;
    private Collider2D bodyCollider;
    private Animator animator;
    private TeamMember ownTeam;
    private Stat stat;
    private BladeAssasin blade;
    private GoblinScout goblin;
    private FootSoldier soldier;
    private TeamMember currentChampion;
    private Minion currentMinion;
    private ARAMCombatStructure currentStructure;
    private int waypointIndex;
    private float nextDecisionTime;
    private float nextSkillDecisionTime;
    private Vector2 moveInput;
    private Vector2 lastDirection = Vector2.down;

    // Anti-stuck
    private Vector2 progressAnchor;
    private float progressAnchorTime;
    private float stuckAccum;
    private UnityEngine.Object ignoredTarget;
    private float ignoreUntil;

    private static readonly int MoveX = Animator.StringToHash("MoveX");
    private static readonly int MoveY = Animator.StringToHash("MoveY");
    private static readonly int MoveMagnitude = Animator.StringToHash("MoveMagnitude");
    private static readonly int LastMoveX = Animator.StringToHash("LastMoveX");
    private static readonly int LastMoveY = Animator.StringToHash("LastMoveY");

    public void ConfigureWaypoints(Transform[] route)
    {
        waypoints = route;
        waypointIndex = 0;
    }

    private void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        obstacleSteering = GetComponent<AIObstacleSteering2D>();
        if (obstacleSteering == null) obstacleSteering = gameObject.AddComponent<AIObstacleSteering2D>();
        rb.bodyType = RigidbodyType2D.Kinematic;
        ownTeam = GetComponent<TeamMember>();
        stat = GetComponent<Stat>();
        animator = GetComponent<Animator>();
        if (animator == null) animator = GetComponentInChildren<Animator>();
        blade = GetComponent<BladeAssasin>();
        goblin = GetComponent<GoblinScout>();
        soldier = GetComponent<FootSoldier>();
        foreach (Collider2D c in GetComponents<Collider2D>())
            if (c != null && c.enabled && !c.isTrigger) { bodyCollider = c; break; }
        if (bodyCollider == null)
            foreach (Collider2D c in GetComponents<Collider2D>())
                if (c != null && c.enabled) { bodyCollider = c; break; }
    }

    private void OnEnable()
    {
        currentChampion = null; currentMinion = null; currentStructure = null;
        waypointIndex = 0;
        nextDecisionTime = 0f;
        nextSkillDecisionTime = Time.time + Random.Range(0.1f, 0.6f);
        moveInput = Vector2.zero;

        progressAnchor = transform.position;
        progressAnchorTime = Time.time;
        stuckAccum = 0f;
        ignoredTarget = null;
        ignoreUntil = 0f;

        if (obstacleSteering != null) obstacleSteering.ResetNavigation();
    }

    private void OnDisable()
    {
        moveInput = Vector2.zero;
        if (rb != null) rb.linearVelocity = Vector2.zero;
    }

    private void Update()
    {
        if (ownTeam == null || ownTeam.CurrentHeart <= 0f)
        {
            moveInput = Vector2.zero;
            UpdateAnimator();
            return;
        }

        if (Time.time >= nextDecisionTime)
        {
            nextDecisionTime = Time.time + Mathf.Max(0.1f, decisionInterval);
            ChooseTarget();
        }

        if (IsBusyCasting() || (blade != null && blade.IsDashing))
            moveInput = Vector2.zero;
        else if (currentChampion != null && currentChampion.isActiveAndEnabled && currentChampion.CurrentHeart > 0f)
            FightChampion(currentChampion);
        else if (currentMinion != null && currentMinion.isActiveAndEnabled && currentMinion.gameObject.activeInHierarchy)
            FightMinion(currentMinion);
        else if (currentStructure != null && !currentStructure.isDestroyed && !currentStructure.isInvulnerable)
            FightStructure(currentStructure);
        else moveInput = GetPatrolDirection();

        UpdateStuckWatchdog();

        if (moveInput.sqrMagnitude > 0.001f) lastDirection = moveInput.normalized;
        UpdateAnimator();
    }

    // Đo xem AI có thật sự di chuyển được khi đang muốn di chuyển hay không.
    // Nếu kẹt liên tục quá giveUpSeconds thì bỏ mục tiêu (hoặc waypoint) hiện tại.
    private void UpdateStuckWatchdog()
    {
        if (rb == null) return;

        // Đang đứng đánh / đang dùng chiêu / không có hướng đi -> không tính là kẹt
        if (moveInput.sqrMagnitude < 0.001f || IsBusyCasting() || (blade != null && blade.IsDashing))
        {
            progressAnchor = rb.position;
            progressAnchorTime = Time.time;
            stuckAccum = 0f;
            return;
        }

        float elapsed = Time.time - progressAnchorTime;
        if (elapsed < stuckCheckInterval) return;

        float speed = stat != null && stat.Speed > 0.01f ? stat.Speed : defaultSpeed;
        float expected = speed * speedMultiplier * elapsed;
        float moved = Vector2.Distance(rb.position, progressAnchor);

        progressAnchor = rb.position;
        progressAnchorTime = Time.time;

        if (moved < expected * 0.3f) stuckAccum += elapsed;
        else stuckAccum = 0f;

        if (stuckAccum >= giveUpSeconds)
        {
            stuckAccum = 0f;
            GiveUpCurrentGoal();
        }
    }

    private void GiveUpCurrentGoal()
    {
        if (currentChampion != null)
        {
            if (showStuckDebug) Debug.Log($"[AI Stuck] {name}: bỏ champion {currentChampion.name}", this);
            IgnoreTarget(currentChampion);
        }
        else if (currentMinion != null)
        {
            if (showStuckDebug) Debug.Log($"[AI Stuck] {name}: bỏ minion {currentMinion.name}", this);
            IgnoreTarget(currentMinion);
        }
        else if (currentStructure != null)
        {
            if (showStuckDebug) Debug.Log($"[AI Stuck] {name}: bỏ công trình {currentStructure.name}", this);
            IgnoreTarget(currentStructure);
        }
        else if (waypoints != null && waypointIndex < waypoints.Length - 1)
        {
            // Waypoint có thể nằm trong vật cản / không tới được -> bỏ qua, đi tới waypoint kế tiếp
            if (showStuckDebug) Debug.Log($"[AI Stuck] {name}: bỏ waypoint #{waypointIndex}", this);
            waypointIndex++;
        }

        currentChampion = null;
        currentMinion = null;
        currentStructure = null;
        if (obstacleSteering != null) obstacleSteering.ResetNavigation();
    }

    private void IgnoreTarget(UnityEngine.Object target)
    {
        ignoredTarget = target;
        ignoreUntil = Time.time + ignoreTargetSeconds;
    }

    private bool IsIgnored(UnityEngine.Object target)
    {
        return target != null && ignoredTarget == target && Time.time < ignoreUntil;
    }

    private void FightChampion(TeamMember target)
    {
        Vector2 diff = Bullet.GetAimPoint(target.transform) - (Vector2)transform.position;
        float dist = diff.magnitude;
        if (enableSkills && Time.time >= nextSkillDecisionTime)
        {
            nextSkillDecisionTime = Time.time + Mathf.Max(0.2f, skillDecisionInterval);
            if (Random.value <= skillUseChance && TryUseSkill(target))
            {
                moveInput = Vector2.zero;
                return;
            }
        }
        FightAtDistance(diff, dist, () => TryBasicAttack(target));
    }

    private void FightMinion(Minion target)
    {
        Vector2 diff = Bullet.GetAimPoint(target.transform) - (Vector2)transform.position;
        FightAtDistance(diff, diff.magnitude, () => TryBasicAttack(target));
    }

    private void FightStructure(ARAMCombatStructure target)
    {
        // Collider closest point is more accurate than tower's transform center.
        Collider2D collider = target.GetComponentInChildren<Collider2D>();
        Vector2 aim = collider != null ? collider.ClosestPoint(transform.position) : (Vector2)target.transform.position;
        Vector2 diff = aim - (Vector2)transform.position;
        // Công trình có collider rắn nên không thể áp sát như đánh người: nới thêm tầm để AI không đứng
        // ép sát trụ mà không chịu đánh.
        FightAtDistance(diff, diff.magnitude, () => TryBasicAttack(target), 0.97f);
    }

    private void FightAtDistance(Vector2 diff, float distance, System.Action attack, float rangeFactor = 0.85f)
    {
        float range = Mathf.Max(0.1f, stat.AttackRange) * rangeFactor;
        if (distance <= range)
        {
            moveInput = Vector2.zero;
            if (diff.sqrMagnitude > 0.001f) lastDirection = diff.normalized;
            if (enableBasicAttacks) attack();
        }
        else moveInput = diff.sqrMagnitude > 0.0001f ? diff.normalized : Vector2.zero;
    }

    private void FixedUpdate()
    {
        if (rb == null || ownTeam == null || ownTeam.CurrentHeart <= 0f) return;
        if (IsBusyCasting() || (blade != null && blade.IsDashing)) return;
        float speed = stat.Speed > 0.01f ? stat.Speed : defaultSpeed;
        float stepDistance = speed * speedMultiplier * Time.fixedDeltaTime;
        Vector2 step = obstacleSteering != null
            ? obstacleSteering.GetSafeStep(moveInput, stepDistance)
            : MovementCollision2D.AllowedStep(bodyCollider, moveInput * stepDistance);
        rb.linearVelocity = Vector2.zero;
        rb.MovePosition(rb.position + step);
    }

    private void ChooseTarget()
    {
        currentChampion = null;
        currentMinion = null;
        currentStructure = null;
        if (ownTeam == null) return;

        // Step 1: pick lane enemies (minions and champions). Minions do not need TeamMember.
        float range = Mathf.Max(detectionRange, laneClearRadius);
        Collider2D[] hits = Physics2D.OverlapCircleAll(transform.position, range, targetLayers);
        float bestScore = float.MaxValue;
        foreach (Collider2D hit in hits)
        {
            if (hit == null) continue;
            Minion m = attackMinions ? hit.GetComponentInParent<Minion>() : null;
            if (m != null)
            {
                if (!m.isActiveAndEnabled || !m.gameObject.activeInHierarchy || m.Team == ownTeam.team ||
                    IsIgnored(m)) continue;
                float d = Vector2.Distance(transform.position, Bullet.GetAimPoint(m.transform));
                if (d > range) continue;
                float score = d * minionPriorityFactor;
                if (score < bestScore) { bestScore = score; currentMinion = m; currentChampion = null; }
                continue;
            }
            if (!attackChampions) continue;
            TeamMember c = hit.GetComponentInParent<TeamMember>();
            if (c == null || c == ownTeam || !c.isActiveAndEnabled || c.CurrentHeart <= 0f ||
                !ownTeam.IsEnemy(c) || IsIgnored(c)) continue;
            // A TeamMember on a static structure must not be treated as a champion.
            if (c.GetComponent<FootSoldier>() == null && c.GetComponent<GoblinScout>() == null &&
                c.GetComponent<BladeAssasin>() == null) continue;
            float distance = Vector2.Distance(transform.position, Bullet.GetAimPoint(c.transform));
            if (distance > range) continue;
            if (distance < bestScore) { bestScore = distance; currentChampion = c; currentMinion = null; }
        }

        // Step 2: DO NOT attack buildings while a lane unit is nearby.
        if (currentChampion != null || currentMinion != null || !attackStructures) return;
        ARAMCombatStructure[] structures = FindObjectsByType<ARAMCombatStructure>(FindObjectsSortMode.None);
        float nearest = structureDetectionRange;
        foreach (ARAMCombatStructure structure in structures)
        {
            if (structure == null || !structure.isActiveAndEnabled || structure.isDestroyed ||
                structure.isInvulnerable || structure.team == (ownTeam.team == Team.Blue ? ARAMCombatStructure.Team.Blue : ARAMCombatStructure.Team.Red) ||
                structure.structureType == ARAMCombatStructure.StructureType.HealthRelic ||
                IsIgnored(structure)) continue;
            Collider2D c = structure.GetComponentInChildren<Collider2D>();
            Vector2 point = c != null ? c.ClosestPoint(transform.position) : (Vector2)structure.transform.position;
            float d = Vector2.Distance(transform.position, point);
            if (d < nearest) { nearest = d; currentStructure = structure; }
        }
        if (showDebugTarget)
            Debug.Log($"[AI Target] {name}: {(currentStructure != null ? currentStructure.name : "Patrol")}", this);
    }

    private Vector2 GetPatrolDirection()
    {
        if (waypoints == null || waypoints.Length == 0) return Vector2.zero;
        while (waypointIndex < waypoints.Length)
        {
            Transform point = waypoints[waypointIndex];
            if (point == null) { waypointIndex++; continue; }
            Vector2 diff = (Vector2)point.position - (Vector2)transform.position;
            if (diff.magnitude <= waypointReachedDistance) { waypointIndex++; continue; }
            return diff.normalized;
        }
        return Vector2.zero;
    }

    private void TryBasicAttack(TeamMember target)
    {
        if (soldier != null && soldier.isActiveAndEnabled) soldier.TryAttack(target);
        else if (goblin != null && goblin.isActiveAndEnabled) goblin.TryAttack(target);
        else if (blade != null && blade.isActiveAndEnabled) blade.TryAttack(target);
    }

    private void TryBasicAttack(Minion target)
    {
        if (soldier != null && soldier.isActiveAndEnabled) soldier.TryAttack(target);
        else if (goblin != null && goblin.isActiveAndEnabled) goblin.TryAttack(target);
        else if (blade != null && blade.isActiveAndEnabled) blade.TryAttack(target);
    }

    private void TryBasicAttack(ARAMCombatStructure target)
    {
        if (soldier != null && soldier.isActiveAndEnabled) soldier.TryAttack(target);
        else if (goblin != null && goblin.isActiveAndEnabled) goblin.TryAttack(target);
        else if (blade != null && blade.isActiveAndEnabled) blade.TryAttack(target);
    }

    private bool IsBusyCasting()
    {
        return (soldier != null && soldier.IsUsingSkill) ||
               (goblin != null && goblin.IsUsingSkill) ||
               (blade != null && blade.IsUsingSkill);
    }

    private bool TryUseSkill(TeamMember target)
    {
        if (target == null || target.CurrentHeart <= 0f) return false;
        return prioritizeQ ? (TryQ(target) || TryE(target)) : (TryE(target) || TryQ(target));
    }

    private bool TryQ(TeamMember victim)
    {
        if (soldier != null) return soldier.TryCastQ(victim);
        if (goblin != null) return goblin.TryCastQ(victim);
        if (blade != null) return blade.TryCastQ(victim);
        return false;
    }

    private bool TryE(TeamMember victim)
    {
        if (soldier != null) return soldier.TryCastE(victim);
        if (goblin != null) return goblin.TryCastE(victim);
        if (blade != null) return blade.TryCastE(victim);
        return false;
    }

    private void UpdateAnimator()
    {
        if (animator == null) return;
        animator.SetFloat(MoveX, moveInput.x);
        animator.SetFloat(MoveY, moveInput.y);
        animator.SetFloat(MoveMagnitude, moveInput.magnitude);
        animator.SetFloat(LastMoveX, lastDirection.x);
        animator.SetFloat(LastMoveY, lastDirection.y);
    }

    private void OnDrawGizmosSelected()
    {
        if (!showGizmos) return;
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, detectionRange);
        Gizmos.color = Color.cyan;
        if (waypoints == null) return;
        foreach (Transform point in waypoints)
            if (point != null) Gizmos.DrawLine(transform.position, point.position);
    }
}