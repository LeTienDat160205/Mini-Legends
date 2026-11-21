using UnityEngine;

public class Stat : MonoBehaviour
{
    [SerializeField] private CharacterStatData data;
    public CharacterStatData Data => data;
    public float Attack => data != null ? data.attack : 0f;
    public float AttackSpeed => data != null ? data.attackSpeed : 0f;
    public float AttackRange => data != null ? data.attackRange : 0f;
    public float Defend => data != null ? data.defend : 0f;
    public float Heart => data != null ? data.heart : 0f;
    public float Speed => data != null ? data.speed : 0f;
}
