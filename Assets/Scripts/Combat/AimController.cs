using UnityEngine;

public class AimController : MonoBehaviour
{
    [SerializeField] private Camera targetCamera;
    [SerializeField] private Transform owner;

    private void Awake()
    {
        if (targetCamera == null) targetCamera = Camera.main;
        if (owner == null && transform.parent != null) owner = transform.parent.parent;
    }

    private void Update()
    {
        if (targetCamera == null || owner == null) return;

        Vector3 mouseWorld = targetCamera.ScreenToWorldPoint(Input.mousePosition);
        mouseWorld.z = owner.position.z;

        Vector2 direction = mouseWorld - owner.position;
        if (direction.sqrMagnitude < 0.0001f) return;

        // Aim.up là hướng tấn công được Attack.cs sử dụng.
        transform.up = direction.normalized;
    }
}
