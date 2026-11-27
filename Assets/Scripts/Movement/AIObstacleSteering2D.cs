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
    [SerializeField, Min(0f)] private float progressThreshold = 0.035f;
    [SerializeField] private bool debugGizmos = true;

    private Collider2D bodyCollider;
    private int preferredSide;
    private float sideUntil;
    private float stuckTimer;
    private Vector2 lastPosition;
    private Vector2 visualDirection;
    private bool movingLastFrame;

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
        float traveled = Vector2.Distance(lastPosition, (Vector2)transform.position);
        if (movingLastFrame && traveled < progressThreshold)
            stuckTimer += Time.fixedDeltaTime;
        else
            stuckTimer = Mathf.Max(0f, stuckTimer - Time.fixedDeltaTime * 2f);

        if (stuckTimer >= stuckSeconds)
        {
            // Escape local minima by trying the opposite side.
            preferredSide = preferredSide == 0 ? -1 : -preferredSide;
            sideUntil = Time.time + sideCommitSeconds;
            stuckTimer = 0f;
        }

        lastPosition = transform.position;
        float probeDistance = Mathf.Max(stepDistance * 2f, lookAhead);
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

            int[] sideOrder = preferredSide == -1 ? new[] { -1, 1 } : new[] { 1, -1 };
            foreach (int side in sideOrder)
            {
                // Sideways movement first prevents pressing directly into broad building colliders.
                float[] angles = { 70f, 45f, 90f, 25f, 115f, 145f };
                foreach (float degrees in angles)
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
                selected = Vector2.zero;
            }
        }
        else
        {
            preferredSide = 0;
        }

        visualDirection = selected;
        Vector2 result = selected == Vector2.zero ? Vector2.zero :
            MovementCollision2D.AllowedStep(bodyCollider, selected * stepDistance);
        movingLastFrame = desiredDirection != Vector2.zero;
        return result;
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
    }
}
