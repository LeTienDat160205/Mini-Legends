using UnityEngine;

public class CharacterStats : MonoBehaviour
{
    [SerializeField] private CharacterData data;
    [Header("Character Stats")]
    public float health = 100f;
    public float attack = 10f;
    public float defense = 5f;
    public float attackSpeed = 1f;
    public float attackRange = 2f;
    public float moveSpeed = 1.5f;
    [SerializeField] private float currentHealth = 100f;

    public float AttackRange => data != null ? data.attackRange : attackRange;
    public float MaxHealth => health;
    public float CurrentHealth => currentHealth;

    private void Awake()
    {
        if (data != null)
        {
            health = data.health;
            attack = data.attack;
            defense = data.defense;
            attackSpeed = data.attackSpeed;
            attackRange = data.attackRange;
            moveSpeed = data.moveSpeed;
        }
        currentHealth = health;
    }

    public void TakeDamage(float amount)
    {
        currentHealth = Mathf.Clamp(currentHealth - Mathf.Max(0f, amount), 0f, MaxHealth);
    }
}
