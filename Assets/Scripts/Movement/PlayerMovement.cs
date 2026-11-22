using UnityEngine;

public class PlayerMovement : MonoBehaviour
{
    [SerializeField] private float speed = 1.5f;
    [SerializeField] private Rigidbody2D rb;
    [SerializeField] private Animator animator;
    [SerializeField] private Collider2D bodyCollider;

    private Vector2 moveInput;
    private Vector2 lastMoveDirection = Vector2.down;
    private BladeAssasin bladeAssasin;

    private static readonly int MoveX = Animator.StringToHash("MoveX");
    private static readonly int MoveY = Animator.StringToHash("MoveY");
    private static readonly int MoveMagnitude = Animator.StringToHash("MoveMagnitude");
    private static readonly int LastMoveX = Animator.StringToHash("LastMoveX");
    private static readonly int LastMoveY = Animator.StringToHash("LastMoveY");

    private void Awake()
    {
        if (rb == null) rb = GetComponent<Rigidbody2D>();
        if (rb != null) rb.bodyType = RigidbodyType2D.Kinematic;
        if (bodyCollider == null)
            foreach (Collider2D candidate in GetComponents<Collider2D>())
                if (!candidate.isTrigger) { bodyCollider = candidate; break; }
        if (bodyCollider == null)
            Debug.LogError("Player cần collider thân với Is Trigger tắt để bị tường chặn.", this);
        if (animator == null) animator = GetComponent<Animator>();
        if (animator == null) animator = GetComponentInChildren<Animator>();
        bladeAssasin = GetComponent<BladeAssasin>();

        // The controller has 8 directional clips and no (0,0) idle clip.
        // Initialize the parameters before the Animator evaluates its first frame.
        SetAnimationParameters(Vector2.zero, Vector2.down);
    }

    private void Update()
    {
        ReadInput();
        UpdateAnimation();
    }

    private void FixedUpdate()
    {
        if (rb == null) return;
        if (bladeAssasin != null && bladeAssasin.IsDashing) return;
        rb.linearVelocity = Vector2.zero;
        Vector2 step = MovementCollision2D.AllowedStep(bodyCollider, moveInput * speed * Time.fixedDeltaTime);
        rb.MovePosition(rb.position + step);
    }

    private void ReadInput()
    {
        Vector2 raw = new Vector2(
            Input.GetAxisRaw("Horizontal"),
            Input.GetAxisRaw("Vertical"));

        // Normalize diagonal input so all 8 directions have the same speed.
        moveInput = raw.sqrMagnitude > 1f ? raw.normalized : raw;

        if (moveInput.sqrMagnitude > 0.001f)
            lastMoveDirection = moveInput.normalized;
    }

    private void UpdateAnimation()
    {
        if (animator == null) return;

        SetAnimationParameters(moveInput, lastMoveDirection);
    }

    private void SetAnimationParameters(Vector2 movement, Vector2 facing)
    {
        if (animator == null) return;

        animator.SetFloat(MoveX, movement.x);
        animator.SetFloat(MoveY, movement.y);
        animator.SetFloat(MoveMagnitude, movement.magnitude);
        animator.SetFloat(LastMoveX, facing.x);
        animator.SetFloat(LastMoveY, facing.y);
    }
}
