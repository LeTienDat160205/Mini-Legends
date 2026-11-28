using UnityEngine;

/// <summary>
/// Local 2D obstacle avoidance for champions controlled by EnemyMovement.
/// Works with kinematic Rigidbody2D and MovementCollision2D.AllowedStep.
/// This is steering, not a global pathfinder: lane waypoints still need to be on walkable ground.
/// </summary>
[DisallowMultipleComponent]
public class AIObstacleSteering2D : MonoBehaviour
{
    [Header("Local obstacle avoidance")]
    [SerializeField, Min(0.25f)] private float lookAhead = 1.4f;
    [SerializeField, Range(0.4f, 0.99f)] private float freeThreshold = 0.86f;
    [SerializeField, Min(0.2f)] private float sideCommitSeconds = 1.1f;
    [SerializeField, Min(0.2f)] private float stuckSeconds = 1.2f;

    // Tỉ lệ quãng đường thực tế / quãng đường yêu cầu. Dưới mức này thì coi là "không tiến được".
    // (Thay cho progressThreshold cố định cũ, vốn bị sai khi tốc độ AI thấp.)
    [SerializeField, Range(0.05f, 0.9f)] private float progressRatio = 0.3f;

    [Header("Recovery (thoát khi bị kẹt)")]
    [SerializeField, Min(0.2f)] private float recoverSeconds = 0.7f;

    [SerializeField] private bool debugGizmos = true;

    private const int RecoverSamples = 16;
    private static readonly float[] SideAngles = { 70f, 45f, 90f, 25f, 115f, 145f };
    private static readonly int[] SidesPositiveFirst = { 1, -1 };
    private static readonly int[] SidesNegativeFirst = { -1, 1 };

    private Collider2D bodyCollider;
    private int preferredSide;
    private float sideUntil;
    private float stuckTimer;
    private Vector2 lastPosition;
    private Vector2 visualDirection;
    private bool movingLastFrame;
    private float lastStepDistance;
    private float lastCallTime;

    private Vector2 recoverDirection;
    private float recoverUntil;

    private void Awake()
    {
        Collider2D triggerFallback = null;
        foreach (Collider2D candidate in GetComponents<Collider2D>())
        {
            if (candidate == null || !candidate.enabled) continue;
            if (!candidate.isTrigger)
            {
                bodyCollider = candidate;
                break;
            }
            if (triggerFallback == null) triggerFallback = candidate;
        }
        // Existing champion prefabs use trigger body colliders. Cast can still use
        // their shape to detect solid obstacles before moving the Rigidbody2D.
        if (bodyCollider == null) bodyCollider = triggerFallback;
        lastPosition = transform.position;
    }

    private void OnEnable() => ResetNavigation();

    public void ResetNavigation()
    {
        preferredSide = 0;
        sideUntil = 0f;
        stuckTimer = 0f;
        movingLastFrame = false;
        lastPosition = transform.position;
        visualDirection = Vector2.zero;
        lastStepDistance = 0f;
        lastCallTime = 0f;
        recoverDirection = Vector2.zero;
        recoverUntil = 0f;
    }

    /// <summary>Returns a collision-clamped step. Call only from FixedUpdate.</summary>
    public Vector2 GetSafeStep(Vector2 desiredDirection, float stepDistance)
    {
        if (bodyCollider == null || desiredDirection.sqrMagnitude < 0.00001f || stepDistance <= 0f)
        {
            movingLastFrame = false;
            lastPosition = transform.position;
            visualDirection = Vector2.zero;
            return Vector2.zero;
        }

        desiredDirection.Normalize();

        // Nếu AI đã ngừng gọi một lúc (đang đánh, đứng yên...) thì bắt đầu đo lại từ đầu,
        // tránh tính nhầm quãng đường cũ thành "không tiến được".
        if (Time.time - lastCallTime > Time.fixedDeltaTime * 4f)
        {
            movingLastFrame = false;
            stuckTimer = 0f;
        }
        lastCallTime = Time.time;

        float probeDistance = Mathf.Max(stepDistance * 2f, lookAhead);

        // Phát hiện kẹt: so quãng đường thực tế với quãng đường đã yêu cầu ở lần trước.
        float traveled = Vector2.Distance(lastPosition, (Vector2)transform.position);
        bool noProgress = movingLastFrame && traveled < lastStepDistance * progressRatio;
        if (noProgress)
            stuckTimer += Time.fixedDeltaTime;
        else
            stuckTimer = Mathf.Max(0f, stuckTimer - Time.fixedDeltaTime * 2f);

        if (stuckTimer >= stuckSeconds)
        {
            // Thoát khỏi kẹt: đổi bên ưu tiên và chủ động đi theo hướng thông thoáng nhất một lúc.
            stuckTimer = 0f;
            preferredSide = preferredSide == 0 ? -1 : -preferredSide;
            sideUntil = Time.time + sideCommitSeconds;
            StartRecovery(desiredDirection, stepDistance, probeDistance);
        }

        lastPosition = transform.position;
        lastStepDistance = stepDistance;
        movingLastFrame = true;

        // Đang trong chế độ thoát kẹt: đi theo hướng đã chọn, bỏ qua mục tiêu tạm thời.
        if (Time.time < recoverUntil && recoverDirection != Vector2.zero)
        {
            Vector2 recoverStep = MovementCollision2D.AllowedStep(bodyCollider, recoverDirection * stepDistance);
            visualDirection = recoverDirection;
            if (recoverStep.sqrMagnitude > 0.000001f)
                return recoverStep;

            // Hướng thoát cũng bị chặn -> bỏ recovery, quay lại logic bình thường
            recoverUntil = 0f;
        }

        float aheadFree = FreeDistance(desiredDirection, probeDistance);
        Vector2 selected = desiredDirection;

        // Prefer a straight route when there is sufficient clearance AND we are not
        // currently committing to detouring around a structure.
        if (aheadFree < probeDistance * freeThreshold ||
            (preferredSide != 0 && Time.time < sideUntil))
        {
            float bestScore = float.NegativeInfinity;
            Vector2 bestDirection = Vector2.zero;
            int chosenSide = preferredSide;

            int[] sideOrder = preferredSide == -1 ? SidesNegativeFirst : SidesPositiveFirst;
            foreach (int side in sideOrder)
            {
                // Sideways movement first prevents pressing directly into broad building colliders.
                foreach (float degrees in SideAngles)
                {
                    Vector2 dir = Rotate(desiredDirection, side * degrees);
                    float clear = FreeDistance(dir, probeDistance);
                    float immediate = FreeDistance(dir, Mathf.Max(0.15f, stepDistance * 1.5f));
                    if (immediate < stepDistance * 0.45f || clear < stepDistance * 1.1f)
                        continue;

                    float clearanceScore = Mathf.Clamp01(clear / probeDistance) * 2f;
                    float goalScore = Vector2.Dot(dir, desiredDirection) * 0.8f;
                    float keepSide = side == preferredSide ? 0.65f : 0f;
                    float score = clearanceScore + goalScore + keepSide;
                    if (score > bestScore)
                    {
                        bestScore = score;
                        bestDirection = dir;
                        chosenSide = side;
                    }
                }
            }

            if (bestDirection != Vector2.zero)
            {
                selected = bestDirection;
                if (preferredSide != chosenSide || sideUntil <= Time.time)
                    sideUntil = Time.time + sideCommitSeconds;
                preferredSide = chosenSide;
            }
            else
            {
                // If completely boxed in, remain still rather than entering a building.
                // (Sau stuckSeconds, StartRecovery sẽ tìm hướng thoát.)
                selected = Vector2.zero;
            }
        }
        else
        {
            preferredSide = 0;
        }

        visualDirection = selected;
        return selected == Vector2.zero
            ? Vector2.zero
            : MovementCollision2D.AllowedStep(bodyCollider, selected * stepDistance);
    }

    // Quét 16 hướng quanh người, chọn hướng thông thoáng nhất (ưu tiên đi ngang/lùi hơn là đâm thẳng vào vật cản).
    private void StartRecovery(Vector2 desired, float stepDistance, float probeDistance)
    {
        float bestScore = float.NegativeInfinity;
        Vector2 best = Vector2.zero;

        for (int i = 0; i < RecoverSamples; i++)
        {
            Vector2 dir = Rotate(desired, i * (360f / RecoverSamples));
            float clear = FreeDistance(dir, probeDistance);
            if (clear < stepDistance * 1.5f) continue;

            float score = Mathf.Clamp01(clear / probeDistance) * 2f
                          - Mathf.Abs(Vector2.Dot(dir, desired)) * 0.4f;
            if (score > bestScore)
            {
                bestScore = score;
                best = dir;
            }
        }

        if (best == Vector2.zero)
        {
            // Bị bao kín hoàn toàn (ví dụ bị lính vây): đợi một chút rồi thử lại sớm hơn
            recoverDirection = Vector2.zero;
            recoverUntil = 0f;
            stuckTimer = stuckSeconds * 0.6f;
            return;
        }

        recoverDirection = best;
        recoverUntil = Time.time + recoverSeconds;

        // Sau khi thoát, tiếp tục vòng về phía bên đã chọn để không quay lại đúng chỗ kẹt
        float cross = desired.x * best.y - desired.y * best.x;
        if (Mathf.Abs(cross) > 0.2f)
        {
            preferredSide = cross > 0f ? 1 : -1;
            sideUntil = Time.time + sideCommitSeconds;
        }
    }

    private float FreeDistance(Vector2 direction, float length)
    {
        Vector2 allowed = MovementCollision2D.AllowedStep(bodyCollider, direction * length);
        return allowed.magnitude;
    }

    private static Vector2 Rotate(Vector2 direction, float degrees)
    {
        float radians = degrees * Mathf.Deg2Rad;
        float c = Mathf.Cos(radians);
        float s = Mathf.Sin(radians);
        return new Vector2(direction.x * c - direction.y * s,
                           direction.x * s + direction.y * c);
    }

    private void OnDrawGizmosSelected()
    {
        if (!debugGizmos || !Application.isPlaying) return;
        Gizmos.color = Color.green;
        Gizmos.DrawRay(transform.position, visualDirection * lookAhead);

        if (Time.time < recoverUntil)
        {
            Gizmos.color = Color.yellow;
            Gizmos.DrawRay(transform.position, recoverDirection * lookAhead);
        }
    }
}