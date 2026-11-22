using UnityEngine;

public static class MovementCollision2D
{
    private const float Skin = 0.02f;
    private static readonly RaycastHit2D[] Hits = new RaycastHit2D[32];

    public static Vector2 AllowedStep(Collider2D bodyCollider, Vector2 step, Minion movingMinion = null)
    {
        if (bodyCollider == null || step.sqrMagnitude < 0.000001f) return step;

        float distance = step.magnitude;
        Vector2 direction = step / distance;
        ContactFilter2D filter = new ContactFilter2D();
        filter.useTriggers = false;
        filter.useLayerMask = false;
        int count = bodyCollider.Cast(direction, filter, Hits, distance + Skin);
        float allowed = distance;
        for (int i = 0; i < count; i++)
        {
            Collider2D other = Hits[i].collider;
            if (other == null || other.isTrigger || other.transform.root == bodyCollider.transform.root) continue;
            allowed = Mathf.Min(allowed, Mathf.Max(0f, Hits[i].distance - Skin));
        }
        return direction * allowed;
    }
}
