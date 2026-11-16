using UnityEngine;

[CreateAssetMenu(menuName = "Mini Legends/Character Stat Data", fileName = "CharacterData")]
public class CharacterStatData : ScriptableObject
{
    [Min(0f)] public float attack = 10f;
    [Min(0.01f)] public float attackSpeed = 1f;
    [Min(0f)] public float attackRange = 2f;
    [Min(0f)] public float defend = 5f;
    [Min(1f)] public float heart = 100f;
    [Min(0f)] public float speed = 1.5f;

    [Min(0f)] public float cooldownE = 1f;
    [Min(0f)] public float cooldownQ = 5f;
}
