using UnityEngine;

public enum Team
{
    Neutral, // Quái rừng, mục tiêu trung lập
    Blue,    // Phe Xanh
    Red      // Phe Đỏ
}

public class TeamMember : MonoBehaviour
{
    public event System.Action<float> Damaged;
    private Stat stat;
    private float currentHeart;
    private HealthBar healthBar;
    private Animator animator;
    private PlayerMovement playerMovement;
    private BladeAssasin bladeAssasin;
    private Collider2D bodyCollider;
    [SerializeField] private float respawnDelay = 5f;

    public Team team;
    public float CurrentHeart => currentHeart;

    private void Awake()
    {
        stat = GetComponent<Stat>();
        animator = GetComponent<Animator>();
        playerMovement = GetComponent<PlayerMovement>();
        bladeAssasin = GetComponent<BladeAssasin>();
        foreach (Collider2D candidate in GetComponents<Collider2D>())
            if (!candidate.isTrigger) { bodyCollider = candidate; break; }
        currentHeart = stat != null ? stat.Heart : 0f;
        healthBar = GetComponentInChildren<HealthBar>(true);
        if (healthBar != null) healthBar.SetTarget(transform);
        RefreshHealthBar();
    }

    public void TakeDamage(float damage)
    {
        TakeDamage(damage, null);
    }

    public void TakeDamage(float damage, GameObject attacker)
    {
        if (damage <= 0f || currentHeart <= 0f) return;

        Damaged?.Invoke(damage);
        currentHeart = Mathf.Max(0f, currentHeart - damage);
        RefreshHealthBar();

        if (currentHeart <= 0f)
        {
            if (attacker != null)
                attacker.GetComponent<Level_Gold>()?.GrantKillReward(gameObject);
            if (playerMovement == null) playerMovement = GetComponent<PlayerMovement>();
            if (playerMovement != null)
            {
                MatchAnnouncerAudio.Instance?.PlayPlayerDeath();
                playerMovement.enabled = false;
                if (bladeAssasin != null) bladeAssasin.enabled = false;
                if (bodyCollider != null) bodyCollider.enabled = false;
                Rigidbody2D body = GetComponent<Rigidbody2D>();
                if (body != null) body.linearVelocity = Vector2.zero;
                if (animator != null)
                {
                    animator.ResetTrigger("Attack");
                    animator.ResetTrigger("AttackE");
                    animator.ResetTrigger("AttackQ");
                    animator.SetBool("IsDead", true);
                    animator.Play("Base Layer.Dead", 0, 0f);
                }
                StartCoroutine(RespawnAfterDelay());
            }
            else
            {
                if (name.StartsWith("Turret_"))
                    MatchAnnouncerAudio.Instance?.PlayTurretDestroyed();
                gameObject.SetActive(false);
            }
        }
    }

    private System.Collections.IEnumerator RespawnAfterDelay()
    {
        yield return new WaitForSeconds(respawnDelay);
        Respawn();
    }

    public void Respawn()
    {
        if (playerMovement == null) playerMovement = GetComponent<PlayerMovement>();
        SpawnArea spawn = SpawnArea.FindFor(team);
        if (spawn != null) spawn.PlaceFeet(transform);
        currentHeart = stat != null ? stat.Heart : 0f;
        if (animator != null)
        {
            animator.SetBool("IsDead", false);
            animator.Play("Base Layer.Idle", 0, 0f);
        }
        if (bodyCollider != null) bodyCollider.enabled = true;
        if (playerMovement != null) playerMovement.enabled = true;
        if (bladeAssasin != null) bladeAssasin.enabled = true;
        RefreshHealthBar();
    }

    private void RefreshHealthBar()
    {
        if (healthBar != null)
            healthBar.UpdateBar(currentHeart, stat != null ? stat.Heart : 0f);
    }

    // kiểm tra nhanh xem đối tượng khác có phải kẻ địch không
    public bool IsEnemy(TeamMember other)
    {
        if (other == null) return false;
        // Quái Neutral là kẻ địch của cả 2 phe
        return this.team != other.team;
    }

    public bool IsAlly(TeamMember other)
    {
        if (other == null) return false;
        return this.team == other.team;
    }
}
