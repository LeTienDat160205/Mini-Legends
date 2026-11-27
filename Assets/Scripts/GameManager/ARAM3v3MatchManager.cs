using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Three champion types: FootSoldier, GoblinScout and BladeAssasin.
/// Reuses player spawned by SpawnArea + champion selection.
/// Each team receives one of each champion type, in any order.
/// </summary>
public class ARAM3v3MatchManager : MonoBehaviour
{
    [Header("Existing scene roots")]
    [SerializeField] private Transform blueTeamRoot;
    [SerializeField] private Transform redTeamRoot;

    [Header("All 3 different champion prefabs - exactly one of each type")]
    [SerializeField] private GameObject[] championPrefabs = new GameObject[3];

    [Header("AI route - Blue -> Red")]
    [SerializeField] private Transform[] laneWaypoints;

    [Header("Spawn formation")]
    [SerializeField, Min(0f)] private float formationSpacing = 1.25f;
    [SerializeField] private bool randomizeEnemyOrder = true;
    [SerializeField] private bool spawnOnStart = true;

    private bool spawned;

    private IEnumerator Start()
    {
        if (!spawnOnStart) yield break;
        // SpawnArea.Start spawns player and assigns Cinemachine Follow.
        yield return null;
        SpawnTeams();
    }

    [ContextMenu("Spawn 3v3 (Play Mode)")]
    public void SpawnTeams()
    {
        if (!Application.isPlaying)
        {
            Debug.LogWarning("Only spawn teams in Play Mode.", this);
            return;
        }
        if (spawned) return;

        GameObject player = SpawnArea.SpawnedPlayer;
        if (player == null)
        {
            Debug.LogError("No selected player from SpawnArea. Check ChampSelectResult and SpawnArea.", this);
            return;
        }
        TeamMember playerTeam = player.GetComponent<TeamMember>();
        if (playerTeam == null || (playerTeam.team != Team.Blue && playerTeam.team != Team.Red))
        {
            Debug.LogError("Player must have TeamMember with Blue or Red team.", player);
            return;
        }

        SpawnArea blueSpawn = SpawnArea.FindFor(Team.Blue);
        SpawnArea redSpawn = SpawnArea.FindFor(Team.Red);
        if (blueSpawn == null || redSpawn == null)
        {
            Debug.LogError("Missing Blue or Red SpawnArea with TeamMember.", this);
            return;
        }
        if (laneWaypoints == null || laneWaypoints.Length == 0)
        {
            Debug.LogError("Assign at least one lane Waypoint (Blue -> Red).", this);
            return;
        }

        if (!ValidateRoster(out int[] types)) return;
        int selectedType = GetChampionType(player);
        if (selectedType < 0)
        {
            Debug.LogError("Selected Player must have exactly one supported champion script: FootSoldier / GoblinScout / BladeAssasin.", player);
            return;
        }

        GameObject[] byType = new GameObject[3];
        for (int i = 0; i < championPrefabs.Length; i++)
            byType[types[i]] = championPrefabs[i];

        ConfigureActor(player, playerTeam.team, true, null);

        Transform[] forwardRoute = (Transform[])laneWaypoints.Clone();
        Transform[] reverseRoute = (Transform[])laneWaypoints.Clone();
        System.Array.Reverse(reverseRoute);

        List<int> alliedRoster = new List<int>(3);
        for (int type = 0; type < 3; type++)
            if (type != selectedType) alliedRoster.Add(type);

        List<int> enemyRoster = new List<int> { 0, 1, 2 };
        if (randomizeEnemyOrder)
            Shuffle(enemyRoster);

        Team allyTeam = playerTeam.team;
        Team opposingTeam = allyTeam == Team.Blue ? Team.Red : Team.Blue;
        SpawnArea allySpawn = allyTeam == Team.Blue ? blueSpawn : redSpawn;
        SpawnArea enemySpawn = opposingTeam == Team.Blue ? blueSpawn : redSpawn;
        Transform allyRoot = allyTeam == Team.Blue ? blueTeamRoot : redTeamRoot;
        Transform enemyRoot = opposingTeam == Team.Blue ? blueTeamRoot : redTeamRoot;
        Transform[] allyRoute = allyTeam == Team.Blue ? forwardRoute : reverseRoute;
        Transform[] enemyRoute = opposingTeam == Team.Blue ? forwardRoute : reverseRoute;

        // The human occupies the first slot, then the other two different champions.
        SpawnBots(alliedRoster, byType, allyTeam, allySpawn, allyRoot, allyRoute, firstSlot: -1, teamSize: 3);
        SpawnBots(enemyRoster, byType, opposingTeam, enemySpawn, enemyRoot, enemyRoute, firstSlot: 0, teamSize: 3);

        spawned = true;
        Debug.Log($"[ARAM 3v3] Ready: Player={ChampionName(selectedType)} Team={allyTeam}. Each team contains all 3 unique champions.", this);
    }

    // Identity is determined by the champion script, not name, prefab asset instance or display name.
    // This also works when ChampSelect creates a clone of a prefab.
    private static int GetChampionType(GameObject actor)
    {
        if (actor == null) return -1;
        bool foot = actor.GetComponent<FootSoldier>() != null;
        bool goblin = actor.GetComponent<GoblinScout>() != null;
        bool blade = actor.GetComponent<BladeAssasin>() != null;
        if ((foot ? 1 : 0) + (goblin ? 1 : 0) + (blade ? 1 : 0) != 1) return -1;
        return foot ? 0 : goblin ? 1 : 2;
    }

    private bool ValidateRoster(out int[] types)
    {
        types = null;
        if (championPrefabs == null || championPrefabs.Length != 3)
        {
            Debug.LogError("Champion Prefabs must have exactly 3 entries.", this);
            return false;
        }
        int[] foundTypes = new int[3];
        bool[] seen = new bool[3];
        for (int i = 0; i < 3; i++)
        {
            GameObject prefab = championPrefabs[i];
            int type = GetChampionType(prefab);
            if (type < 0 || prefab.GetComponent<TeamMember>() == null || prefab.GetComponent<Stat>() == null)
            {
                Debug.LogError($"Champion Prefabs[{i}] needs exactly one champion script, TeamMember and Stat.", this);
                return false;
            }
            if (seen[type])
            {
                Debug.LogError($"Duplicate champion type {ChampionName(type)} in inspector. Assign 3 DIFFERENT types.", this);
                return false;
            }
            seen[type] = true;
            foundTypes[i] = type;
        }
        types = foundTypes;
        return true;
    }

    private void SpawnBots(List<int> roster, GameObject[] byType, Team team, SpawnArea spawn,
        Transform teamRoot, Transform[] route, int firstSlot, int teamSize)
    {
        for (int i = 0; i < roster.Count; i++)
        {
            int type = roster[i];
            GameObject bot = Instantiate(byType[type], spawn.transform.position, Quaternion.identity);
            bot.name = $"{team}_AI_{ChampionName(type)}";
            if (teamRoot != null) bot.transform.SetParent(teamRoot, true);
            spawn.PlaceFeet(bot.transform);
            // Spread sideways around the spawn stone (but remain inside MapBoundary).
            // Human occupies center slot 1. Ally bots use slots 0 and 2.
            int slot = firstSlot < 0 ? (i == 0 ? 0 : 2) : firstSlot + i;
            float lateral = (slot - (teamSize - 1) * 0.5f) * formationSpacing;
            Vector3 preferred = bot.transform.position + new Vector3(0f, lateral, 0f);
            if (!TryFindFreeChampionSpot(bot, preferred, team, out Vector3 safePosition))
            {
                Debug.LogError($"[ARAM 3v3] No free spawn point for {bot.name}. Move the SpawnArea to an open spot on ancient platform.", bot);
                Destroy(bot);
                continue;
            }
            Rigidbody2D body = bot.GetComponent<Rigidbody2D>();
            bot.transform.position = safePosition;
            if (body != null) body.position = safePosition;
            ConfigureActor(bot, team, false, route);
        }
    }


    /// <summary>Look for collision-free spots around a champion's spawn platform.</summary>
    private static bool TryFindFreeChampionSpot(GameObject bot, Vector3 preferred, Team team, out Vector3 found)
    {
        Collider2D own = null;
        foreach (Collider2D c in bot.GetComponents<Collider2D>())
            if (c.enabled && !c.isTrigger) { own = c; break; }
        if (own == null)
        {
            found = preferred;
            return true;
        }

        Rigidbody2D rb = bot.GetComponent<Rigidbody2D>();
        Vector3 original = bot.transform.position;
        // Prefer shifting away from the Nexus into the lane, then slightly up/down.
        float forward = team == Team.Blue ? 1f : -1f;
        for (int ring = 0; ring <= 9; ring++)
        {
            int count = ring == 0 ? 1 : 12;
            for (int i = 0; i < count; i++)
            {
                float angle = count == 1 ? 0f : i * Mathf.PI * 2f / count;
                Vector3 p = preferred + new Vector3(
                    forward * ring * 0.4f * (0.5f + Mathf.Abs(Mathf.Cos(angle))),
                    ring * 0.4f * Mathf.Sin(angle), 0f);
                bot.transform.position = p;
                if (rb != null) rb.position = p;
                Physics2D.SyncTransforms();

                ContactFilter2D filter = new ContactFilter2D();
                filter.useTriggers = false;
                Collider2D[] hits = new Collider2D[32];
                int overlaps = own.Overlap(filter, hits);
                bool blocked = false;
                for (int k = 0; k < overlaps; k++)
                {
                    Collider2D other = hits[k];
                    if (other == null || other.isTrigger || !other.enabled ||
                        other.transform.root == bot.transform.root) continue;
                    blocked = true;
                    break;
                }
                if (!blocked)
                {
                    found = p;
                    return true;
                }
            }
        }
        bot.transform.position = original;
        if (rb != null) rb.position = original;
        found = original;
        return false;
    }

    private static void ConfigureActor(GameObject actor, Team team, bool human, Transform[] route)
    {
        TeamMember member = actor.GetComponent<TeamMember>();
        member.team = team;
        member.SetAIControlled(!human); // Requires TeamMember from Stage1 SceneIntegrated ZIP.

        PlayerMovement move = actor.GetComponent<PlayerMovement>();
        if (human && move == null) move = actor.AddComponent<PlayerMovement>();
        if (move != null) move.enabled = human;

        FootSoldier foot = actor.GetComponent<FootSoldier>();
        if (foot != null) foot.SetPlayerInput(human);
        GoblinScout goblin = actor.GetComponent<GoblinScout>();
        if (goblin != null) goblin.SetPlayerInput(human);
        BladeAssasin blade = actor.GetComponent<BladeAssasin>();
        if (blade != null) blade.SetPlayerInput(human);

        EnemyMovement ai = actor.GetComponent<EnemyMovement>();
        if (!human && ai == null) ai = actor.AddComponent<EnemyMovement>();
        if (ai != null)
        {
            ai.ConfigureWaypoints(route);
            ai.enabled = !human;
        }
    }

    private static void Shuffle(List<int> values)
    {
        for (int i = values.Count - 1; i > 0; i--)
        {
            int j = Random.Range(0, i + 1);
            (values[i], values[j]) = (values[j], values[i]);
        }
    }

    private static string ChampionName(int type) => type == 0 ? "FootSoldier" : type == 1 ? "GoblinScout" : "BladeAssasin";
}
