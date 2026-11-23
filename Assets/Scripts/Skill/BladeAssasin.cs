using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class BladeAssasin : MonoBehaviour
{
    [Header("Attack")]
    [SerializeField] private GameObject rangeIndicator;
    [SerializeField] private Stat stat;
    [SerializeField] private TeamMember teamMember;
    [SerializeField] private Animator animator;
    [SerializeField] private string attackTrigger = "Attack";
    [SerializeField] private LayerMask attackTargetLayers = ~0;
    [Header("Audio")]
    [SerializeField] private AudioSource sfxSource;
    [SerializeField] private AudioClip attackSound;
    [SerializeField] private AudioClip skillESound;
    [SerializeField] private AudioClip skillQSound;
    [SerializeField] private AudioClip skillUnavailableSound;

    [Header("Skill E - Dash")]
    [SerializeField] private GameObject eDashHitBox;
    [SerializeField] private Transform eDashPivot;
    [SerializeField] private Rigidbody2D body;
    [SerializeField] private float dashDistance = 3f;
    [SerializeField] private float dashDuration = 0.16f;
    [SerializeField] private float dashDamageMultiplier = 1f;
    [SerializeField] private LayerMask dashTargetLayers = ~0;
    [SerializeField] private float dashHitBoxAngleOffset;

    [Header("Skill Q - Area")]
    [SerializeField] private GameObject qRangeIndicator;
    [SerializeField] private float qDuration = 3f;
    [SerializeField] private float qDamageInterval = 0.2f;
    [SerializeField] private float qDamageMultiplier = 1f;
    [SerializeField] private LayerMask qTargetLayers = ~0;

    private Vector3 indicatorBaseScale;
    private readonly List<TeamMember> enemiesInRange = new List<TeamMember>();
    private readonly HashSet<Minion> minionsInRange = new HashSet<Minion>();
    private readonly Dictionary<TeamMember, GameObject> highlights = new Dictionary<TeamMember, GameObject>();
    private readonly HashSet<TeamMember> dashTargetsHit = new HashSet<TeamMember>();
    private readonly HashSet<Minion> dashMinionsHit = new HashSet<Minion>();
    private static readonly int AttackX = Animator.StringToHash("AttackX");
    private static readonly int AttackY = Animator.StringToHash("AttackY");
    private static readonly int AttackSpeed = Animator.StringToHash("AttackSpeed");
    private static readonly int AttackE = Animator.StringToHash("AttackE");
    private static readonly int AttackQ = Animator.StringToHash("AttackQ");
    private static readonly int AttackQState = Animator.StringToHash("AttackQ");
    private bool aimingE;
    private bool dashBusy;
    public bool IsDashing => dashBusy;
    private bool aimingQ;
    private bool qBusy;
    private float cooldownE;
    private float cooldownQ;
    private float nextAttackTime;
    private Vector2 dashDirection;

    private void Awake()
    {
        if (stat == null) stat = GetComponent<Stat>();
        if (teamMember == null) teamMember = GetComponent<TeamMember>();
        if (animator == null) animator = GetComponent<Animator>();
        if (body == null) body = GetComponent<Rigidbody2D>();
        if (sfxSource == null) sfxSource = GetComponent<AudioSource>();
        if (sfxSource == null) sfxSource = gameObject.AddComponent<AudioSource>();
        sfxSource.playOnAwake = false;
        sfxSource.spatialBlend = 0f;
        if (rangeIndicator == null) rangeIndicator = FindChild("RangeIndicator");
        if (eDashHitBox == null) eDashHitBox = FindChild("E_DashHitBox");
        if (qRangeIndicator == null) qRangeIndicator = FindChild("Q_RangeIndicator");
        if (qRangeIndicator != null && qRangeIndicator.GetComponent<Collider2D>() == null)
        {
            BoxCollider2D qCollider = qRangeIndicator.AddComponent<BoxCollider2D>();
            qCollider.isTrigger = true;
        }
        if (eDashPivot == null)
        {
            GameObject pivotObject = FindChild("E_Dash");
            if (pivotObject == null)
            {
                pivotObject = new GameObject("E_Dash");
                eDashPivot = pivotObject.transform;
                eDashPivot.SetParent(transform, false);
            }
            else eDashPivot = pivotObject.transform;
        }

        // E_Dash is the pivot at the player's center. Keep the hitbox's
        // authored local offset (for example, below the player's feet).
        if (eDashPivot != null && eDashHitBox != null && eDashHitBox.transform.parent != eDashPivot)
            eDashHitBox.transform.SetParent(eDashPivot, false);

        if (rangeIndicator != null)
        {
            indicatorBaseScale = rangeIndicator.transform.localScale;
            rangeIndicator.SetActive(false);
        }

        if (eDashHitBox != null)
            eDashHitBox.SetActive(false);
        if (qRangeIndicator != null)
            qRangeIndicator.SetActive(false);
    }

    private void OnDisable()
    {
        StopAllCoroutines();
        aimingE = false;
        aimingQ = false;
        dashBusy = false;
        qBusy = false;
        dashTargetsHit.Clear();
        dashMinionsHit.Clear();
        if (eDashHitBox != null) eDashHitBox.SetActive(false);
        if (qRangeIndicator != null) qRangeIndicator.SetActive(false);
    }

    private void Update()
    {
        if (cooldownE > 0f)
            cooldownE -= Time.deltaTime;
        if (cooldownQ > 0f)
            cooldownQ -= Time.deltaTime;

        HandleDashInput();
        HandleQInput();

        if (aimingE || dashBusy || aimingQ || qBusy)
        {
            if (rangeIndicator != null) rangeIndicator.SetActive(false);
            return;
        }

        bool aiming = Input.GetMouseButton(0);
        if (rangeIndicator != null) rangeIndicator.SetActive(aiming);

        UpdateRangeIndicator();


        if (Input.GetMouseButtonDown(0))
            Attack();
    }

    public void Attack()
    {
        if (aimingE || dashBusy || aimingQ || qBusy || teamMember == null ||
            teamMember.CurrentHeart <= 0f || Time.time < nextAttackTime) return;

        enemiesInRange.Clear();
        minionsInRange.Clear();
        Vector2 center = rangeIndicator != null ? (Vector2)rangeIndicator.transform.position :
            (Vector2)transform.position;
        SpriteRenderer indicatorSprite = rangeIndicator != null ?
            rangeIndicator.GetComponent<SpriteRenderer>() : null;
        Vector2 radii = indicatorSprite != null && indicatorSprite.sprite != null ?
            (Vector2)indicatorSprite.bounds.extents : Vector2.one *
            (stat != null ? stat.AttackRange : 0f);
        if (radii.x <= 0f || radii.y <= 0f) return;
        Collider2D[] hits = Physics2D.OverlapCircleAll(
            center,
            Mathf.Max(radii.x, radii.y),
            attackTargetLayers);

        foreach (Collider2D hit in hits)
        {
            if (hit == null) continue;
            Vector2 closest = hit.ClosestPoint(center) - center;
            if (closest.x * closest.x / (radii.x * radii.x) +
                closest.y * closest.y / (radii.y * radii.y) > 1f) continue;
            Minion minion = hit.GetComponentInParent<Minion>();
            if (minion != null)
            {
                if (minion.Team != teamMember.team) minionsInRange.Add(minion);
                continue;
            }
            TeamMember target = hit.GetComponentInParent<TeamMember>();

            if (target == null || target == teamMember || !target.isActiveAndEnabled ||
                target.CurrentHeart <= 0f)
                continue;

            if (teamMember.IsEnemy(target) && !enemiesInRange.Contains(target))
                enemiesInRange.Add(target);
        }

        if (enemiesInRange.Count == 0 && minionsInRange.Count == 0) return;

        float attackSpeed = stat != null && stat.AttackSpeed > 0f ? stat.AttackSpeed : 1f;
        nextAttackTime = Time.time + 1f / attackSpeed;
        SetAttackDirectionFromMouse();
        if (animator != null) animator.SetFloat(AttackSpeed, attackSpeed);
        animator?.SetTrigger(attackTrigger);
        PlaySound(attackSound);
        foreach (TeamMember enemy in enemiesInRange)
            enemy.TakeDamage(stat != null ? stat.Attack : 0f, gameObject);
        foreach (Minion minion in minionsInRange)
            minion.TakeDamage(stat != null ? stat.Attack : 0f, gameObject);
    }

    private void SetAttackDirectionFromMouse()
    {
        if (animator == null || Camera.main == null) return;

        Vector3 mouseWorld = Camera.main.ScreenToWorldPoint(Input.mousePosition);
        Vector2 direction = (Vector2)(mouseWorld - transform.position);
        if (direction.sqrMagnitude < 0.001f) return;

        direction.Normalize();
        animator.SetFloat(AttackX, direction.x);
        animator.SetFloat(AttackY, direction.y);
    }

    private void UpdateRangeIndicator()
    {
        if (rangeIndicator == null) return;

        float attackRange = stat != null ? stat.AttackRange : 1f;
        rangeIndicator.transform.localScale = indicatorBaseScale * Mathf.Max(0.01f, attackRange);
    }

    private void HandleDashInput()
    {
        if (dashBusy) return;

        if (!aimingE && Input.GetKeyDown(KeyCode.E))
        {
            if (cooldownE > 0f) { PlaySound(skillUnavailableSound); return; }
            aimingE = true;
            dashTargetsHit.Clear();
            if (eDashHitBox != null) eDashHitBox.SetActive(true);
            UpdateDashAim();
        }

        if (!aimingE) return;

        UpdateDashAim();

        // A left click while E is held cancels the skill instead of attacking.
        if (Input.GetMouseButtonDown(0))
        {
            CancelDashAim();
            return;
        }

        // Releasing E commits the dash.
        if (Input.GetKeyUp(KeyCode.E))
        {
            dashDirection = GetMouseDirection();
            if (dashDirection.sqrMagnitude < 0.001f)
                dashDirection = Vector2.right;

            dashDirection.Normalize();
            aimingE = false;
            if (eDashHitBox != null) eDashHitBox.SetActive(false);
            StartCoroutine(PerformDash(dashDirection));
        }
    }

    private void HandleQInput()
    {
        if (dashBusy || qBusy) return;

        if (!aimingQ && Input.GetKeyDown(KeyCode.Q))
        {
            if (cooldownQ > 0f) { PlaySound(skillUnavailableSound); return; }
            aimingQ = true;
            if (qRangeIndicator != null) qRangeIndicator.SetActive(true);
            UpdateQAim();
        }

        if (!aimingQ) return;

        UpdateQAim();

        // Clicking while Q is held cancels the skill.
        if (Input.GetMouseButtonDown(0))
        {
            CancelQAim();
            return;
        }

        // Releasing Q creates the damaging area.
        if (Input.GetKeyUp(KeyCode.Q))
        {
            Vector2 direction = GetMouseDirection();
            if (direction.sqrMagnitude < 0.001f) direction = Vector2.right;
            direction.Normalize();
            aimingQ = false;
            StartCoroutine(PerformQ(direction));
        }
    }

    private void UpdateQAim()
    {
        if (qRangeIndicator == null) return;

        // Q is an area skill. Its position and rotation are authored in the
        // scene and must remain unchanged while the player is holding Q.
    }

    private void CancelQAim()
    {
        aimingQ = false;
        if (qRangeIndicator != null) qRangeIndicator.SetActive(false);
    }

    private IEnumerator PerformQ(Vector2 direction)
    {
        qBusy = true;
        SetAttackDirection(direction);
        animator?.SetTrigger(AttackQ);
        PlaySound(skillQSound);

        if (qRangeIndicator != null) qRangeIndicator.SetActive(true);

        float elapsed = 0f;
        float damageTimer = 0f;
        while (elapsed < qDuration)
        {
            float delta = Time.deltaTime;
            elapsed += delta;
            damageTimer -= delta;
            if (damageTimer <= 0f)
            {
                DamageQTargets();
                damageTimer += Mathf.Max(0.01f, qDamageInterval);
            }
            LoopQAnimation();
            yield return null;
        }

        DamageQTargets();
        if (qRangeIndicator != null) qRangeIndicator.SetActive(false);
        cooldownQ = stat != null && stat.Data != null ? stat.Data.cooldownQ : 5f;
        qBusy = false;
    }

    private void DamageQTargets()
    {
        if (teamMember == null) return;

        if (qRangeIndicator == null) return;

        Collider2D[] colliders = qRangeIndicator.GetComponentsInChildren<Collider2D>(true);
        foreach (Collider2D areaCollider in colliders)
        {
            ContactFilter2D filter = new ContactFilter2D();
            filter.SetLayerMask(qTargetLayers);
            filter.useTriggers = true;
            Collider2D[] hits = new Collider2D[32];
            int count = areaCollider.Overlap(filter, hits);
            for (int i = 0; i < count; i++)
            {
                Minion minion = hits[i] != null ? hits[i].GetComponentInParent<Minion>() : null;
                if (minion != null)
                {
                    if (minion.Team != teamMember.team)
                        minion.TakeDamage(stat != null ? stat.Attack * qDamageMultiplier : 0f, gameObject);
                    continue;
                }
                TeamMember target = hits[i] != null ? hits[i].GetComponentInParent<TeamMember>() : null;
                if (target == null || target == teamMember || !teamMember.IsEnemy(target)) continue;
                target.TakeDamage(stat != null ? stat.Attack * qDamageMultiplier : 0f, gameObject);
            }
        }
    }

    private void LoopQAnimation()
    {
        if (animator == null) return;

        AnimatorStateInfo state = animator.GetCurrentAnimatorStateInfo(0);
        if (state.shortNameHash != AttackQState || state.normalizedTime >= 1f)
            animator.Play(AttackQState, 0, 0f);
    }

    private void UpdateDashAim()
    {
        if (eDashHitBox == null) return;

        Vector2 direction = GetMouseDirection();
        if (direction.sqrMagnitude < 0.001f) return;

        float angle = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg;
        if (eDashPivot != null)
            eDashPivot.rotation = Quaternion.Euler(0f, 0f, angle + dashHitBoxAngleOffset);
        else
            eDashHitBox.transform.rotation = Quaternion.Euler(0f, 0f, angle + dashHitBoxAngleOffset);
    }

    private Vector2 GetMouseDirection()
    {
        if (Camera.main == null) return Vector2.zero;
        Vector3 mouseScreen = Input.mousePosition;
        mouseScreen.z = Mathf.Abs(Camera.main.transform.position.z - transform.position.z);
        Vector3 mouseWorld = Camera.main.ScreenToWorldPoint(mouseScreen);
        mouseWorld.z = transform.position.z;
        return (Vector2)(mouseWorld - transform.position);
    }

    private void CancelDashAim()
    {
        aimingE = false;
        dashTargetsHit.Clear();
        dashMinionsHit.Clear();
        if (eDashHitBox != null) eDashHitBox.SetActive(false);
    }

    private IEnumerator PerformDash(Vector2 direction)
    {
        dashBusy = true;
        dashTargetsHit.Clear();
        dashMinionsHit.Clear();
        SetAttackDirection(direction);
        animator?.SetTrigger(AttackE);
        PlaySound(skillESound);

        Collider2D movementCollider = null;
        foreach (Collider2D candidate in GetComponents<Collider2D>())
            if (!candidate.isTrigger) { movementCollider = candidate; break; }
        float elapsed = 0f;

        while (elapsed < dashDuration)
        {
            float delta = Mathf.Min(Time.fixedDeltaTime, dashDuration - elapsed);
            Vector2 requested = direction * (dashDistance * delta / Mathf.Max(0.01f, dashDuration));
            Vector2 step = MovementCollision2D.AllowedStep(movementCollider, requested);
            Vector2 next = (body != null ? body.position : (Vector2)transform.position) + step;
            if (body != null) body.MovePosition(next);
            else transform.position = next;
            HitDashTargets();
            elapsed += delta;
            if (step.sqrMagnitude < 0.000001f) break;
            yield return new WaitForFixedUpdate();
        }

        yield return new WaitForFixedUpdate();
        HitDashTargets();

        cooldownE = stat != null && stat.Data != null ? stat.Data.cooldownE : 1f;
        dashBusy = false;
    }

    private void HitDashTargets()
    {
        if (eDashHitBox == null || teamMember == null) return;

        Collider2D[] colliders = eDashHitBox.GetComponentsInChildren<Collider2D>(true);
        foreach (Collider2D hitBoxCollider in colliders)
        {
            if (hitBoxCollider == null) continue;
            ContactFilter2D filter = new ContactFilter2D();
            filter.SetLayerMask(dashTargetLayers);
            filter.useTriggers = true;
            Collider2D[] hits = new Collider2D[32];
            int count = hitBoxCollider.Overlap(filter, hits);
            for (int i = 0; i < count; i++)
            {
                Minion minion = hits[i] != null ? hits[i].GetComponentInParent<Minion>() : null;
                if (minion != null)
                {
                    if (minion.Team != teamMember.team && dashMinionsHit.Add(minion))
                        minion.TakeDamage(stat != null ? stat.Attack * dashDamageMultiplier : 0f, gameObject);
                    continue;
                }
                TeamMember target = hits[i] != null ? hits[i].GetComponentInParent<TeamMember>() : null;
                if (target == null || target == teamMember || !teamMember.IsEnemy(target) || !dashTargetsHit.Add(target)) continue;
                target.TakeDamage(stat != null ? stat.Attack * dashDamageMultiplier : 0f, gameObject);
            }
        }
    }

    private void SetAttackDirection(Vector2 direction)
    {
        if (animator == null) return;
        animator.SetFloat(AttackX, direction.x);
        animator.SetFloat(AttackY, direction.y);
    }

    private GameObject FindChild(string childName)
    {
        foreach (Transform child in GetComponentsInChildren<Transform>(true))
            if (child.name == childName) return child.gameObject;
        return null;
    }

    private void PlaySound(AudioClip clip)
    {
        if (sfxSource != null && clip != null) sfxSource.PlayOneShot(clip);
    }

    // AttackE()
    // Giữ E, hiện E_DashHitBox
    // Click khi giữ E, hủy chiêu
    // THả E, thực hiện dash 1 đoạn theo hướng chỉ định
    // Hướng Dash lấy theo hướng player và chuột
    // E_DashHitBox xoay theo hướng chuột với tâm ở Player
    // Khi xong chiêu thì đặt lại cooldownE lấy ở stat của nhân vật

    // AttackQ()
    // Giữ Q, hiện Q_RangeIndicator
    // Click khi giữ Q, hủy chiêu
    // THả Q, thực hiện gây sát thương trong collider mỗi 0.2s một lần
    // Chạy animation AttackQ khi dùng chiêu
    // Chiêu Q thời giay tồn tại 3s
    // Khi xong chiêu thì đặt lại cooldownQ lấy ở stat của nhân vật
}
