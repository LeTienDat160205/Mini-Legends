using System;
using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.Animations;
using UnityEngine.Playables;

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
    [SerializeField] private TMP_Text levelText;
    private SpriteRenderer effectRenderer;
    private Animator effectAnimator;
    private PlayableGraph effectGraph;
    private Coroutine effectRoutine;

    private void Awake()
    {
        maxLevel = Mathf.Max(1, maxLevel);
        currentLevel = Mathf.Clamp(currentLevel, 1, maxLevel);
        baseXPForLevelUp = Mathf.Max(1, xpForLevelUp);
        xpForLevelUp = RequiredXP(currentLevel);
        currentGold = Mathf.Max(0, currentGold);
        currentXP = currentLevel >= maxLevel ? 0 : Mathf.Clamp(currentXP, 0, xpForLevelUp - 1);
        HealthBar healthBar = GetComponentInChildren<HealthBar>(true);
        if (levelText == null && healthBar != null)
            levelText = healthBar.transform.Find("LevelText")?.GetComponent<TMP_Text>();
        RefreshLevelText();
    }

    public void AddXP(int amount)
    {
        if (amount <= 0 || currentLevel >= maxLevel) return;

        currentXP = (int)Math.Min((long)currentXP + amount, int.MaxValue);
        Debug.Log($"[Level_Gold] {name} nhận {amount} XP ({currentXP}/{xpForLevelUp}), cấp {currentLevel}.", this);
        while (currentLevel < maxLevel && currentXP >= xpForLevelUp)
        {
            Stat stat = GetComponent<Stat>();
            float previousMaxHeart = stat != null ? stat.Heart : 0f;
            currentXP -= xpForLevelUp;
            currentLevel++;
            xpForLevelUp = RequiredXP(currentLevel);
            TeamMember member = GetComponent<TeamMember>();
            if (member != null && stat != null)
                member.ApplyLevelUpHeart(stat.Heart - previousMaxHeart);
            Debug.Log($"[Level_Gold] {name} LÊN CẤP {currentLevel}! XP còn {currentXP}/{xpForLevelUp}.", this);
            RefreshLevelText();
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

    // Called by the defeated unit when its health first reaches zero.
    public void GrantKillReward(GameObject defeated)
    {
        if (defeated == null || defeated == gameObject) return;

        TeamMember owner = GetComponent<TeamMember>();
        TeamMember targetMember = defeated.GetComponent<TeamMember>();
        Minion targetMinion = defeated.GetComponent<Minion>();
        if (owner == null || (targetMember == null && targetMinion == null)) return;
        if (targetMember != null && owner.team == targetMember.team) return;

        if (targetMinion != null && owner.team == targetMinion.Team) return;

        Vector2Int reward;
        if (targetMinion != null) reward = minionReward;
        else if (defeated.CompareTag("Tower") || defeated.name.StartsWith("Turret_")) reward = towerReward;
        else if (targetMember != null) reward = championReward;
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

    private void RefreshLevelText()
    {
        if (levelText != null) levelText.text = currentLevel.ToString();
    }

    private void PlayLevelUpAnimation()
    {
        TeamMember member = GetComponent<TeamMember>();
        if (member != null && member.CurrentHeart <= 0f)
        {
            Debug.LogWarning($"[Level_Gold] {name} lên cấp khi đang chết; bỏ qua animation LevelUp.", this);
            return;
        }

        Animator characterAnimator = GetComponent<Animator>();
        if (characterAnimator == null || characterAnimator.runtimeAnimatorController == null)
        {
            Debug.LogWarning($"[Level_Gold] {name} thiếu Animator hoặc controller cho hiệu ứng LevelUp.", this);
            return;
        }
        AnimationClip clip = null;
        foreach (AnimationClip candidate in characterAnimator.runtimeAnimatorController.animationClips)
            if (candidate != null && candidate.name == "LevelUp") { clip = candidate; break; }
        if (clip == null)
        {
            Debug.LogWarning($"[Level_Gold] Controller của {name} không có clip LevelUp.", this);
            return;
        }

        if (effectRenderer == null)
        {
            GameObject effect = new GameObject("LevelUpEffect");
            effect.transform.SetParent(transform, false);
            effectRenderer = effect.AddComponent<SpriteRenderer>();
            effectAnimator = effect.AddComponent<Animator>();
            SpriteRenderer characterRenderer = GetComponent<SpriteRenderer>();
            if (characterRenderer != null)
            {
                effectRenderer.sortingLayerID = characterRenderer.sortingLayerID;
                effectRenderer.sortingOrder = characterRenderer.sortingOrder + 1;
            }
        }

        if (effectRoutine != null) StopCoroutine(effectRoutine);
        if (effectGraph.IsValid()) effectGraph.Destroy();
        effectRenderer.enabled = true;
        effectGraph = PlayableGraph.Create("LevelUpEffect");
        AnimationPlayableOutput output = AnimationPlayableOutput.Create(effectGraph, "LevelUp", effectAnimator);
        output.SetSourcePlayable(AnimationClipPlayable.Create(effectGraph, clip));
        effectGraph.Play();
        effectRoutine = StartCoroutine(HideEffectAfter(clip.length));
    }

    private IEnumerator HideEffectAfter(float duration)
    {
        yield return new WaitForSeconds(duration);
        if (effectGraph.IsValid()) effectGraph.Destroy();
        if (effectRenderer != null) effectRenderer.enabled = false;
        effectRoutine = null;
    }

    private void OnDestroy()
    {
        if (effectGraph.IsValid()) effectGraph.Destroy();
    }
}
