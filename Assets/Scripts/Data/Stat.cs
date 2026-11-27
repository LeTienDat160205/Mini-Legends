using UnityEngine;

public class Stat : MonoBehaviour
{
    [SerializeField] private CharacterStatData data;
    private Level_Gold progression;
    public CharacterStatData Data => data;
    private int LevelsGained => progression != null ? Mathf.Max(0, progression.currentLevel - 1) : 0;

    private void Awake()
    {
        progression = GetComponent<Level_Gold>();
    }

    public float Attack => data != null ? data.attack + data.atkPerLevel * LevelsGained : 0f;
    public float AttackSpeed => data != null ? data.attackSpeed + data.atkSpdPerLevel * LevelsGained : 0f;
    public float AttackRange => data != null ? data.attackRange : 0f;
    public float Defend => data != null ? data.defend + data.defPerLevel * LevelsGained : 0f;
    public float Heart => data != null ? data.heart + data.hpPerLevel * LevelsGained : 0f;
    public float Speed => data != null ? data.speed : 0f;
}
