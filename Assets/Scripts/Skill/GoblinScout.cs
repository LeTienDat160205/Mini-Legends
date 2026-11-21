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
    [SerializeField] private float eRange = 2.5f;

    private bool aimingE;
    private bool castingE;
    private float cooldownE;
    private Vector2 eDirection;

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

        if (rangeIndicator == null)
            rangeIndicator = FindChild("RangeIndicator");

        if (attackE == null)
            attackE = FindChild("AttackE");

        if (eDamageHitBox == null)
            eDamageHitBox = FindChild("E_DamageHitBox");

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

        if (attackE != null)
            attackE.SetActive(false);

        if (eDamageHitBox != null)
            eDamageHitBox.SetActive(false);

        if (attackQ != null)
            attackQ.SetActive(false);

        if (qRangeIndicator != null)
            qRangeIndicator.SetActive(false);

        if (qHitBox != null)
            qHitBox.SetActive(false);

        if (qDamageHitBox != null)
            qDamageHitBox.SetActive(false);
    }

    private void Update()
    {
        // Giảm cooldown
        if (cooldownTimer > 0f)
            cooldownTimer -= Time.deltaTime;

        if (cooldownE > 0f)
            cooldownE -= Time.deltaTime;

        if (cooldownQ > 0f)
            cooldownQ -= Time.deltaTime;

        // Xử lý input kỹ năng
        HandleSkillInput();

        // Chỉ cập nhật vùng ngắm khi đang giữ/ngắm kỹ năng
        if (aimingE)
            UpdateEAim();

        if (aimingQ)
            UpdateQAim();

        // Không đánh thường khi đang ngắm hoặc thi triển kỹ năng
        if (aimingE || aimingQ || castingE || castingQ)
        {
            if (rangeIndicator != null)
                rangeIndicator.SetActive(false);

            return;
        }

        // Hiển thị tầm đánh khi giữ chuột
        bool aiming = Input.GetMouseButton(0);

        if (rangeIndicator != null)
            rangeIndicator.SetActive(aiming);

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

        Debug.Log("Goblin Scout: Normal Attack!");

        // Tạm thời chưa xử lý sát thương

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
        // Bắt đầu ngắm E
        if (Input.GetKeyDown(KeyCode.E) &&
            !aimingE && !aimingQ &&
            !castingE && !castingQ &&
            cooldownE <= 0f)
        {
            aimingE = true;

            if (attackE != null)
                attackE.SetActive(true);

            UpdateEAim();
        }

        // Bắt đầu ngắm Q
        if (Input.GetKeyDown(KeyCode.Q) &&
            !aimingE && !aimingQ &&
            !castingE && !castingQ &&
            cooldownQ <= 0f)
        {
            aimingQ = true;

            if (attackQ != null)
                attackQ.SetActive(true);

            if (qRangeIndicator != null)
                qRangeIndicator.SetActive(true);

            if (qHitBox != null)
                qHitBox.SetActive(true);

            UpdateQAim();
        }

        // Click chuột trái để hủy kỹ năng
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

        // Thả E để thực hiện kỹ năng
        if (Input.GetKeyUp(KeyCode.E) && aimingE)
            PerformE();

        // Thả Q để thực hiện kỹ năng
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

        Vector2 mousePosition = GetMouseWorldPosition();

        Vector2 direction =
            mousePosition - (Vector2)transform.position;

        if (direction.sqrMagnitude < 0.001f)
            return;

        eDirection = direction.normalized;

        float angle = Mathf.Atan2(
            eDirection.y,
            eDirection.x
        ) * Mathf.Rad2Deg;

        // Không ghi đè vị trí local đã chỉnh trong Inspector
        attackE.transform.localPosition = new Vector3(0f, -0.8f, 0f);

        attackE.transform.rotation =
            Quaternion.Euler(0f, 0f, angle);
    }

    private void CancelE()
    {
        aimingE = false;

        if (attackE != null)
            attackE.SetActive(false);

        Debug.Log("Skill E cancelled!");
    }

    private void PerformE()
    {
        if (!aimingE)
            return;

        // Cập nhật lần cuối để lấy đúng hướng tại thời điểm thả E
        UpdateEAim();

        aimingE = false;
        castingE = true;

        if (attackE != null)
            attackE.SetActive(false);

        SetAttackDirection(eDirection);

        if (animator != null)
            animator.SetTrigger(AttackE);

        Debug.Log("Goblin Scout: Attack E!");

        // Tạm thời chờ animation hoàn thành
        Invoke(nameof(FinishE), 1f);
    }

    private void FinishE()
    {
        castingE = false;

        if (eDamageHitBox != null)
            eDamageHitBox.SetActive(false);

        cooldownE = stat != null && stat.Data != null
            ? stat.Data.cooldownE
            : 1f;

        Debug.Log("Skill E finished!");
    }

    // =================================
    // SKILL Q - JUMP ATTACK
    // =================================

    private void UpdateQAim()
    {
        Vector2 mousePosition = GetMouseWorldPosition();
        Vector2 direction = mousePosition - (Vector2)transform.position;

        // Giới hạn khoảng cách nhảy
        if (direction.magnitude > jumpRange)
            direction = direction.normalized * jumpRange;

        qTargetPosition = (Vector2)transform.position + direction;

        if (qHitBox != null)
            qHitBox.transform.position = qTargetPosition;
    }

    private void CancelQ()
    {
        aimingQ = false;

        if (attackQ != null)
            attackQ.SetActive(false);

        if (qRangeIndicator != null)
            qRangeIndicator.SetActive(false);

        if (qHitBox != null)
            qHitBox.SetActive(false);

        Debug.Log("Skill Q cancelled!");
    }

    private void PerformQ()
    {
        if (!aimingQ)
            return;

        // Cập nhật lần cuối để lấy đúng vị trí tại thời điểm thả Q
        UpdateQAim();

        aimingQ = false;

        if (attackQ != null)
            attackQ.SetActive(false);

        if (qRangeIndicator != null)
            qRangeIndicator.SetActive(false);

        if (qHitBox != null)
            qHitBox.SetActive(false);

        Vector2 direction = qTargetPosition - (Vector2)transform.position;
        SetAttackDirection(direction);

        if (animator != null)
            animator.SetTrigger(AttackQ);

        Debug.Log("Goblin Scout: Attack Q!");

        StartCoroutine(JumpToTarget());
    }

    private IEnumerator JumpToTarget()
    {
        castingQ = true;

        Vector2 startPosition = transform.position;
        Vector2 endPosition = qTargetPosition;

        float elapsed = 0f;

        while (elapsed < jumpDuration)
        {
            elapsed += Time.deltaTime;

            float progress = Mathf.Clamp01(elapsed / jumpDuration);

            Vector2 position = Vector2.Lerp(
                startPosition,
                endPosition,
                progress
            );

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

        Debug.Log("Goblin Scout: Q landed!");

        if (qDamageHitBox != null)
            qDamageHitBox.SetActive(false);

        castingQ = false;

        cooldownQ = stat != null && stat.Data != null
            ? stat.Data.cooldownQ
            : 5f;

        Debug.Log("Skill Q finished!");
    }

    // =================================
    // TARGET DETECTION
    // =================================

    private TeamMember GetTargetUnderMouse()
    {
        if (Camera.main == null)
            return null;

        Vector3 mousePosition =
            Camera.main.ScreenToWorldPoint(Input.mousePosition);

        Vector2 mouseWorldPosition = mousePosition;

        Collider2D hit = Physics2D.OverlapPoint(mouseWorldPosition);

        if (hit == null)
            return null;

        return hit.GetComponentInParent<TeamMember>();
    }

    // =================================
    // ATTACK DIRECTION
    // =================================

    private void SetAttackDirection(Vector3 targetPosition)
    {
        Vector2 direction =
            (Vector2)(targetPosition - transform.position);

        SetAttackDirection(direction);
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

        Vector3 mouseScreen = Input.mousePosition;

        mouseScreen.z = Mathf.Abs(
            Camera.main.transform.position.z - transform.position.z
        );

        Vector3 mouseWorld =
            Camera.main.ScreenToWorldPoint(mouseScreen);

        return mouseWorld;
    }

    private Vector2 GetMouseDirection()
    {
        return GetMouseWorldPosition() - (Vector2)transform.position;
    }

    // =================================
    // RANGE INDICATOR
    // =================================

    private void UpdateRangeIndicator()
    {
        if (rangeIndicator == null || stat == null)
            return;

        float attackRange = stat.AttackRange;

        rangeIndicator.transform.localScale =
            indicatorBaseScale * Mathf.Max(0.01f, attackRange);
    }

    // =================================
    // COMMON
    // =================================

    private GameObject FindChild(string childName)
    {
        foreach (Transform child in GetComponentsInChildren<Transform>(true))
        {
            if (child.name == childName)
                return child.gameObject;
        }

        return null;
    }
}
