using UnityEngine;

public class CharacterMovement : MonoBehaviour
{
    [SerializeField] private float speed = 0.5f;
    [SerializeField] private Rigidbody2D rb;
    [SerializeField] private Animator animator;

    private Vector2 moveInput;
    private Vector2 lastMoveDirection = Vector2.down;
    private bool movementLocked;

    private static readonly int MoveX = Animator.StringToHash("MoveX");
    private static readonly int MoveY = Animator.StringToHash("MoveY");
    private static readonly int MoveMagnitude = Animator.StringToHash("MoveMagnitude");
    private static readonly int LastMoveX = Animator.StringToHash("LastMoveX");
    private static readonly int LastMoveY = Animator.StringToHash("LastMoveY");

    private void Awake()
    {
        if (rb == null) rb = GetComponent<Rigidbody2D>();
        if (animator == null) animator = GetComponent<Animator>();
        if (animator == null)
            animator = GetComponentInChildren<Animator>();
    }

    private void Update()
    {
        if (movementLocked)
        {
            moveInput = Vector2.zero;
            UpdateAnimation();
            return;
        }

        ReadMovementInput();
        UpdateAnimation();
    }

    private void FixedUpdate()
    {
        if (rb != null)
            rb.linearVelocity = moveInput * speed;
    }

    public void SetMovementLocked(bool locked)
    {
        movementLocked = locked;
        if (locked) moveInput = Vector2.zero;
        if (rb != null && locked) rb.linearVelocity = Vector2.zero;
    }

    public void SetFacingDirection(Vector2 direction)
    {
        if (direction.sqrMagnitude <= 0.001f) return;
        lastMoveDirection = direction.normalized;
        UpdateAnimation();
    }

    private void ReadMovementInput()
    {
        // Nhận đồng thời X và Y để hỗ trợ đủ 8 hướng.
        Vector2 rawInput = new Vector2(
            Input.GetAxisRaw("Horizontal"),
            Input.GetAxisRaw("Vertical"));

        // Chặn tốc độ chéo lớn hơn tốc độ ngang/dọc.
        moveInput = rawInput.sqrMagnitude > 1f ? rawInput.normalized : rawInput;

        if (moveInput.sqrMagnitude > 0.001f)
            lastMoveDirection = moveInput.normalized;
    }

    private void UpdateAnimation()
    {
        if (animator == null)
            return;

        animator.SetFloat(MoveX, moveInput.x);
        animator.SetFloat(MoveY, moveInput.y);
        animator.SetFloat(MoveMagnitude, moveInput.magnitude);
        animator.SetFloat(LastMoveX, lastMoveDirection.x);
        animator.SetFloat(LastMoveY, lastMoveDirection.y);
    }
}
