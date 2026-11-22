public static class ChampSelectResult
{
    public static ChampionData PickedChampion;
    public static Team PickedTeam = Team.Blue;
    public static int PickedSlotIndex;

    public static void SetPicked(ChampionData champion, Team team, int slotIndex)
    {
        PickedChampion = champion;
        PickedTeam = team;
        PickedSlotIndex = slotIndex;
    }
}
