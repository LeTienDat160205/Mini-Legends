using System;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "ARAMMenuCatalog", menuName = "ARAM/Menu Catalog")]
public class ARAMMenuCatalog : ScriptableObject
{
    [Serializable]
    public class ChampionInfo
    {
        public string displayName;
        public string role;
        [TextArea(2, 5)] public string description;
        public Sprite portrait;
        public string attackStyle;
        [TextArea(1, 3)] public string skillQ;
        [TextArea(1, 3)] public string skillE;
    }

    [Serializable]
    public class ItemInfo
    {
        public string displayName;
        public int price;
        public Sprite icon;
        [TextArea(2, 5)] public string description;
        [TextArea(1, 3)] public string stats;
    }

    public List<ChampionInfo> champions = new List<ChampionInfo>();
    public List<ItemInfo> items = new List<ItemInfo>();
}
