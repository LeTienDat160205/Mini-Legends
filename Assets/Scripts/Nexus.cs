using UnityEngine;

public class Nexus : MonoBehaviour
{
    [SerializeField, Min(1f)] private float maxHealth = 1000f;
    private float currentHealth;

    private void Awake() => currentHealth = maxHealth;

    public void TakeDamage(float amount)
    {
        if (amount <= 0f || currentHealth <= 0f) return;
        currentHealth = Mathf.Max(0f, currentHealth - amount);
        if (currentHealth <= 0f)
        {
            Debug.Log($"{name} đã bị phá hủy.", this);
            TeamMember member = GetComponent<TeamMember>();
            if (member != null && member.team == ChampSelectResult.PickedTeam)
                MatchAnnouncerAudio.Instance?.PlayDefeat();
            else if (member != null)
                MatchAnnouncerAudio.Instance?.PlayVictory();
        }
    }
}
