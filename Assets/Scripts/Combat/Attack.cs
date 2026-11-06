using UnityEngine;

public class Attack : MonoBehaviour
{
    [SerializeField] private Animator animator;
    [SerializeField] private Transform aim;
    [SerializeField] private MeleeAttack meleeAttack;
    [SerializeField] private CharacterStats characterStats;
    [SerializeField] private CharacterMovement movement;
    [SerializeField] private float cooldown = 0.35f;

    private static readonly int AttackTrigger = Animator.StringToHash("Attack");
    private static readonly int AttackX = Animator.StringToHash("AttackX");
    private static readonly int AttackY = Animator.StringToHash("AttackY");
    private float nextAttackTime;

    private void Awake()
    {
        if (animator == null) animator = GetComponentInChildren<Animator>();
        if (aim == null) aim = transform.Find("Combat/Aim");
        if (meleeAttack == null) meleeAttack = GetComponentInChildren<MeleeAttack>();
        if (characterStats == null) characterStats = GetComponent<CharacterStats>();
        if (movement == null) movement = GetComponent<CharacterMovement>();
        ApplyAttackRange();
        DisableHitbox();
    }

    private void Update()
    {
        if (Input.GetMouseButtonDown(0)) TryAttack();
    }

    public void TryAttack()
    {
        if (animator == null || Time.time < nextAttackTime) return;
        nextAttackTime = Time.time + cooldown;
        ApplyAttackRange();

        Vector2 direction = GetAimDirection();
        movement?.SetFacingDirection(direction);
        movement?.SetMovementLocked(true);
        animator.SetFloat(AttackX, direction.x);
        animator.SetFloat(AttackY, direction.y);
        meleeAttack?.ShowAttackRange();
        animator.SetTrigger(AttackTrigger);
    }

    private Vector2 GetAimDirection()
    {
        if (aim != null && aim.up.sqrMagnitude > 0.001f)
            return aim.up.normalized;
        return Vector2.down;
    }

    public void EnableHitbox() => meleeAttack?.EnableHitbox();
    public void DisableHitbox() => meleeAttack?.DisableHitbox();

    public void EndAttack()
    {
        meleeAttack?.DisableHitbox();
        movement?.SetMovementLocked(false);
    }

    private void ApplyAttackRange()
    {
        if (meleeAttack == null) return;
        meleeAttack.SetAttackRange(characterStats != null ? characterStats.AttackRange : 2f);
    }
}
