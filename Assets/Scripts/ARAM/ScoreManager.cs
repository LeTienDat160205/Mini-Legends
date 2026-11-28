using UnityEngine;
using TMPro;

// Đếm tỉ số theo số lần TƯỚNG (champion) hạ gục TƯỚNG của phe địch.
// Lính (Minion) và công trình (Turret, Nexus, Inhibitor) hạ gục không được tính.
public class ScoreManager : MonoBehaviour
{
    public static ScoreManager Instance { get; private set; }

    // Bao cho script khac (vi du KillFeed) khi mot TUONG ha guc mot TUONG phe dich: (nguoi ha, nguoi bi ha)
    public static event System.Action<TeamMember, TeamMember> ChampionKilled;

    [Header("UI (kéo thả vào, hoặc để trống để tự tìm theo tên)")]
    [SerializeField] private TextMeshProUGUI playerTeamScoreText; // HUD/MathInfor/PlayerTeamScore
    [SerializeField] private TextMeshProUGUI enemyTeamScoreText;  // HUD/MathInfor/EnemyTeamScore

    private int blueScore;
    private int redScore;
    private Team lastLocalTeam = Team.Neutral;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void ResetStatics() { Instance = null; }

    private void Awake()
    {
        Instance = this;
        blueScore = 0;
        redScore = 0;
        FindTextsIfMissing();
    }

    private void Start()
    {
        RefreshUI();
    }

    private void LateUpdate()
    {
        // Team của người chơi có thể được gán muộn (sau khi chọn tướng / spawn) -> tự cập nhật lại
        Team local = GetLocalTeam();
        if (local != lastLocalTeam)
            RefreshUI();
    }

    private void OnDestroy()
    {
        if (Instance == this) Instance = null;
    }

    // Gọi từ TeamMember.TakeDamage khi một TeamMember chết
    public static void RegisterChampionKill(GameObject attacker, TeamMember victim)
    {
        if (attacker == null || victim == null) return;

        TeamMember killer = attacker.GetComponentInParent<TeamMember>();
        if (killer == null || killer == victim) return;

        // Cả người hạ và người bị hạ đều phải là tướng (không tính lính, trụ, nexus...)
        if (!IsChampion(killer) || !IsChampion(victim)) return;

        // Phải khác phe, và không tính phe trung lập
        if (killer.team == victim.team) return;
        if (killer.team == Team.Neutral || victim.team == Team.Neutral) return;

        ChampionKilled?.Invoke(killer, victim);

        if (Instance != null) Instance.AddKill(killer.team);
    }

    private static bool IsChampion(TeamMember member)
    {
        return member.GetComponent<PlayerMovement>() != null ||
               member.GetComponent<EnemyMovement>() != null;
    }

    private void AddKill(Team killerTeam)
    {
        if (killerTeam == Team.Blue) blueScore++;
        else if (killerTeam == Team.Red) redScore++;
        RefreshUI();
    }

    private Team GetLocalTeam()
    {
        Team local = MinimapManager.LocalPlayerTeam;
        // Chưa xác định thì coi người chơi là phe Blue
        return local == Team.Neutral ? Team.Blue : local;
    }

    private void RefreshUI()
    {
        FindTextsIfMissing();

        Team local = GetLocalTeam();
        lastLocalTeam = MinimapManager.LocalPlayerTeam;

        int playerTeamScore = local == Team.Blue ? blueScore : redScore;
        int enemyTeamScore = local == Team.Blue ? redScore : blueScore;

        if (playerTeamScoreText != null) playerTeamScoreText.text = playerTeamScore.ToString();
        if (enemyTeamScoreText != null) enemyTeamScoreText.text = enemyTeamScore.ToString();
    }

    private void FindTextsIfMissing()
    {
        if (playerTeamScoreText != null && enemyTeamScoreText != null) return;

        TextMeshProUGUI[] all = FindObjectsByType<TextMeshProUGUI>(
            FindObjectsInactive.Include, FindObjectsSortMode.None);

        foreach (TextMeshProUGUI text in all)
        {
            if (playerTeamScoreText == null && text.name == "PlayerTeamScore")
                playerTeamScoreText = text;
            else if (enemyTeamScoreText == null && text.name == "EnemyTeamScore")
                enemyTeamScoreText = text;
        }
    }
}