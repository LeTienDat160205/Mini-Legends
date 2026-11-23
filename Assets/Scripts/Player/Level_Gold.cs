using System;
using System.Collections;
using UnityEngine;

public class Level_Gold : MonoBehaviour
{
    [Header("Level and experience")]
    [Min(1)] public int maxLevel = 15;
    [Min(1)] public int currentLevel = 1;
    [Min(1)] public int xpForLevelUp = 100;
    [Min(0)] public int currentXP;
    [Min(0), SerializeField] private int xpIncreasePerLevel = 50;

    [Header("Gold")]
    [Min(0), SerializeField] private int currentGold;
    public int CurrentGold => currentGold;

    [Header("Kill rewards: gold / XP")]
    [SerializeField] private Vector2Int minionReward = new Vector2Int(30, 60);
    [SerializeField] private Vector2Int championReward = new Vector2Int(300, 60);
    [SerializeField] private Vector2Int towerReward = new Vector2Int(150, 60);

    public event Action ProgressChanged;
    public event Action<int> LevelChanged;

    private int baseXPForLevelUp;
    private static readonly int LevelUpState = Animator.StringToHash("Base Layer.LevelUp");

    private void Awake()
    {
        maxLevel = Mathf.Max(1, maxLevel);
        currentLevel = Mathf.Clamp(currentLevel, 1, maxLevel);
        baseXPForLevelUp = Mathf.Max(1, xpForLevelUp);
        xpForLevelUp = RequiredXP(currentLevel);
        currentGold = Mathf.Max(0, currentGold);
        currentXP = currentLevel >= maxLevel ? 0 : Mathf.Clamp(currentXP, 0, xpForLevelUp - 1);
    }

    public void AddXP(int amount)
    {
        if (amount <= 0 || currentLevel >= maxLevel) return;

        currentXP = (int)Math.Min((long)currentXP + amount, int.MaxValue);
        Debug.Log($"[Level_Gold] {name} nhận {amount} XP ({currentXP}/{xpForLevelUp}), cấp {currentLevel}.", this);
        while (currentLevel < maxLevel && currentXP >= xpForLevelUp)
        {
            currentXP -= xpForLevelUp;
            currentLevel++;
            xpForLevelUp = RequiredXP(currentLevel);
            Debug.Log($"[Level_Gold] {name} LÊN CẤP {currentLevel}! XP còn {currentXP}/{xpForLevelUp}.", this);
            PlayLevelUpAnimation();
            LevelChanged?.Invoke(currentLevel);
        }

        if (currentLevel >= maxLevel) currentXP = 0;
        ProgressChanged?.Invoke();
    }

    public void AddGold(int amount)
    {
        if (amount <= 0) return;
        currentGold = (int)Math.Min((long)currentGold + amount, int.MaxValue);
        ProgressChanged?.Invoke();
    }

    public bool TrySpendGold(int amount)
    {
        if (amount < 0 || currentGold < amount) return false;
        if (amount == 0) return true;
        currentGold -= amount;
        ProgressChanged?.Invoke();
        return true;
    }

    // Call once when this character has actually earned a kill.
    // Pass the target's root GameObject, which has the gameplay tag.
    public void GrantKillReward(GameObject defeated)
    {
        if (defeated == null || defeated == gameObject) return;

        TeamMember owner = GetComponent<TeamMember>();
        TeamMember targetMember = defeated.GetComponent<TeamMember>();
        if (owner != null && targetMember != null && owner.team == targetMember.team) return;

        Minion targetMinion = defeated.GetComponent<Minion>();
        if (targetMinion != null && owner != null && owner.team == targetMinion.Team) return;

        Vector2Int reward;
        if (defeated.CompareTag("Minion")) reward = minionReward;
        else if (defeated.CompareTag("Tower") || defeated.name.StartsWith("Turret_")) reward = towerReward;
        else if (defeated.CompareTag("Enemy") || defeated.CompareTag("Player") ||
                 defeated.CompareTag("Ally")) reward = championReward;
        else return;

        AddGold(Mathf.Max(0, reward.x));
        Debug.Log($"[Level_Gold] {name} hạ {defeated.name}: +{Mathf.Max(0, reward.x)} vàng, +{Mathf.Max(0, reward.y)} XP.", this);
        AddXP(Mathf.Max(0, reward.y));
    }

    private int RequiredXP(int level)
    {
        return (int)Math.Min((long)baseXPForLevelUp +
            (long)Mathf.Max(0, xpIncreasePerLevel) * (level - 1), int.MaxValue);
    }

    private void PlayLevelUpAnimation()
    {
        if (GetComponent<BladeAssasin>() == null)
        {
            Debug.LogWarning($"[Level_Gold] {name} lên cấp nhưng không có BladeAssasin; bỏ qua animation LevelUp.", this);
            return;
        }
        TeamMember member = GetComponent<TeamMember>();
        if (member != null && member.CurrentHeart <= 0f)
        {
            Debug.LogWarning($"[Level_Gold] {name} lên cấp khi đang chết; bỏ qua animation LevelUp.", this);
            return;
        }

        Animator animator = GetComponent<Animator>();
        if (animator == null || !animator.isActiveAndEnabled)
        {
            Debug.LogWarning($"[Level_Gold] {name} thiếu Animator đang hoạt động; không thể phát LevelUp.", this);
            return;
        }
        if (!animator.HasState(0, LevelUpState))
        {
            Debug.LogWarning($"[Level_Gold] Animator của {name} không có state Base Layer.LevelUp.", this);
            return;
        }

        animator.Play(LevelUpState, 0, 0f);
        Debug.Log($"[Level_Gold] Đã yêu cầu Animator của {name} phát LevelUp.", this);
        StartCoroutine(CheckLevelUpState(animator));
    }

    private IEnumerator CheckLevelUpState(Animator animator)
    {
        yield return null;
        if (animator != null && !animator.GetCurrentAnimatorStateInfo(0).IsName("LevelUp"))
            Debug.LogWarning($"[Level_Gold] Animator của {name} đã chuyển khỏi LevelUp ngay sau khi gọi Play. Kiểm tra các transition/trigger trong Animator.", this);
    }
}
