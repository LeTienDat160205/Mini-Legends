using UnityEngine;

public enum Team
{
    Neutral, // Quái rừng, mục tiêu trung lập
    Blue,    // Phe Xanh
    Red      // Phe Đỏ
}

public class TeamMember : MonoBehaviour
{
    public Team team;

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