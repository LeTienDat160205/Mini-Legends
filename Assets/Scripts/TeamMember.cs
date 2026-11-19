using UnityEngine;

public enum Team
{
    Neutral, // Quái rừng, mục tiêu trung lập
    Blue,    // Phe Xanh
    Red      // Phe Đỏ
}

public class TeamMember : MonoBehaviour
{
    private Stat stat;
    private float currentHeart;

    public Team team;
    public float CurrentHeart => currentHeart;

    private void Awake()
    {
        stat = GetComponent<Stat>();
        currentHeart = stat != null ? stat.Heart : 0f;
    }

    public void TakeDamage(float damage)
    {
        if (damage <= 0f || currentHeart <= 0f) return;

        currentHeart = Mathf.Max(0f, currentHeart - damage);

        if (currentHeart <= 0f)
            gameObject.SetActive(false);
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
