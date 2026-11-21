using UnityEngine;
using UnityEngine.Events;

public class PlayerHealth : MonoBehaviour
{
    [SerializeField] private Stat stat;
    float maxHealth;
    float currentHealth;

    public HealthBar healthBar;
    public UnityEvent onDeath;

    public void OnEnable()
    {
        onDeath.AddListener(Death);
    }

    public void OnDisable()
    {
        onDeath.RemoveListener(Death);
    }
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        if (stat == null) stat = GetComponent<Stat>();
        maxHealth = stat != null ? stat.Heart : 0f;
        currentHealth = maxHealth;
        if (healthBar != null) healthBar.UpdateBar(currentHealth, maxHealth);
    }

    public void TakeDamage(int damage)
    {
        currentHealth -= damage;
        if(currentHealth <= 0)
        {
            currentHealth = 0;
            onDeath.Invoke();
        }
        if (healthBar != null) healthBar.UpdateBar(currentHealth, maxHealth);
    }

    public void Death()
    {
        Destroy(gameObject);
    }
}
