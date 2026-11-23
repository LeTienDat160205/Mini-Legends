using System.Collections;
using UnityEngine;

public class GoblinScout : MonoBehaviour
{
    // =================================
    // REFERENCES
    // =================================

    [Header("References")]
    [SerializeField] private GameObject rangeIndicator;
    [SerializeField] private Stat stat;
    [SerializeField] private TeamMember teamMember;
    [SerializeField] private Animator animator;
    [SerializeField] private Rigidbody2D body;
    [SerializeField] private CapsuleCollider2D playerCollider;

    // =================================
    // AUDIO
    // =================================

    [Header("Audio")]
    [SerializeField] private AudioSource sfxSource;
    [SerializeField] private AudioClip attackSoud;
    [SerializeField] private AudioClip skillESound;
    [SerializeField] private AudioClip skillQSound;
    [SerializeField] private AudioClip skillUnavailableSound;

    // =================================
    // NORMAL ATTACK
    // =================================

    [Header("Normal Attack")]
    [SerializeField] private string attackTrigger = "Attack";

    private float attackCooldown;
    private float cooldownTimer;
    private Vector3 indicatorBaseScale;

    // =================================
    // SKILL E
    // =================================

    [Header("Skill E")]
    [SerializeField] private GameObject attackE;
    [SerializeField] private GameObject eDamageHitBox;

    private bool aimingE;
    private bool castingE;
    private float cooldownE;
    private Vector2 eDirection = Vector2.right;

    // =================================
    // SKILL Q
    // =================================

    [Header("Skill Q")]
    [SerializeField] private GameObject attackQ;
    [SerializeField] private GameObject qRangeIndicator;
    [SerializeField] private GameObject qHitBox;
    [SerializeField] private GameObject qDamageHitBox;
    [SerializeField] private float jumpRange = 5f;
    [SerializeField] private float jumpDuration = 0.4f;
    [SerializeField] private float jumpHeight = 1.5f;
    [SerializeField] private LayerMask ignoreDuringQ;
    [SerializeField] private LayerMask structureLayer;
    [SerializeField] private float landingCheckStep = 0.1f;
    [SerializeField] private float landingCheckRadius = 0.3f;
    [SerializeField] private int maxLandingAttempts = 30;
    private bool aimingQ;
    private bool castingQ;
    private float cooldownQ;
    private Vector2 qTargetPosition;

    // =================================
    // ANIMATOR PARAMETERS
    // =================================

    private static readonly int AttackX = Animator.StringToHash("AttackX");
    private static readonly int AttackY = Animator.StringToHash("AttackY");
    private static readonly int AttackE = Animator.StringToHash("AttackE");
    private static readonly int AttackQ = Animator.StringToHash("AttackQ");

    // =================================
    // UNITY METHODS
    // =================================

    private void Awake()
    {
        if (stat == null)
            stat = GetComponent<Stat>();

        if (teamMember == null)
            teamMember = GetComponent<TeamMember>();

        if (animator == null)
            animator = GetComponent<Animator>();

        if (body == null)
            body = GetComponent<Rigidbody2D>();

        if (sfxSource == null)
            sfxSource = GetComponent<AudioSource>();

        if (sfxSource == null)
            sfxSource = gameObject.AddComponent<AudioSource>();

        sfxSource.playOnAwake = false;
        sfxSource.spatialBlend = 0f;

        if (rangeIndicator == null)
            rangeIndicator = FindChild("RangeIndicator");

        if (attackE == null)
            attackE = FindChild("AttackE");

        if (eDamageHitBox == null)
            eDamageHitBox = FindChild("E_DamageHitBox");

        if (playerCollider == null)
            playerCollider = GetComponent<CapsuleCollider2D>();

        if (attackQ == null)
            attackQ = FindChild("AttackQ");

        if (qRangeIndicator == null)
            qRangeIndicator = FindChild("Q_RangeIndicator");

        if (qHitBox == null)
            qHitBox = FindChild("Q_HitBox");

        if (qDamageHitBox == null)
            qDamageHitBox = FindChild("Q_DamageHitBox");

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

    private void Update()
    {
        if (cooldownTimer > 0f)
            cooldownTimer -= Time.deltaTime;

        if (cooldownE > 0f)
            cooldownE -= Time.deltaTime;

        if (cooldownQ > 0f)
            cooldownQ -= Time.deltaTime;

        HandleSkillInput();

        if (aimingE)
            UpdateEAim();

        if (aimingQ)
            UpdateQAim();

        if (aimingE || aimingQ || castingE || castingQ)
        {
            if (rangeIndicator != null)
                rangeIndicator.SetActive(false);

            return;
        }

        bool aimingNormalAttack = Input.GetMouseButton(0);

        if (rangeIndicator != null)
            rangeIndicator.SetActive(aimingNormalAttack);

        UpdateRangeIndicator();

        if (Input.GetMouseButtonDown(0))
            Attack();
    }

    // =================================
    // NORMAL ATTACK
    // =================================

    private void Attack()
    {
        if (cooldownTimer > 0f || stat == null)
            return;

        TeamMember target = GetTargetUnderMouse();

        if (target == null)
            return;

        if (teamMember == null || !teamMember.IsEnemy(target))
            return;

        float distance = Vector2.Distance(
            transform.position,
            target.transform.position
        );

        if (distance > stat.AttackRange)
            return;

        SetAttackDirection(target.transform.position);
        animator?.SetTrigger(attackTrigger);
        PlaySound(attackSoud);

        // Tạm thời giữ cách gây sát thương đánh thường như bản hiện tại.
        target.SendMessageUpwards(
            "TakeDamage",
            stat.Attack,
            SendMessageOptions.DontRequireReceiver
        );

        attackCooldown = stat.AttackSpeed > 0f
            ? 1f / stat.AttackSpeed
            : 1f;

        cooldownTimer = attackCooldown;
    }

    // =================================
    // SKILL INPUT
    // =================================

    private void HandleSkillInput()
    {
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
                UpdateQAim();
            }
        }

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

        if (Input.GetKeyUp(KeyCode.E) && aimingE)
            PerformE();

        if (Input.GetKeyUp(KeyCode.Q) && aimingQ)
            PerformQ();
    }

    // =================================
    // SKILL E - RECTANGULAR AIM
    // =================================

    private void UpdateEAim()
    {
        if (attackE == null || Camera.main == null)
            return;

        Vector2 direction = GetMouseWorldPosition() - (Vector2)transform.position;

        if (direction.sqrMagnitude < 0.001f)
            return;

        eDirection = direction.normalized;

        float angle = Mathf.Atan2(eDirection.y, eDirection.x) * Mathf.Rad2Deg;

        // AttackE là tâm xoay; E_Indicator là object con được offset sang phải trong Inspector.
        attackE.transform.localPosition = new Vector3(0f, -0.8f, 0f);
        attackE.transform.rotation = Quaternion.Euler(0f, 0f, angle);
    }

    private void CancelE()
    {
        aimingE = false;
        SetActiveIfAssigned(attackE, false);
        Debug.Log("Skill E cancelled.");
    }

    private void PerformE()
    {
        if (!aimingE)
            return;

        UpdateEAim();

        aimingE = false;
        castingE = true;

        SetActiveIfAssigned(attackE, false);
        SetAttackDirection(eDirection);
        animator?.SetTrigger(AttackE);
        PlaySound(skillESound);

        Debug.Log("Goblin Scout: Skill E!");

        // Chưa kích hoạt hitbox gây sát thương vì hệ thống damage kỹ năng chưa hoàn thiện.
        Invoke(nameof(FinishE), GetAnimationLengthOrDefault("AttackE", 1f));
    }

    private void FinishE()
    {
        castingE = false;
        SetActiveIfAssigned(eDamageHitBox, false);

        cooldownE = stat != null && stat.Data != null
            ? stat.Data.cooldownE
            : 1f;
        TriggerHUDCooldown(SkillSlot.SkillType.E, cooldownE);
    }

    // =================================
    // SKILL Q - JUMP AIM
    // =================================

    private void UpdateQAim()
    {
        if (Camera.main == null)
            return;

        Vector2 direction = GetMouseWorldPosition() - (Vector2)transform.position;
        direction = Vector2.ClampMagnitude(direction, jumpRange);

        Vector2 targetPosition = (Vector2)transform.position + direction;

        bool blocked = false;

        if (playerCollider != null)
        {
            Vector2 checkPosition = targetPosition +
                (Vector2)playerCollider.offset;

            Vector2 checkSize = Vector2.Scale(
                playerCollider.size,
                playerCollider.transform.lossyScale
            );

            Collider2D structure = Physics2D.OverlapCapsule(
                checkPosition,
                checkSize,
                playerCollider.direction,
                playerCollider.transform.eulerAngles.z,
                structureLayer
            );

            blocked = structure != null;
        }

        if (!blocked)
        {
            qTargetPosition = targetPosition;

            if (qHitBox != null)
                qHitBox.transform.position = qTargetPosition;
        }
    }

    private void CancelQ()
    {
        aimingQ = false;

        SetActiveIfAssigned(attackQ, false);
        SetActiveIfAssigned(qRangeIndicator, false);
        SetActiveIfAssigned(qHitBox, false);

        Debug.Log("Skill Q cancelled.");
    }

    private void PerformQ()
    {
        if (!aimingQ)
            return;

        UpdateQAim();

        qTargetPosition = GetSafeQLandingPosition(qTargetPosition);

        aimingQ = false;

        SetActiveIfAssigned(attackQ, false);
        SetActiveIfAssigned(qRangeIndicator, false);
        SetActiveIfAssigned(qHitBox, false);

        Vector2 direction = qTargetPosition - (Vector2)transform.position;
        SetAttackDirection(direction);

        animator?.SetTrigger(AttackQ);
        PlaySound(skillQSound);

        Debug.Log("Goblin Scout: Skill Q!");

        StartCoroutine(JumpToTarget());
    }
    private Vector2 GetSafeQLandingPosition(Vector2 target)
    {
        if (playerCollider == null)
            return target;

        // Nếu vị trí hiện tại không bị Structure chiếm, giữ nguyên.
        if (!IsBlockedByStructure(target))
            return target;

        Vector2 direction = target - (Vector2)transform.position;

        // Thử lùi dần từ điểm Q về phía Goblin.
        for (int i = 1; i <= maxLandingAttempts; i++)
        {
            float distance = i * landingCheckStep;

            Vector2 candidate = target - direction.normalized * distance;

            if (!IsBlockedByStructure(candidate))
                return candidate;
        }

        // Nếu không tìm được vị trí trống, giữ nguyên vị trí hiện tại.
        return transform.position;
    }
    private void SetQCollision(bool ignore)
    {
        Collider2D[] ownColliders = GetComponents<Collider2D>();

        Collider2D[] targets = FindObjectsByType<Collider2D>(
            FindObjectsSortMode.None
        );

        foreach (Collider2D own in ownColliders)
        {
            foreach (Collider2D target in targets)
            {
                if (target == null || target == own)
                    continue;

                // Bỏ qua Collider thuộc chính Goblin
                if (target.transform.IsChildOf(transform))
                    continue;

                if (target.transform.root == transform.root)
                    continue;

                // Chỉ xử lý các Layer được chọn trong Ignore During Q
                if (((1 << target.gameObject.layer) & ignoreDuringQ) == 0)
                    continue;

                if (ignore)
                {
                    // Bắt đầu Q: bỏ qua va chạm
                    Physics2D.IgnoreCollision(own, target, true);
                }
                else
                {
                    // Kết thúc Q: khôi phục khi hai Collider đã tách nhau
                    StartCoroutine(
                        RestoreCollisionWhenSeparated(own, target)
                    );
                }
            }
        }
    }

    private bool IsBlockedByStructure(Vector2 position)
    {
        Vector2 checkPosition = position +
            (Vector2)playerCollider.transform.TransformVector(playerCollider.offset);

        Vector2 checkSize = Vector2.Scale(
            playerCollider.size,
            playerCollider.transform.lossyScale
        );

        Collider2D structure = Physics2D.OverlapCapsule(
            checkPosition,
            checkSize,
            playerCollider.direction,
            playerCollider.transform.eulerAngles.z,
            structureLayer
        );

        return structure != null;
    }
    private IEnumerator RestoreCollisionWhenSeparated(
    Collider2D own,
    Collider2D target)
    {
        while (own != null && target != null)
        {
            if (!Physics2D.Distance(own, target).isOverlapped)
                break;

            yield return new WaitForFixedUpdate();
        }

        if (own != null && target != null)
            Physics2D.IgnoreCollision(own, target, false);
    }
    private IEnumerator JumpToTarget()
    {
        castingQ = true;
        SetQCollision(true);

        Vector2 startPosition = transform.position;
        Vector2 endPosition = qTargetPosition;
        float elapsed = 0f;

        bool hadBody = body != null;
        bool previousSimulated = false;

        if (hadBody)
        {
            previousSimulated = body.simulated;
            body.simulated = false;
        }

        while (elapsed < jumpDuration)
        {
            elapsed += Time.deltaTime;
            float progress = Mathf.Clamp01(elapsed / jumpDuration);

            Vector2 position = Vector2.Lerp(startPosition, endPosition, progress);
            float height = Mathf.Sin(progress * Mathf.PI) * jumpHeight;

            transform.position = new Vector3(
                position.x,
                position.y + height,
                transform.position.z
            );

            yield return null;
        }

        transform.position = new Vector3(
            endPosition.x,
            endPosition.y,
            transform.position.z
        );

        if (hadBody)
        {
            body.simulated = previousSimulated;
            body.linearVelocity = Vector2.zero;
            body.angularVelocity = 0f;
            body.WakeUp();
        }
        SetQCollision(false);

        SetActiveIfAssigned(qDamageHitBox, false);
       
        castingQ = false;
        

        cooldownQ = stat != null && stat.Data != null
            ? stat.Data.cooldownQ
            : 5f;
        TriggerHUDCooldown(SkillSlot.SkillType.Q, cooldownQ);

        Debug.Log("Goblin Scout: Skill Q landed.");
    }

    // =================================
    // TARGET DETECTION
    // =================================

    private TeamMember GetTargetUnderMouse()
    {
        if (Camera.main == null)
            return null;

        Vector3 mousePosition = Camera.main.ScreenToWorldPoint(Input.mousePosition);
        Collider2D hit = Physics2D.OverlapPoint(
            new Vector2(mousePosition.x, mousePosition.y)
        );

        if (hit == null)
            return null;

        return hit.GetComponentInParent<TeamMember>();
    }

    // =================================
    // ATTACK DIRECTION
    // =================================

    private void SetAttackDirection(Vector3 targetPosition)
    {
        SetAttackDirection((Vector2)(targetPosition - transform.position));
    }

    private void SetAttackDirection(Vector2 direction)
    {
        if (animator == null || direction.sqrMagnitude < 0.001f)
            return;

        direction.Normalize();
        animator.SetFloat(AttackX, direction.x);
        animator.SetFloat(AttackY, direction.y);
    }

    // =================================
    // MOUSE POSITION
    // =================================

    private Vector2 GetMouseWorldPosition()
    {
        if (Camera.main == null)
            return transform.position;

        Vector3 screenPosition = Input.mousePosition;
        screenPosition.z = Mathf.Abs(
            Camera.main.transform.position.z - transform.position.z
        );

        Vector3 worldPosition = Camera.main.ScreenToWorldPoint(screenPosition);
        return new Vector2(worldPosition.x, worldPosition.y);
    }

    // =================================
    // RANGE INDICATOR
    // =================================

    private void UpdateRangeIndicator()
    {
        if (rangeIndicator == null || stat == null)
            return;

        rangeIndicator.transform.localScale =
            indicatorBaseScale * Mathf.Max(0.01f, stat.AttackRange);
    }

    // =================================
    // AUDIO
    // =================================

    private void PlaySound(AudioClip clip)
    {
        if (sfxSource != null && clip != null)
            sfxSource.PlayOneShot(clip);
    }

    // =================================
    // HELPERS
    // =================================

    private float GetAnimationLengthOrDefault(string stateName, float defaultLength)
    {
        if (animator == null || animator.runtimeAnimatorController == null)
            return defaultLength;

        foreach (AnimationClip clip in animator.runtimeAnimatorController.animationClips)
        {
            if (clip != null && clip.name == stateName)
                return clip.length;
        }

        return defaultLength;
    }

    private void SetActiveIfAssigned(GameObject obj, bool active)
    {
        if (obj != null)
            obj.SetActive(active);
    }

    private GameObject FindChild(string childName)
    {
        foreach (Transform child in GetComponentsInChildren<Transform>(true))
        {
            if (child.name == childName)
                return child.gameObject;
        }

        return null;
    }
    // Gửi lệnh kích hoạt hiệu ứng xoay cooldown sang HUD
    private void TriggerHUDCooldown(SkillSlot.SkillType type, float duration)
    {
        SkillSlot[] allSlots = FindObjectsByType<SkillSlot>(FindObjectsSortMode.None);
        foreach (var slot in allSlots)
        {
            if (slot.skillType == type)
            {
                slot.TriggerCooldown(duration);
            }
        }
    }
}
