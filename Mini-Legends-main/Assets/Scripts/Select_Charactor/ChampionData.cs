using UnityEngine;

public enum ChampionRole { Fighter, Tank, Mage, Marksman, Assassin, Support }

[CreateAssetMenu(menuName = "Game/Champion")]
public class ChampionData : ScriptableObject
{
    public string id;
    public string displayName;
    public Sprite portrait;
    public GameObject playerPrefab;   // ví dụ FootSoldier, BladeAssasin
    public ChampionRole[] roles;
}
