using UnityEngine;

[CreateAssetMenu(menuName = "Mini Legends/Item Data", fileName = "ItemData")]
public class ItemData : ScriptableObject
{
    public string displayName;
    public Sprite icon;
    [TextArea] public string description;
    [Min(0f)] public float attack = 10f;
    [Min(0.01f)] public float attackSpeed = 1f;
    [Min(0f)] public float attackRange = 2f;
    [Min(0f)] public float defend = 5f;
    [Min(1f)] public float heart = 100f;
    [Min(0f)] public float speed = 1.5f;

    [Min(0f)] public float cooldownReduction = 1f;
    [Header("Kích hoạt")]
    public bool canActive;
    public ItemActiveEffect activeEffect;
    [Min(0f)] public float activeValue;
    [Min(0f)] public float activeDuration;
    [Min(0f)] public float activeCooldown;
    [TextArea] public string activeDescription;

    public string DisplayName => string.IsNullOrWhiteSpace(displayName) ? name : displayName;
}

public enum ItemActiveEffect
{
    None,
    Heal,
    Shield,
    SpeedBoost,
    Damage
}
