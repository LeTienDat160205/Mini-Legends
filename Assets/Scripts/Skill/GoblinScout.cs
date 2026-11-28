using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class GoblinScout : MonoBehaviour
{
    [Header("AI / Input")]
    [SerializeField] private bool acceptPlayerInput = true;
    public void SetPlayerInput(bool value) => acceptPlayerInput = value;
    public bool IsUsingSkill => aimingE || castingE || aimingQ || castingQ;
    public bool CanUseE => isActiveAndEnabled && cooldownE <= 0f && !IsUsingSkill &&
        teamMember != null && teamMember.CurrentHeart > 0f;
    public bool CanUseQ => isActiveAndEnabled && cooldownQ <= 0f && !IsUsingSkill &&
        teamMember != null && teamMember.CurrentHeart > 0f && playerCollider != null;

    public bool TryCastE(TeamMember victim)
    {
        if (!CanUseE || victim == null || victim.CurrentHeart <= 0f || !teamMember.IsEnemy(victim) || stat == null)
            return false;
        Vector2 direction = victim.transform.position - transform.position;
        if (direction.sqrMagnitude < 0.001f || direction.magnitude > Mathf.Max(2.5f, stat.AttackRange * 1.4f))
            return false;
        eDirection = direction.normalized;
        aimingE = true;
        PerformE(false);
        return true;
    }

    public bool TryCastQ(TeamMember victim)
    {
        if (!CanUseQ || victim == null || victim.CurrentHeart <= 0f || !teamMember.IsEnemy(victim))
            return false;
        Vector2 delta = victim.transform.position - transform.position;
        float distance = delta.magnitude;
        if (distance < 1.5f || distance > jumpRange) return false;
        Vector2 heading = delta.normalized;
        float jumpDistance = Mathf.Min(distance * 0.65f, jumpRange);
        ContactFilter2D filter = new ContactFilter2D { useTriggers = false };
        RaycastHit2D[] hits = new RaycastHit2D[16];
        int count = playerCollider.Cast(heading, filter, hits, jumpDistance);
        for (int i = 0; i < count; i++)
            if (hits[i].collider != null && hits[i].collider.transform.root != transform.root)
                return false;
        qTargetPosition = (Vector2)transform.position + heading * jumpDistance;
        aimingQ = true;
        PerformQ(false);
        return true;
    }

    [Header("References")]
    [SerializeField] private GameObject rangeIndicator;
    [SerializeField] private Stat stat;
    [SerializeField] private TeamMember teamMember;
    [SerializeField] private Animator animator;
    [SerializeField] private Rigidbody2D body;
    [SerializeField] private CapsuleCollider2D playerCollider;

    [Header("Audio")]
    [SerializeField] private AudioSource sfxSource;
    [SerializeField] private AudioClip attackSoud;
    [SerializeField] private AudioClip skillESound;
    [SerializeField] private AudioClip skillQSound;
    [SerializeField] private AudioClip skillUnavailableSound;

    [Header("Normal Attack")]
    [SerializeField] private string attackTrigger = "Attack";
    [SerializeField] private LayerMask attackTargetLayers = ~0;

    [Header("Skill E - Directional Attack")]
    [SerializeField] private GameObject attackE;
    [SerializeField] private GameObject eDamageHitBox;
    [SerializeField] private float skillEMultiplier = 1.5f;
    [SerializeField] private LayerMask enemyLayer = ~0;

    [Header("Skill Q - Jump")]
    [SerializeField] private GameObject attackQ;
    [SerializeField] private GameObject qRangeIndicator;
    [SerializeField] private GameObject qHitBox;
    [SerializeField] private GameObject qDamageHitBox;

    [SerializeField] private float jumpRange = 5f;
    [SerializeField] private float jumpDuration = 0.4f;
    [SerializeField] private float jumpHeight = 1.5f;
    [SerializeField] private float landingCheckRadius = 1.5f;
    [SerializeField] private int maxLandingAttempts = 30;
    [SerializeField] private float landingCheckStep = 0.1f;
    [SerializeField] private float skillQMultiplier = 2f;

    [Header("Q Collision")]
    [SerializeField] private LayerMask structureLayer;

    private static readonly int AttackX = Animator.StringToHash("AttackX");
    private static readonly int AttackY = Animator.StringToHash("AttackY");
    private static readonly int AttackE = Animator.StringToHash("AttackE");
    private static readonly int AttackQ = Animator.StringToHash("AttackQ");

    private readonly List<Collider2D> ignoredMinionColliders = new List<Collider2D>();
    private Collider2D[] playerColliders;

    private Collider2D[] qHitBoxColliders;
    private bool[] qHitBoxColliderStates;

    // Lưu trạng thái sprite của vòng ngắm E để ẩn đi mà không tắt cả object AttackE
    private SpriteRenderer[] eAimSprites;
    private bool[] eAimSpriteStates;

    // Buffer dùng lại cho Overlap của E (tránh cấp phát mảng mới mỗi frame)
    private readonly Collider2D[] eOverlapResults = new Collider2D[64];

    private RigidbodyType2D previousBodyType;
    private float previousGravityScale;
    private Vector3 indicatorBaseScale;

    private bool aimingE;
    private bool castingE;
    private bool aimingQ;
    private bool castingQ;

    private float attackCooldown;
    private float cooldownTimer;
    private float cooldownE;
    private float cooldownQ;

    private Vector2 eDirection = Vector2.right;
    private Vector2 qTargetPosition;

    private readonly HashSet<TeamMember> hitEnemies = new HashSet<TeamMember>();
    private readonly HashSet<Minion> hitMinions = new HashSet<Minion>();

    private void Awake()
    {
        if (stat == null) stat = GetComponent<Stat>();
        if (teamMember == null) teamMember = GetComponent<TeamMember>();
        if (animator == null) animator = GetComponent<Animator>();
        if (body == null) body = GetComponent<Rigidbody2D>();
        if (playerCollider == null) playerCollider = GetComponent<CapsuleCollider2D>();

        if (sfxSource == null) sfxSource = GetComponent<AudioSource>();
        if (sfxSource == null) sfxSource = gameObject.AddComponent<AudioSource>();

        sfxSource.playOnAwake = false;
        sfxSource.spatialBlend = 0f;

        if (rangeIndicator == null) rangeIndicator = FindChild("RangeIndicator");
        if (attackE == null) attackE = FindChild("AttackE");
        if (eDamageHitBox == null) eDamageHitBox = FindChild("E_DamageHitBox");
        if (attackQ == null) attackQ = FindChild("AttackQ");
        if (qRangeIndicator == null) qRangeIndicator = FindChild("Q_RangeIndicator");
        if (qHitBox == null) qHitBox = FindChild("Q_HitBox");
        if (qDamageHitBox == null) qDamageHitBox = FindChild("Q_DamageHitBox");

        if (rangeIndicator != null)
        {
            indicatorBaseScale = rangeIndicator.transform.localScale;
            rangeIndicator.SetActive(false);
        }

        SetActiveIfAssigned(attackE, false);
        SetActiveIfAssigned(eDamageHitBox, false);
        SetActiveIfAssigned(attackQ, false);
        SetActiveIfAssigned(qRangeIndicator, false);
        SetActiveIfAssigned(qHitBox, false);
        SetActiveIfAssigned(qDamageHitBox, false);
    }

    private void OnEnable()
    {
        // Đảm bảo vật lý luôn bật khi script được bật lại (ví dụ lúc hồi sinh)
        if (body != null) body.simulated = true;
    }

    private void OnDisable()
    {
        // Nếu chết giữa lúc đang nhảy Q thì trả lại trạng thái vật lý
        if (castingQ && body != null)
        {
            body.gravityScale = previousGravityScale;
            body.bodyType = previousBodyType;
        }

        StopAllCoroutines();

        // Trả lại va chạm với lính vì coroutine RestoreMinionCollisions đã bị dừng
        if (playerColliders != null)
        {
            foreach (Collider2D minionCollider in ignoredMinionColliders)
            {
                if (minionCollider == null) continue;
                foreach (Collider2D playerCol in playerColliders)
                {
                    if (playerCol != null)
                        Physics2D.IgnoreCollision(playerCol, minionCollider, false);
                }
            }
        }
        ignoredMinionColliders.Clear();

        aimingE = false;
        castingE = false;
        aimingQ = false;
        castingQ = false;

        RestoreEAimVisual();
        RestoreQHitBoxCollision();

        // ĐÃ XÓA: body.simulated = previousBodySimulated;
        // (biến đó chưa bao giờ được gán nên luôn là false -> tắt vật lý của Goblin sau khi chết)

        SetActiveIfAssigned(rangeIndicator, false);
        SetActiveIfAssigned(attackE, false);
        SetActiveIfAssigned(eDamageHitBox, false);
        SetActiveIfAssigned(attackQ, false);
        SetActiveIfAssigned(qRangeIndicator, false);
        SetActiveIfAssigned(qHitBox, false);
        SetActiveIfAssigned(qDamageHitBox, false);

        hitEnemies.Clear();
        hitMinions.Clear();
    }

    private void Update()
    {
        if (cooldownTimer > 0f) cooldownTimer -= Time.deltaTime;
        if (cooldownE > 0f) cooldownE -= Time.deltaTime;
        if (cooldownQ > 0f) cooldownQ -= Time.deltaTime;
        if (!acceptPlayerInput) return;

        HandleSkillInput();

        if (aimingE) UpdateEAim();
        if (aimingQ) UpdateQAim();

        // Chặn hoàn toàn việc gọi Attack khi đang ngắm hoặc thực hiện chiêu E/Q
        if (aimingE || castingE || aimingQ || castingQ)
        {
            if (rangeIndicator != null)
                rangeIndicator.SetActive(false);

            return;
        }

        bool aimingAttack = Input.GetMouseButton(0);

        if (rangeIndicator != null)
            rangeIndicator.SetActive(aimingAttack);

        UpdateRangeIndicator();

        if (Input.GetMouseButtonDown(0))
            Attack();
    }

    private void Attack()
    {
        if (stat == null || teamMember == null ||
            teamMember.CurrentHeart <= 0f ||
            cooldownTimer > 0f)
            return;

        hitEnemies.Clear();
        hitMinions.Clear();

        Vector2 center = rangeIndicator != null
            ? (Vector2)rangeIndicator.transform.position
            : (Vector2)transform.position;

        SpriteRenderer sprite = rangeIndicator != null
            ? rangeIndicator.GetComponent<SpriteRenderer>()
            : null;

        Vector2 radii = sprite != null && sprite.sprite != null
            ? (Vector2)sprite.bounds.extents
            : Vector2.one * stat.AttackRange;

        if (radii.x <= 0f || radii.y <= 0f)
            return;

        Collider2D[] hits = Physics2D.OverlapCircleAll(
            center,
            Mathf.Max(radii.x, radii.y),
            attackTargetLayers
        );

        foreach (Collider2D hit in hits)
        {
            if (hit == null) continue;

            Vector2 closest = hit.ClosestPoint(center) - center;
            float ellipseDistance =
                closest.x * closest.x / (radii.x * radii.x) +
                closest.y * closest.y / (radii.y * radii.y);

            if (ellipseDistance > 1f) continue;

            TryDamageTarget(hit.gameObject, stat.Attack);
        }

        if (hitEnemies.Count == 0 && hitMinions.Count == 0)
            return;

        float attackSpeed = stat.AttackSpeed > 0f ? stat.AttackSpeed : 1f;
        attackCooldown = 1f / attackSpeed;
        cooldownTimer = attackCooldown;

        SetAttackDirection(GetMouseDirection());

        if (animator != null)
            animator.SetFloat(Animator.StringToHash("AttackSpeed"), attackSpeed);

        animator?.SetTrigger(attackTrigger);
        PlaySound(attackSoud);
    }

    public bool TryAttack(TeamMember target)
    {
        if (target == null || teamMember == null || !teamMember.IsEnemy(target) ||
            target.CurrentHeart <= 0f || !CanBasicAttackTarget(target.transform)) return false;
        BeginBasicAttack(target.transform.position);
        hitEnemies.Clear();
        hitMinions.Clear();
        TryDamageTarget(target.gameObject, stat.Attack);
        return true;
    }

    public bool TryAttack(Minion target)
    {
        if (target == null || !target.isActiveAndEnabled || teamMember == null ||
            target.Team == teamMember.team || !CanBasicAttackTarget(target.transform)) return false;
        BeginBasicAttack(target.transform.position);
        hitEnemies.Clear();
        hitMinions.Clear();
        TryDamageTarget(target.gameObject, stat.Attack);
        return true;
    }

    public bool TryAttack(ARAMCombatStructure target)
    {
        if (target == null || target.isDestroyed || target.isInvulnerable || teamMember == null ||
            target.team == (teamMember.team == Team.Blue ? ARAMCombatStructure.Team.Blue : ARAMCombatStructure.Team.Red) ||
            !CanBasicAttackTarget(target.transform)) return false;
        BeginBasicAttack(target.transform.position);
        target.TakeDamage(stat.Attack, gameObject);
        return true;
    }

    private bool CanBasicAttackTarget(Transform target)
    {
        if (stat == null || teamMember == null || teamMember.CurrentHeart <= 0f ||
            cooldownTimer > 0f || IsUsingSkill || target == null) return false;
        Collider2D collider = target.GetComponentInChildren<Collider2D>();
        Vector2 aim = collider != null ? collider.ClosestPoint(transform.position) : (Vector2)target.position;
        return Vector2.Distance(transform.position, aim) <= stat.AttackRange;
    }

    private void BeginBasicAttack(Vector3 worldPosition)
    {
        SetAttackDirection((Vector2)(worldPosition - transform.position));
        animator?.SetTrigger(attackTrigger);
        PlaySound(attackSoud);
        float speed = stat.AttackSpeed > 0f ? stat.AttackSpeed : 1f;
        animator?.SetFloat("AttackSpeed", speed);
        attackCooldown = 1f / speed;
        cooldownTimer = attackCooldown;
    }

    private void HandleSkillInput()
    {
        // 1. Nhấn phím E để bắt đầu ngắm
        if (Input.GetKeyDown(KeyCode.E) &&
            !aimingE && !aimingQ && !castingE && !castingQ)
        {
            if (cooldownE > 0f)
            {
                PlaySound(skillUnavailableSound);
            }
            else
            {
                aimingE = true;
                SetActiveIfAssigned(attackE, true);
                UpdateEAim();
            }
        }

        // 2. Nhấn phím Q để bắt đầu ngắm
        if (Input.GetKeyDown(KeyCode.Q) &&
            !aimingE && !aimingQ && !castingE && !castingQ)
        {
            if (cooldownQ > 0f)
            {
                PlaySound(skillUnavailableSound);
            }
            else
            {
                aimingQ = true;
                SetActiveIfAssigned(attackQ, true);
                SetActiveIfAssigned(qRangeIndicator, true);
                SetActiveIfAssigned(qHitBox, true);
                DisableQHitBoxCollision();
                UpdateQAim();
            }
        }

        // 3. Click chuột trái khi đang giữ ngắm E hoặc Q -> HỦY CHIÊU (Không bị đánh thường)
        if (Input.GetMouseButtonDown(0))
        {
            if (aimingE)
            {
                CancelE();
                return;
            }

            if (aimingQ)
            {
                CancelQ();
                return;
            }
        }

        // 4. Thả phím E hoặc Q -> THỰC HIỆN CHIÊU
        if (Input.GetKeyUp(KeyCode.E) && aimingE)
            PerformE();

        if (Input.GetKeyUp(KeyCode.Q) && aimingQ)
            PerformQ();
    }

    private void UpdateEAim()
    {
        if (attackE == null || Camera.main == null)
            return;

        eDirection = GetMouseDirection();
        if (eDirection.sqrMagnitude < 0.001f) return;

        eDirection.Normalize();
        float angle = Mathf.Atan2(eDirection.y, eDirection.x) * Mathf.Rad2Deg;

        attackE.transform.localPosition = new Vector3(0f, -0.8f, 0f);
        attackE.transform.rotation = Quaternion.Euler(0f, 0f, angle);
    }

    private void CancelE()
    {
        aimingE = false;
        SetActiveIfAssigned(attackE, false);
    }

    private void PerformE(bool aimFromMouse = true)
    {
        if (!aimingE) return;

        if (aimFromMouse) UpdateEAim();
        else if (attackE != null)
            attackE.transform.rotation = Quaternion.Euler(0f, 0f,
                Mathf.Atan2(eDirection.y, eDirection.x) * Mathf.Rad2Deg);
        aimingE = false;
        castingE = true;

        SetAttackDirection(eDirection);

        animator?.SetTrigger(AttackE);
        PlaySound(skillESound);

        // FIX: KHÔNG tắt AttackE nữa (nếu E_DamageHitBox là con của AttackE thì hitbox
        // sẽ không hoạt động). Chỉ ẩn hình ảnh vòng ngắm, giữ object active.
        HideEAimVisual();

        // Kích hoạt coroutine quét sát thương liên tục trong suốt thời gian animation E chạy
        StartCoroutine(ExecuteEDamageRoutine());
    }

    // Ẩn sprite của vòng ngắm nhưng giữ nguyên object (và hitbox con) ở trạng thái active
    private void HideEAimVisual()
    {
        if (attackE == null) return;

        eAimSprites = attackE.GetComponentsInChildren<SpriteRenderer>(true);
        eAimSpriteStates = new bool[eAimSprites.Length];

        for (int i = 0; i < eAimSprites.Length; i++)
        {
            SpriteRenderer sr = eAimSprites[i];
            if (sr == null) continue;

            eAimSpriteStates[i] = sr.enabled;

            // Giữ lại sprite thuộc hitbox sát thương (hiệu ứng chém nếu có)
            if (eDamageHitBox != null && sr.transform.IsChildOf(eDamageHitBox.transform))
                continue;

            sr.enabled = false;
        }
    }

    private void RestoreEAimVisual()
    {
        if (eAimSprites == null || eAimSpriteStates == null) return;

        int count = Mathf.Min(eAimSprites.Length, eAimSpriteStates.Length);
        for (int i = 0; i < count; i++)
        {
            if (eAimSprites[i] != null)
                eAimSprites[i].enabled = eAimSpriteStates[i];
        }

        eAimSprites = null;
        eAimSpriteStates = null;
    }

    private IEnumerator ExecuteEDamageRoutine()
    {
        // Nếu hitbox KHÔNG phải con của AttackE thì tự đặt vị trí/hướng theo hướng ngắm
        if (eDamageHitBox != null && attackE != null &&
            !eDamageHitBox.transform.IsChildOf(attackE.transform))
        {
            float angle = Mathf.Atan2(eDirection.y, eDirection.x) * Mathf.Rad2Deg;
            eDamageHitBox.transform.localPosition = new Vector3(0f, -0.8f, 0f);
            eDamageHitBox.transform.rotation = Quaternion.Euler(0f, 0f, angle);
        }

        // Bật hitbox gây sát thương của E
        SetActiveIfAssigned(eDamageHitBox, true);

        hitEnemies.Clear();
        hitMinions.Clear();

        float duration = GetAnimationLengthOrDefault("AttackE", 0.3f);
        float elapsed = 0f;

        // Quét va chạm liên tục mỗi frame trong thời gian animation diễn ra để đảm bảo nhận diện trúng mục tiêu
        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            ApplyEDamageFrame();
            yield return null;
        }

        SetActiveIfAssigned(eDamageHitBox, false);

        // Khôi phục sprite vòng ngắm rồi mới tắt hẳn AttackE cho lần dùng sau
        RestoreEAimVisual();
        SetActiveIfAssigned(attackE, false);

        castingE = false;

        cooldownE = stat != null && stat.Data != null ? stat.Data.cooldownE : 1f;
        TriggerHUDCooldown(SkillSlot.SkillType.E, cooldownE);
    }

    private void ApplyEDamageFrame()
    {
        if (eDamageHitBox == null || !eDamageHitBox.activeInHierarchy) return;

        Collider2D[] hitBoxes = eDamageHitBox.GetComponentsInChildren<Collider2D>(true);

        ContactFilter2D filter = new ContactFilter2D();
        filter.SetLayerMask(enemyLayer);
        filter.useLayerMask = true;
        filter.useTriggers = true;

        float damage = stat != null ? stat.Attack * skillEMultiplier : 10f;

        foreach (Collider2D hitBox in hitBoxes)
        {
            if (hitBox == null || !hitBox.enabled) continue;

            int count = hitBox.Overlap(filter, eOverlapResults);

            for (int i = 0; i < count; i++)
            {
                if (eOverlapResults[i] == null) continue;
                TryDamageTarget(eOverlapResults[i].gameObject, damage);
            }
        }
    }

    private void UpdateQAim()
    {
        if (Camera.main == null) return;

        Vector2 mouseWorld = GetMouseWorldPosition();
        Vector2 direction = mouseWorld - (Vector2)transform.position;
        float distance = direction.magnitude;

        if (distance > jumpRange)
            direction = direction.normalized * jumpRange;

        qTargetPosition = (Vector2)transform.position + direction;

        if (qHitBox != null)
            qHitBox.transform.position = qTargetPosition;
    }

    private void CancelQ()
    {
        aimingQ = false;
        SetActiveIfAssigned(attackQ, false);
        SetActiveIfAssigned(qRangeIndicator, false);
        SetActiveIfAssigned(qHitBox, false);
        RestoreQHitBoxCollision();
    }

    private void PerformQ(bool aimFromMouse = true)
    {
        if (!aimingQ) return;

        if (aimFromMouse) UpdateQAim();
        aimingQ = false;

        SetActiveIfAssigned(attackQ, false);
        SetActiveIfAssigned(qRangeIndicator, false);
        SetActiveIfAssigned(qHitBox, false);
        RestoreQHitBoxCollision();

        Vector2 direction = qTargetPosition - (Vector2)transform.position;
        SetAttackDirection(direction);

        animator?.SetTrigger(AttackQ);
        PlaySound(skillQSound);

        StartCoroutine(JumpToTarget());
    }

    private void DisableQHitBoxCollision()
    {
        if (qHitBox == null) return;

        qHitBoxColliders = qHitBox.GetComponentsInChildren<Collider2D>(true);
        qHitBoxColliderStates = new bool[qHitBoxColliders.Length];

        for (int i = 0; i < qHitBoxColliders.Length; i++)
        {
            Collider2D hitBoxCollider = qHitBoxColliders[i];
            if (hitBoxCollider == null) continue;

            qHitBoxColliderStates[i] = hitBoxCollider.enabled;
            hitBoxCollider.enabled = false;
        }
    }

    private void RestoreQHitBoxCollision()
    {
        if (qHitBoxColliders == null || qHitBoxColliderStates == null) return;

        int count = Mathf.Min(qHitBoxColliders.Length, qHitBoxColliderStates.Length);
        for (int i = 0; i < count; i++)
        {
            Collider2D hitBoxCollider = qHitBoxColliders[i];
            if (hitBoxCollider == null) continue;

            hitBoxCollider.enabled = qHitBoxColliderStates[i];
        }

        qHitBoxColliders = null;
        qHitBoxColliderStates = null;
    }

    private void ResolveStructureOverlap()
    {
        if (playerCollider == null || structureLayer.value == 0) return;

        ContactFilter2D filter = new ContactFilter2D();
        filter.SetLayerMask(structureLayer);
        filter.useTriggers = false;

        Collider2D[] overlaps = new Collider2D[32];
        const int maxResolveIterations = 12;
        const float separationPadding = 0.03f;

        for (int iteration = 0; iteration < maxResolveIterations; iteration++)
        {
            Physics2D.SyncTransforms();
            int count = playerCollider.Overlap(filter, overlaps);
            if (count == 0) break;

            bool moved = false;
            for (int i = 0; i < count; i++)
            {
                Collider2D structure = overlaps[i];
                if (structure == null || !structure.enabled ||
                    !structure.gameObject.activeInHierarchy ||
                    structure.transform.root == transform.root)
                    continue;

                ColliderDistance2D distance = Physics2D.Distance(playerCollider, structure);
                if (!distance.isOverlapped) continue;

                Vector2 pushDirection = -distance.normal;
                if (pushDirection.sqrMagnitude < 0.0001f)
                    pushDirection = (Vector2)playerCollider.bounds.center - (Vector2)structure.bounds.center;
                if (pushDirection.sqrMagnitude < 0.0001f)
                    pushDirection = Vector2.right;

                pushDirection.Normalize();
                float pushDistance = Mathf.Max(-distance.distance, 0.01f) + separationPadding;
                Vector2 newPosition = (Vector2)transform.position + pushDirection * pushDistance;

                transform.position = new Vector3(newPosition.x, newPosition.y, transform.position.z);
                if (body != null) body.position = newPosition;

                moved = true;
                Physics2D.SyncTransforms();
            }

            if (!moved) break;
        }

        qTargetPosition = transform.position;
    }

    private IEnumerator JumpToTarget()
    {
        castingQ = true;
        Vector2 startPosition = transform.position;
        Vector2 endPosition = qTargetPosition;
        float elapsed = 0f;

        IgnoreMinionCollisions();
        bool hadBody = body != null;

        if (hadBody)
        {
            previousBodyType = body.bodyType;
            previousGravityScale = body.gravityScale;
            body.linearVelocity = Vector2.zero;
            body.angularVelocity = 0f;
            body.gravityScale = 0f;
            body.bodyType = RigidbodyType2D.Kinematic;
        }

        while (elapsed < jumpDuration)
        {
            elapsed += Time.deltaTime;
            float progress = Mathf.Clamp01(elapsed / Mathf.Max(0.01f, jumpDuration));
            Vector2 position = Vector2.Lerp(startPosition, endPosition, progress);
            float height = Mathf.Sin(progress * Mathf.PI) * jumpHeight;

            transform.position = new Vector3(position.x, position.y + height, transform.position.z);
            yield return null;
        }

        transform.position = new Vector3(endPosition.x, endPosition.y, transform.position.z);

        if (hadBody)
        {
            body.position = endPosition;
            body.linearVelocity = Vector2.zero;
            body.angularVelocity = 0f;
        }

        Physics2D.SyncTransforms();
        ResolveStructureOverlap();

        if (hadBody)
        {
            body.position = transform.position;
            body.linearVelocity = Vector2.zero;
            body.angularVelocity = 0f;
            body.gravityScale = previousGravityScale;
            body.bodyType = previousBodyType;
            body.WakeUp();
        }

        ApplyQLandingDamage();

        yield return new WaitForSeconds(0.15f);
        SetActiveIfAssigned(qDamageHitBox, false);
        castingQ = false;

        StartCoroutine(RestoreMinionCollisions());

        cooldownQ = stat != null && stat.Data != null ? stat.Data.cooldownQ : 5f;
        TriggerHUDCooldown(SkillSlot.SkillType.Q, cooldownQ);
    }

    private void ApplyQLandingDamage()
    {
        SetActiveIfAssigned(qDamageHitBox, true);
        hitEnemies.Clear();
        hitMinions.Clear();

        Collider2D[] hits = Physics2D.OverlapCircleAll(qTargetPosition, landingCheckRadius, enemyLayer);
        float damage = (stat != null ? stat.Attack : 10f) * skillQMultiplier;

        foreach (Collider2D hit in hits)
        {
            if (hit == null) continue;
            TryDamageTarget(hit.gameObject, damage);
        }
    }

    private void TryDamageTarget(GameObject targetObject, float damage)
    {
        if (targetObject == null || teamMember == null) return;

        Minion minion = targetObject.GetComponentInParent<Minion>();
        if (minion != null)
        {
            if (minion.Team != teamMember.team && hitMinions.Add(minion))
            {
                minion.TakeDamage(damage, gameObject);
                DamagePopup.Create(minion.transform.position + Vector3.up * 0.8f, damage);
            }
            return;
        }

        TeamMember enemy = targetObject.GetComponentInParent<TeamMember>();
        if (enemy == null || enemy == teamMember || !enemy.isActiveAndEnabled || enemy.CurrentHeart <= 0f)
            return;

        if (teamMember.IsEnemy(enemy) && hitEnemies.Add(enemy))
        {
            enemy.TakeDamage(damage, gameObject);
            DamagePopup.Create(enemy.transform.position + Vector3.up * 0.8f, damage);
        }
    }

    private void IgnoreMinionCollisions()
    {
        ignoredMinionColliders.Clear();
        playerColliders = GetComponentsInChildren<Collider2D>(true);
        Minion[] minions = FindObjectsByType<Minion>(FindObjectsSortMode.None);

        foreach (Minion minion in minions)
        {
            if (minion == null) continue;
            Collider2D[] minionColliders = minion.GetComponentsInChildren<Collider2D>(true);

            foreach (Collider2D minionCollider in minionColliders)
            {
                if (minionCollider == null || minionCollider.transform.root == transform.root) continue;

                foreach (Collider2D playerCollider in playerColliders)
                {
                    if (playerCollider == null || playerCollider == minionCollider) continue;
                    Physics2D.IgnoreCollision(playerCollider, minionCollider, true);
                }
                ignoredMinionColliders.Add(minionCollider);
            }
        }
    }

    private IEnumerator RestoreMinionCollisions()
    {
        yield return new WaitForFixedUpdate();

        while (ignoredMinionColliders.Count > 0)
        {
            for (int i = ignoredMinionColliders.Count - 1; i >= 0; i--)
            {
                Collider2D minionCollider = ignoredMinionColliders[i];
                if (minionCollider == null)
                {
                    ignoredMinionColliders.RemoveAt(i);
                    continue;
                }

                bool overlapping = false;
                foreach (Collider2D playerCollider in playerColliders)
                {
                    if (playerCollider == null || !playerCollider.enabled) continue;
                    ColliderDistance2D distance = Physics2D.Distance(playerCollider, minionCollider);
                    if (distance.isOverlapped) { overlapping = true; break; }
                }

                if (!overlapping)
                {
                    foreach (Collider2D playerCollider in playerColliders)
                    {
                        if (playerCollider == null) continue;
                        Physics2D.IgnoreCollision(playerCollider, minionCollider, false);
                    }
                    ignoredMinionColliders.RemoveAt(i);
                }
            }
            yield return new WaitForFixedUpdate();
        }
    }

    private Vector2 GetMouseDirection()
    {
        if (Camera.main == null) return Vector2.right;
        Vector3 mouseScreen = Input.mousePosition;
        mouseScreen.z = Mathf.Abs(Camera.main.transform.position.z - transform.position.z);
        Vector3 mouseWorld = Camera.main.ScreenToWorldPoint(mouseScreen);
        return (Vector2)(mouseWorld - transform.position);
    }

    private Vector2 GetMouseWorldPosition()
    {
        if (Camera.main == null) return transform.position;
        return GetMouseDirection() + (Vector2)transform.position;
    }

    private void SetAttackDirection(Vector2 direction)
    {
        if (animator == null || direction.sqrMagnitude < 0.001f) return;
        direction.Normalize();
        animator.SetFloat(AttackX, direction.x);
        animator.SetFloat(AttackY, direction.y);
    }

    private void UpdateRangeIndicator()
    {
        if (rangeIndicator == null || stat == null) return;
        rangeIndicator.transform.localScale = indicatorBaseScale * Mathf.Max(0.01f, stat.AttackRange);
    }

    private GameObject FindChild(string childName)
    {
        foreach (Transform child in GetComponentsInChildren<Transform>(true))
        {
            if (child.name == childName) return child.gameObject;
        }
        return null;
    }

    private void SetActiveIfAssigned(GameObject obj, bool active)
    {
        if (obj != null) obj.SetActive(active);
    }

    private void PlaySound(AudioClip clip)
    {
        if (sfxSource != null && clip != null) sfxSource.PlayOneShot(clip);
    }

    private float GetAnimationLengthOrDefault(string stateName, float defaultLength)
    {
        if (animator == null || animator.runtimeAnimatorController == null) return defaultLength;

        foreach (AnimationClip clip in animator.runtimeAnimatorController.animationClips)
        {
            if (clip != null && clip.name == stateName) return clip.length;
        }
        return defaultLength;
    }

    private void TriggerHUDCooldown(SkillSlot.SkillType type, float duration)
    {
        if (!acceptPlayerInput) return;
        SkillSlot[] allSlots = FindObjectsByType<SkillSlot>(FindObjectsSortMode.None);
        foreach (var slot in allSlots)
        {
            if (slot.skillType == type) slot.TriggerCooldown(duration);
        }
    }
}
