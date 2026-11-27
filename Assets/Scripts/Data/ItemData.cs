using System;
using UnityEngine;

/// <summary>
/// Một món đồ gồm nhiều phẩm trong cùng một file.
/// - Phẩm 0 (gốc): các trường ngay bên dưới (giữ nguyên dữ liệu cũ).
/// - Phẩm 1, 2, 3...: danh sách Upgrade Tiers, mỗi phần tử là một phẩm cao hơn.
/// </summary>
[CreateAssetMenu(menuName = "Mini Legends/Item Data", fileName = "ItemData")]
public class ItemData : ScriptableObject
{
    [Header("Phẩm 0 (gốc)")]
    public string displayName;
    public Sprite icon;
    [TextArea] public string description;
    [Min(0)] public int gold;
    [Min(0f)] public float attack;
    [Min(0f)] public float attackSpeed;
    [Min(0f)] public float attackRange;
    [Min(0f)] public float defend;
    [Min(0f)] public float heart;
    [Min(0f)] public float speed;

    [Min(0f)] public float cooldownReduction;
    [Header("Kích hoạt")]
    public bool canActive;
    public ItemActiveEffect activeEffect;
    [Min(0f)] public float activeValue;
    [Min(0f)] public float activeDuration;
    [Min(0f)] public float activeCooldown;
    [TextArea] public string activeDescription;

    [Header("Các phẩm cao hơn (phẩm 1, 2, 3...)")]
    public ItemTier[] upgradeTiers;

    public string DisplayName => string.IsNullOrWhiteSpace(displayName) ? name : displayName;

    // Tổng số phẩm, gồm cả phẩm gốc.
    public int TierCount => 1 + (upgradeTiers != null ? upgradeTiers.Length : 0);

    // Chỉ số phẩm cao nhất (phẩm gốc là 0).
    public int MaxTierIndex => TierCount - 1;

    // Lấy dữ liệu của một phẩm. Chỉ số ngoài phạm vi sẽ được kẹp lại.
    public ItemTier GetTier(int index)
    {
        index = Mathf.Clamp(index, 0, MaxTierIndex);
        if (index > 0 && upgradeTiers != null && upgradeTiers[index - 1] != null)
            return ResolveUpgradeTier(upgradeTiers[index - 1]);

        return new ItemTier
        {
            icon = icon,
            description = description,
            gold = gold,
            attack = attack,
            attackSpeed = attackSpeed,
            attackRange = attackRange,
            defend = defend,
            heart = heart,
            speed = speed,
            cooldownReduction = cooldownReduction,
            canActive = canActive,
            activeEffect = activeEffect,
            activeValue = activeValue,
            activeDuration = activeDuration,
            activeCooldown = activeCooldown,
            activeDescription = activeDescription
        };
    }

    // Phẩm cao hơn chưa cài đặt (chỉ số = 0, mô tả trống, chưa có icon) thì lấy của phẩm 0.
    private ItemTier ResolveUpgradeTier(ItemTier t)
    {
        ItemTier baseTier = GetTier(0);
        return new ItemTier
        {
            nameOverride = t.nameOverride,
            icon = t.icon != null ? t.icon : baseTier.icon,
            description = string.IsNullOrWhiteSpace(t.description) ? baseTier.description : t.description,
            gold = t.gold > 0 ? t.gold : baseTier.gold,
            attack = t.attack > 0f ? t.attack : baseTier.attack,
            attackSpeed = t.attackSpeed > 0f ? t.attackSpeed : baseTier.attackSpeed,
            attackRange = t.attackRange > 0f ? t.attackRange : baseTier.attackRange,
            defend = t.defend > 0f ? t.defend : baseTier.defend,
            heart = t.heart > 0f ? t.heart : baseTier.heart,
            speed = t.speed > 0f ? t.speed : baseTier.speed,
            cooldownReduction = t.cooldownReduction > 0f ? t.cooldownReduction : baseTier.cooldownReduction,

            // Phẩm gốc có kích hoạt thì các phẩm sau mặc định cũng có.
            canActive = t.canActive || baseTier.canActive,
            // Các giá trị kích hoạt để trống thì lấy của phẩm gốc.
            activeEffect = t.activeEffect != ItemActiveEffect.None ? t.activeEffect : baseTier.activeEffect,
            activeValue = t.activeValue > 0f ? t.activeValue : baseTier.activeValue,
            activeDuration = t.activeDuration > 0f ? t.activeDuration : baseTier.activeDuration,
            activeCooldown = t.activeCooldown > 0f ? t.activeCooldown : baseTier.activeCooldown,
            activeDescription = string.IsNullOrWhiteSpace(t.activeDescription) ? baseTier.activeDescription : t.activeDescription
        };
    }

    // Tên hiển thị của một phẩm: dùng tên riêng của phẩm đó nếu có, không thì dùng tên gốc.
    public string GetDisplayName(int index)
    {
        if (index > 0 && upgradeTiers != null && index - 1 < upgradeTiers.Length)
        {
            ItemTier tier = upgradeTiers[index - 1];
            if (tier != null && !string.IsNullOrWhiteSpace(tier.nameOverride))
                return tier.nameOverride;
        }
        return DisplayName;
    }
}

// Dữ liệu của một phẩm cao hơn (cùng các trường với phẩm gốc).
[Serializable]
public class ItemTier
{
    [Tooltip("Để trống = dùng tên gốc của món")]
    public string nameOverride;
    public Sprite icon;
    [TextArea] public string description;
    [Min(0)] public int gold;

    [Min(0f)] public float attack;
    [Min(0f)] public float attackSpeed;
    [Min(0f)] public float attackRange;
    [Min(0f)] public float defend;
    [Min(0f)] public float heart;
    [Min(0f)] public float speed;

    [Min(0f)] public float cooldownReduction;
    [Header("Kích hoạt")]
    public bool canActive;
    public ItemActiveEffect activeEffect;
    [Min(0f)] public float activeValue;
    [Min(0f)] public float activeDuration;
    [Min(0f)] public float activeCooldown;

    [TextArea] public string activeDescription;
}

public enum ItemActiveEffect
{
    None,
    Heal,
    Shield,
    SpeedBoost,
    Damage
}
