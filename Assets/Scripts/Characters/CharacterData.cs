using UnityEngine;

[CreateAssetMenu(menuName = "Mini Legends/Character Data", fileName = "CharacterData")]
public class CharacterData : ScriptableObject
{
    public float health = 100f;
    public float attack = 10f;
    public float defense = 5f;
    public float attackSpeed = 1f;
    public float attackRange = 2f;
    public float moveSpeed = 1.5f;
}
