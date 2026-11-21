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
    [SerializeField] private float respawnDelay = 5f;

    public Team team;
    public float CurrentHeart => currentHeart;

    private void Awake()
    {
        stat = GetComponent<Stat>();
        currentHeart = stat != null ? stat.Heart : 0f;
        healthBar = GetComponentInChildren<HealthBar>(true);
        if (healthBar != null) healthBar.SetTarget(transform);
        RefreshHealthBar();
    }

    public void TakeDamage(float damage)
    {
        if (damage <= 0f || currentHeart <= 0f) return;

        Damaged?.Invoke(damage);
        currentHeart = Mathf.Max(0f, currentHeart - damage);
        RefreshHealthBar();

        if (currentHeart <= 0f)
        {
            if (GetComponent<PlayerMovement>() != null)
                StartCoroutine(RespawnAfterDelay());
            else gameObject.SetActive(false);
        }
    }

    private System.Collections.IEnumerator RespawnAfterDelay()
    {
        PlayerMovement movement = GetComponent<PlayerMovement>();
        if (movement != null) movement.enabled = false;
        yield return new WaitForSeconds(respawnDelay);
        Respawn();
        if (movement != null) movement.enabled = true;
    }

    public void Respawn()
    {
        SpawnArea spawn = SpawnArea.FindFor(team);
        if (spawn != null) spawn.PlaceFeet(transform);
        currentHeart = stat != null ? stat.Heart : 0f;
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
