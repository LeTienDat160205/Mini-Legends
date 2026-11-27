using UnityEngine;

/// <summary>
/// Adapter used by Stage-3 champion AI. Turrets reuse TeamMember health and CombatDrone;
/// Inhibitors own health here; Nexus forwards damage to existing Nexus.cs.
/// Does NOT fire projectiles or create a second turret health pool.
/// </summary>
[DisallowMultipleComponent]
public class ARAMCombatStructure : MonoBehaviour
{
    public enum Team { Blue, Red }
    public enum StructureType { Turret, Inhibitor, Nexus, HealthRelic }

    [Header("Identity")]
    public Team team = Team.Blue;
    public StructureType structureType = StructureType.Turret;

    [Header("Inhibitor health ONLY (Turret uses TeamMember; Nexus uses Nexus.cs)")]
    [Min(1f)] public float maxHealth = 600f;
    [SerializeField] private float currentHealth;

    [Header("Objective progression")]
    [SerializeField] private bool invulnerableOverride;
    [Tooltip("Previous objectives of the SAME team that must be destroyed first.")]
    [SerializeField] private ARAMCombatStructure[] prerequisites;

    [Header("Inhibitor destroyed visuals")]
    [SerializeField] private GameObject destructionEffect;
    [SerializeField] private bool hideSpriteOnDestroyed = true;
    [SerializeField] private bool disableBodyColliderOnDestroyed = true;

    private Nexus nexus;
    private TeamMember turretTeamMember;
    private SpriteRenderer visual;
    private bool destroyed;

    public bool isDestroyed
    {
        get
        {
            if (destroyed) return true;
            if (structureType == StructureType.Turret)
                return turretTeamMember != null && turretTeamMember.CurrentHeart <= 0f;
            return structureType == StructureType.Inhibitor && currentHealth <= 0f;
        }
    }

    public bool isInvulnerable
    {
        get
        {
            if (invulnerableOverride) return true;
            if (prerequisites == null) return false;
            foreach (ARAMCombatStructure previous in prerequisites)
                if (previous != null && !previous.isDestroyed) return true;
            return false;
        }
    }

    public float CurrentHealth => structureType == StructureType.Turret && turretTeamMember != null
        ? turretTeamMember.CurrentHeart : currentHealth;

    private void Awake()
    {
        currentHealth = Mathf.Max(1f, maxHealth);
        nexus = GetComponent<Nexus>();
        turretTeamMember = GetComponent<TeamMember>();
        visual = GetComponent<SpriteRenderer>();

        if (structureType == StructureType.Turret && turretTeamMember == null)
            Debug.LogError("Turret requires existing TeamMember component for health.", this);
        if (structureType == StructureType.Nexus && nexus == null)
            Debug.LogError("Nexus requires existing Nexus.cs for health and endgame.", this);

        if (structureType == StructureType.Turret && turretTeamMember != null)
        {
            global::Team expected = team == Team.Blue ? global::Team.Blue : global::Team.Red;
            if (turretTeamMember.team != expected)
                Debug.LogWarning("ARAMCombatStructure Team differs from Turret TeamMember.team. Fix Inspector!", this);
        }
    }

    public void TakeDamage(float damage) => TakeDamage(damage, null);

    public void TakeDamage(float damage, GameObject attacker)
    {
        if (damage <= 0f || isDestroyed || isInvulnerable) return;
        switch (structureType)
        {
            case StructureType.Turret:
                if (turretTeamMember != null)
                    turretTeamMember.TakeDamage(damage, attacker);
                break;
            case StructureType.Nexus:
                if (nexus != null) nexus.TakeDamage(damage);
                break;
            case StructureType.Inhibitor:
                currentHealth = Mathf.Max(0f, currentHealth - damage);
                if (currentHealth <= 0f) DestroyInhibitor();
                break;
        }
    }

    private void DestroyInhibitor()
    {
        if (destroyed) return;
        destroyed = true;
        if (destructionEffect != null)
            Instantiate(destructionEffect, transform.position, Quaternion.identity);
        if (hideSpriteOnDestroyed && visual != null) visual.enabled = false;
        if (disableBodyColliderOnDestroyed)
        {
            foreach (Collider2D c in GetComponentsInChildren<Collider2D>())
                if (!c.isTrigger) c.enabled = false;
        }
        Debug.Log($"[ARAM] {team} Inhibitor destroyed", this);
        // Keep adapter enabled so objectives referencing it can query isDestroyed.
        // Hide minimap marker separately instead of disabling the entire GameObject.
        MinimapEntity icon = GetComponent<MinimapEntity>();
        if (icon != null) icon.enabled = false;
    }
}
