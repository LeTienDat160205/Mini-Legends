
using UnityEngine;
using UnityEngine.SceneManagement;
using TMPro;
using System.Collections;

public class Nexus : MonoBehaviour
{
    [Header("Thong tin nha chinh")]
    [SerializeField] private Team nexusTeam = Team.Blue;
    [SerializeField, Min(1f)] private float maxHealth = 1000f;
    public Team NexusTeam => nexusTeam;

    [Header("Hieu ung no")]
    [SerializeField] private GameObject explosionPrefab;
    [SerializeField] private float explosionDuration = 2f;

    [Header("End Game")]
    [SerializeField] private GameObject endGamePanel;
    [SerializeField] private TMP_Text resultText;

    private float currentHealth;
    private bool destroyed;
    private static bool matchEnded;

    [RuntimeInitializeOnLoadMethod(
        RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void ResetMatchState()
    {
        matchEnded = false;
    }

    void Awake()
    {
        currentHealth = maxHealth;
    }

    public void TakeDamage(float damage)
    {
        if (damage <= 0f || destroyed || matchEnded)
            return;

        currentHealth = Mathf.Max(
            0f, currentHealth - damage
        );

        Debug.Log(
            name + " con " + currentHealth + " mau"
        );

        if (currentHealth <= 0f)
        {
            destroyed = true;
            matchEnded = true;
            StartCoroutine(ExplodeAndEndGame());
        }
    }

    IEnumerator ExplodeAndEndGame()
    {
        Debug.Log(name + " da bi pha huy!");

        // Tao hieu ung no tai vi tri Nexus
        if (explosionPrefab != null)
        {
            GameObject explosion = Instantiate(
                explosionPrefab,
                transform.position,
                Quaternion.identity
            );

            Destroy(explosion, 3f);
        }

        // An hinh nha chinh
        SpriteRenderer sprite =
            GetComponent<SpriteRenderer>();

        if (sprite != null)
            sprite.enabled = false;

        // Xac dinh ket qua tran dau
        bool victory =
            nexusTeam != ChampSelectResult.PickedTeam;

        if (victory)
            MatchAnnouncerAudio.Instance?.PlayVictory();
        else
            MatchAnnouncerAudio.Instance?.PlayDefeat();

        // Cho hieu ung no hoan tat
        yield return new WaitForSecondsRealtime(
            Mathf.Max(0f, explosionDuration)
        );

        // Hien man hinh ket qua
        if (resultText != null)
        {
            resultText.text = victory
                ? "VICTORY"
                : "DEFEAT";

            resultText.color = victory
                ? Color.yellow
                : Color.red;
        }

        if (endGamePanel != null)
            endGamePanel.SetActive(true);

        // Dung tran dau
        Time.timeScale = 0f;
    }

    // Nut choi lai
    public void RestartMatch()
    {
        Time.timeScale = 1f;
        matchEnded = false;

        SceneManager.LoadScene(
            SceneManager.GetActiveScene().buildIndex
        );
    }

    // Dung de test trong Play Mode
    [ContextMenu("Test Destroy Nexus")]
    private void TestDestroy()
    {
        if (Application.isPlaying)
            TakeDamage(maxHealth);
    }
}
