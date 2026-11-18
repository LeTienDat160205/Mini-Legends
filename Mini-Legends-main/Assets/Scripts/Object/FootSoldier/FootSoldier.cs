using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class FootSoldier : MonoBehaviour
{
    [Header("Attack")]
    [SerializeField] private GameObject rangeIndicator;
    [SerializeField] private Stat stat;
    [SerializeField] private TeamMember teamMember;
    [SerializeField] private Animator animator;
    [SerializeField] private string attackTrigger = "Attack";
    [SerializeField] private string attackSpeedParam = "AttackSpeed";   // Float param, dùng làm Speed Multiplier của state Attack
    [SerializeField] private Bullet bulletPrefab;
    [SerializeField] private Transform firePoint;                       // Nếu để trống sẽ bắn từ tâm player
    [SerializeField] private LayerMask attackTargetLayers = ~0;

    [Header("Skill E - Rapid Fire")]
    [SerializeField] private GameObject eHitBox;                        // Object 2D (sphere + collider) hiển thị hướng bắn
    [SerializeField] private Transform ePivot;                          // Trục xoay đặt ở tâm player
    [SerializeField] private string eSpeedParam = "AttackESpeed";       // Float param, Speed Multiplier của state AttackE
    [SerializeField] private float eClipLength = 1f;                    // Độ dài (giây) của clip AttackE gốc
    [SerializeField] private float eDuration = 3f;
    [SerializeField] private int eBulletCount = 9;
    [SerializeField] private float eDamageMultiplier = 1f;
    [SerializeField] private float eHitBoxAngleOffset;

    [Header("Skill Q - Big Bullet")]
    [SerializeField] private GameObject qHitBox;                        // Hình hiển thị hướng bắn Q
    [SerializeField] private Transform qPivot;                          // Trục xoay đặt ở tâm player
    [SerializeField] private float qHitBoxAngleOffset;
    [SerializeField] private float qRange = 6f;                         // Tầm chọn mục tiêu để đạn đuổi theo
    [SerializeField] private Bullet qBulletPrefab;                      // Đạn to
    [SerializeField] private float qDamageMultiplier = 3f;
    [SerializeField] private float qFireDelay = 0.2f;                   // Trễ từ lúc chạy animation tới lúc đạn bay ra
    [SerializeField] private LayerMask qTargetLayers = ~0;

    private Vector3 indicatorBaseScale;
    private Camera mainCamera;
    private readonly HashSet<int> animatorParams = new HashSet<int>();

    private static readonly int AttackX = Animator.StringToHash("AttackX");
    private static readonly int AttackY = Animator.StringToHash("AttackY");
    private static readonly int AttackE = Animator.StringToHash("AttackE");
    private static readonly int AttackQ = Animator.StringToHash("AttackQ");

    private bool aimingE;
    private bool eBusy;
    private bool aimingQ;
    private bool qBusy;
    private float attackCooldown;
    private float cooldownE;
    private float cooldownQ;
    private int ignoreAttackFrame = -1;

    // ------------------------------------------------------------------
    // Khởi tạo
    // ------------------------------------------------------------------
    private void Awake()
    {
        mainCamera = Camera.main;

        if (stat == null) stat = GetComponent<Stat>();
        if (teamMember == null) teamMember = GetComponent<TeamMember>();
        if (animator == null) animator = GetComponent<Animator>();
        if (rangeIndicator == null) rangeIndicator = FindChild("RangeIndicator");
        if (eHitBox == null) eHitBox = FindChild("E_HitBox");
        if (qHitBox == null) qHitBox = FindChild("Q_HitBox");

        if (ePivot == null)
        {
            GameObject pivotObject = FindChild("E_Pivot");
            if (pivotObject == null)
            {
                pivotObject = new GameObject("E_Pivot");
                pivotObject.transform.SetParent(transform, false);
            }
            ePivot = pivotObject.transform;
        }

        // E_Pivot nằm ở tâm player, giữ nguyên offset local của hitbox.
        if (eHitBox != null && eHitBox.transform.parent != ePivot)
            eHitBox.transform.SetParent(ePivot, false);

        if (qPivot == null)
        {
            GameObject pivotObject = FindChild("Q_Pivot");
            if (pivotObject == null)
            {
                pivotObject = new GameObject("Q_Pivot");
                pivotObject.transform.SetParent(transform, false);
            }
            qPivot = pivotObject.transform;
        }

        // Q_Pivot nằm ở tâm player, giữ nguyên offset local của hitbox.
        if (qHitBox != null && qHitBox.transform.parent != qPivot)
            qHitBox.transform.SetParent(qPivot, false);

        if (animator != null)
            foreach (AnimatorControllerParameter p in animator.parameters)
                animatorParams.Add(p.nameHash);

        if (rangeIndicator != null)
        {
            indicatorBaseScale = rangeIndicator.transform.localScale;
            rangeIndicator.SetActive(false);
        }
        if (eHitBox != null) eHitBox.SetActive(false);
        if (qHitBox != null) qHitBox.SetActive(false);
    }

    private void Update()
    {
        if (attackCooldown > 0f) attackCooldown -= Time.deltaTime;
        if (cooldownE > 0f) cooldownE -= Time.deltaTime;
        if (cooldownQ > 0f) cooldownQ -= Time.deltaTime;

        HandleEInput();
        HandleQInput();

        // Đang ngắm / đang dùng chiêu, hoặc vừa click để huỷ chiêu ở frame này -> không đánh thường.
        if (aimingE || eBusy || aimingQ || qBusy || Time.frameCount == ignoreAttackFrame)
        {
            if (rangeIndicator != null) rangeIndicator.SetActive(false);
            return;
        }

        // Giữ chuột -> hiện tầm đánh.
        bool holding = Input.GetMouseButton(0);
        if (rangeIndicator != null) rangeIndicator.SetActive(holding);
        UpdateRangeIndicator();

        // Click vào mục tiêu trong tầm.
        if (Input.GetMouseButtonDown(0))
            Attack();
    }

    // ------------------------------------------------------------------
    // Đánh thường
    // ------------------------------------------------------------------
    public void Attack()
    {
        if (aimingE || eBusy || aimingQ || qBusy) return;
        if (attackCooldown > 0f || bulletPrefab == null) return;

        TeamMember target = GetTargetUnderMouse(attackTargetLayers);
        if (target == null) return;

        float range = GetIndicatorRadius(rangeIndicator, stat != null ? stat.AttackRange : 1f);
        if (!IsInRange(target, range)) return;

        // Hướng animation + bắn đạn về phía mục tiêu.
        Vector2 direction = Bullet.GetAimPoint(target.transform) - (Vector2)transform.position;
        SetAttackDirection(direction);

        // attackSpeed càng cao -> cooldown càng ngắn, animation chạy càng nhanh.
        float attackSpeed = Mathf.Max(0.01f, stat != null ? stat.AttackSpeed : 1f);
        attackCooldown = 1f / attackSpeed;
        SetFloatSafe(attackSpeedParam, attackSpeed);
        if (animator != null) animator.SetTrigger(attackTrigger);

        FireBullet(bulletPrefab, direction, stat != null ? stat.Attack : 0f, target.transform);
    }

    private void UpdateRangeIndicator()
    {
        if (rangeIndicator == null) return;

        float attackRange = stat != null ? stat.AttackRange : 1f;
        rangeIndicator.transform.localScale = indicatorBaseScale * Mathf.Max(0.01f, attackRange);
    }

    // ------------------------------------------------------------------
    // Skill E - Nạp đạn bắn liên tục trong 3s
    // ------------------------------------------------------------------
    private void HandleEInput()
    {
        if (eBusy || qBusy || aimingQ) return;

        // Giữ E -> hiện E_HitBox.
        if (!aimingE && Input.GetKeyDown(KeyCode.E))
        {
            if (cooldownE > 0f) return;
            aimingE = true;
            if (eHitBox != null) eHitBox.SetActive(true);
            UpdateEAim();
        }

        if (!aimingE) return;

        UpdateEAim();

        // Click khi giữ E -> huỷ chiêu.
        if (Input.GetMouseButtonDown(0))
        {
            CancelEAim();
            ignoreAttackFrame = Time.frameCount;
            return;
        }

        // Thả E -> bắn.
        if (Input.GetKeyUp(KeyCode.E))
        {
            Vector2 direction = GetMouseDirection();
            if (direction.sqrMagnitude < 0.001f) direction = Vector2.right;
            direction.Normalize();

            aimingE = false;
            if (eHitBox != null) eHitBox.SetActive(false);
            StartCoroutine(PerformE(direction));
        }
    }

    private void UpdateEAim()
    {
        Vector2 direction = GetMouseDirection();
        if (direction.sqrMagnitude < 0.001f) return;

        float angle = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg;
        Transform rotateTarget = ePivot != null ? ePivot : (eHitBox != null ? eHitBox.transform : null);
        if (rotateTarget != null)
            rotateTarget.rotation = Quaternion.Euler(0f, 0f, angle + eHitBoxAngleOffset);
    }

    private void CancelEAim()
    {
        aimingE = false;
        if (eHitBox != null) eHitBox.SetActive(false);
    }

    private IEnumerator PerformE(Vector2 direction)
    {
        eBusy = true;
        SetAttackDirection(direction);

        // Kéo giãn animation để chạy đủ eDuration giây.
        float duration = Mathf.Max(0.01f, eDuration);
        SetFloatSafe(eSpeedParam, eClipLength / duration);
        if (animator != null) animator.SetTrigger(AttackE);

        int count = Mathf.Max(1, eBulletCount);
        float interval = duration / count;
        float damage = (stat != null ? stat.Attack : 0f) * eDamageMultiplier;

        for (int i = 0; i < count; i++)
        {
            FireBullet(bulletPrefab, direction, damage, null);
            yield return new WaitForSeconds(interval);
        }

        SetFloatSafe(eSpeedParam, 1f);
        cooldownE = stat != null && stat.Data != null ? stat.Data.cooldownE : 8f;
        eBusy = false;
    }

    // ------------------------------------------------------------------
    // Skill Q - Bắn 1 viên đạn TO theo hướng chuột
    // ------------------------------------------------------------------
    private void HandleQInput()
    {
        if (eBusy || qBusy || aimingE) return;

        // Giữ Q -> hiện Q_HitBox.
        if (!aimingQ && Input.GetKeyDown(KeyCode.Q))
        {
            if (cooldownQ > 0f) return;
            aimingQ = true;
            if (qHitBox != null) qHitBox.SetActive(true);
            UpdateQAim();
        }

        if (!aimingQ) return;

        UpdateQAim();

        // Click khi giữ Q -> huỷ chiêu.
        if (Input.GetMouseButtonDown(0))
        {
            CancelQAim();
            ignoreAttackFrame = Time.frameCount;
            return;
        }

        // Thả Q -> bắn đạn to.
        // Tự aim vào kẻ địch gần nhất trong tầm qRange, đạn đuổi theo mục tiêu đó.
        // Không có kẻ địch nào trong tầm -> đạn bay thẳng theo hướng chuột.
        if (Input.GetKeyUp(KeyCode.Q))
        {
            TeamMember target = FindNearestEnemy(qRange, qTargetLayers);

            Vector2 direction = target != null
                ? Bullet.GetAimPoint(target.transform) - (Vector2)transform.position
                : GetMouseDirection();
            if (direction.sqrMagnitude < 0.001f) direction = Vector2.right;
            direction.Normalize();

            aimingQ = false;
            if (qHitBox != null) qHitBox.SetActive(false);

            StartCoroutine(PerformQ(direction, target));
        }
    }

    private void UpdateQAim()
    {
        // Khi giữ Q, hitbox chĩa vào kẻ địch gần nhất; không có thì chĩa theo chuột.
        TeamMember nearest = FindNearestEnemy(qRange, qTargetLayers);
        Vector2 direction = nearest != null
            ? Bullet.GetAimPoint(nearest.transform) - (Vector2)transform.position
            : GetMouseDirection();
        if (direction.sqrMagnitude < 0.001f) return;

        float angle = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg;
        Transform rotateTarget = qPivot != null ? qPivot : (qHitBox != null ? qHitBox.transform : null);
        if (rotateTarget != null)
            rotateTarget.rotation = Quaternion.Euler(0f, 0f, angle + qHitBoxAngleOffset);
    }

    private void CancelQAim()
    {
        aimingQ = false;
        if (qHitBox != null) qHitBox.SetActive(false);
    }

    private IEnumerator PerformQ(Vector2 direction, TeamMember target)
    {
        qBusy = true;

        SetAttackDirection(direction);
        if (animator != null) animator.SetTrigger(AttackQ);

        // Chờ animation vung tay rồi mới bắn.
        if (qFireDelay > 0f) yield return new WaitForSeconds(qFireDelay);

        // Mục tiêu còn sống thì cập nhật lại hướng, nếu đã chết thì giữ hướng cũ.
        Transform homing = null;
        if (target != null)
        {
            homing = target.transform;
            direction = Bullet.GetAimPoint(homing) - (Vector2)transform.position;
        }

        float damage = (stat != null ? stat.Attack : 0f) * qDamageMultiplier;
        FireBullet(qBulletPrefab != null ? qBulletPrefab : bulletPrefab, direction, damage, homing);

        cooldownQ = stat != null && stat.Data != null ? stat.Data.cooldownQ : 5f;
        qBusy = false;
    }

    // ------------------------------------------------------------------
    // Hàm dùng chung
    // ------------------------------------------------------------------
    private void FireBullet(Bullet prefab, Vector2 direction, float damage, Transform homingTarget)
    {
        if (prefab == null) return;

        Vector3 spawnPos = firePoint != null ? firePoint.position : transform.position;
        Bullet bullet = Instantiate(prefab, spawnPos + new Vector3(0, 0.5f, 0), Quaternion.identity);
        bullet.Init(teamMember, damage, direction, homingTarget);
    }

    // Tìm TeamMember dưới con trỏ chuột, chỉ nhận kẻ địch (IsEnemy xử lý Blue/Red/Neutral).
    private TeamMember GetTargetUnderMouse(LayerMask layers)
    {
        if (teamMember == null) return null;

        Vector2 mouseWorld = GetMouseWorld();
        Collider2D hit = Physics2D.OverlapPoint(mouseWorld, layers);
        if (hit == null) return null;

        TeamMember target = hit.GetComponentInParent<TeamMember>();
        if (target == null || target == teamMember || !teamMember.IsEnemy(target)) return null;
        return target;
    }

    private bool IsInRange(TeamMember target, float radius)
    {
        float sqrDistance = (Bullet.GetAimPoint(target.transform) - (Vector2)transform.position).sqrMagnitude;
        return sqrDistance <= radius * radius;
    }

    private readonly Collider2D[] nearbyBuffer = new Collider2D[32];

    // Tìm kẻ địch gần nhất (tính tới điểm nhắm) trong bán kính radius quanh player.
    private TeamMember FindNearestEnemy(float radius, LayerMask layers)
    {
        if (teamMember == null) return null;

        ContactFilter2D filter = new ContactFilter2D();
        filter.SetLayerMask(layers);
        filter.useTriggers = true;

        int count = Physics2D.OverlapCircle(transform.position, radius, filter, nearbyBuffer);

        TeamMember nearest = null;
        float bestSqrDistance = float.MaxValue;

        for (int i = 0; i < count; i++)
        {
            Collider2D col = nearbyBuffer[i];
            if (col == null) continue;

            TeamMember candidate = col.GetComponentInParent<TeamMember>();
            if (candidate == null || candidate == teamMember || !teamMember.IsEnemy(candidate)) continue;

            float sqrDistance = (Bullet.GetAimPoint(candidate.transform) - (Vector2)transform.position).sqrMagnitude;
            if (sqrDistance > radius * radius || sqrDistance >= bestSqrDistance) continue;

            bestSqrDistance = sqrDistance;
            nearest = candidate;
        }

        return nearest;
    }

    // Bán kính của indicator hình tròn (sprite 1 đơn vị -> bán kính = scale / 2).
    // Nếu sprite của bạn không phải 1 đơn vị, hãy chỉnh lại công thức này.
    private float GetIndicatorRadius(GameObject indicator, float fallback)
    {
        if (indicator == null) return fallback;
        return indicator.transform.lossyScale.x * 0.5f;
    }

    private Vector2 GetMouseWorld()
    {
        if (mainCamera == null) mainCamera = Camera.main;
        if (mainCamera == null) return transform.position;

        Vector3 mouseScreen = Input.mousePosition;
        mouseScreen.z = Mathf.Abs(mainCamera.transform.position.z - transform.position.z);
        Vector3 mouseWorld = mainCamera.ScreenToWorldPoint(mouseScreen);
        return new Vector2(mouseWorld.x, mouseWorld.y);
    }

    private Vector2 GetMouseDirection()
    {
        return GetMouseWorld() - (Vector2)transform.position;
    }

    private void SetAttackDirection(Vector2 direction)
    {
        if (animator == null || direction.sqrMagnitude < 0.001f) return;

        direction.Normalize();
        animator.SetFloat(AttackX, direction.x);
        animator.SetFloat(AttackY, direction.y);
    }

    private void SetFloatSafe(string paramName, float value)
    {
        if (animator == null || string.IsNullOrEmpty(paramName)) return;
        int hash = Animator.StringToHash(paramName);
        if (animatorParams.Contains(hash))
            animator.SetFloat(hash, value);
    }

    private GameObject FindChild(string childName)
    {
        foreach (Transform child in GetComponentsInChildren<Transform>(true))
            if (child.name == childName) return child.gameObject;
        return null;
    }
}