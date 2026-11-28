using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
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
    [Tooltip("De trong: tu tao Image con 'ResultImage' o giua EndGamePanel")]
    [SerializeField] private Image resultImage;
    [SerializeField] private Sprite victorySprite;
    [SerializeField] private Sprite defeatSprite;
    [Tooltip("Kich thuoc toi da cua anh (pixel). Anh lon hon se duoc thu nho cho vua, KHONG phong to")]
    [SerializeField] private Vector2 maxImageSize = new Vector2(600f, 300f);

    [Header("Quay ve menu")]
    [Tooltip("Thoi gian hien man hinh ket qua truoc khi chuyen scene")]
    [SerializeField, Min(0f)] private float returnDelay = 4f;
    [SerializeField] private string menuSceneName = "ModeMenu_Styled";

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

        // Hien man hinh ket qua (anh Victory / Defeat)
        ShowResultPanel(victory);

        // Dung tran dau
        Time.timeScale = 0f;

        // Doi roi quay ve menu (dung thoi gian thuc vi timeScale = 0)
        yield return new WaitForSecondsRealtime(
            Mathf.Max(0f, returnDelay)
        );

        ReturnToMenu();
    }

    private void ShowResultPanel(bool victory)
    {
        if (endGamePanel == null)
        {
            Debug.LogWarning(name + ": chua gan EndGamePanel.", this);
            return;
        }

        // Dam bao canvas chua panel hien thi tren moi UI khac
        Canvas canvas = endGamePanel.GetComponentInParent<Canvas>(true);
        if (canvas != null)
            canvas.sortingOrder = Mathf.Max(canvas.sortingOrder, 100);

        // Dung Image con o giua panel (KHONG dung chinh panel) de anh khong bi keo full man hinh
        if (resultImage == null)
        {
            Transform existing = endGamePanel.transform.Find("ResultImage");
            if (existing != null) resultImage = existing.GetComponent<Image>();
        }
        if (resultImage == null)
        {
            GameObject go = new GameObject(
                "ResultImage", typeof(RectTransform), typeof(Image));
            go.transform.SetParent(endGamePanel.transform, false);

            RectTransform rect = (RectTransform)go.transform;
            rect.anchorMin = new Vector2(0.5f, 0.5f);
            rect.anchorMax = new Vector2(0.5f, 0.5f);
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.anchoredPosition = Vector2.zero;

            resultImage = go.GetComponent<Image>();
        }

        Sprite resultSprite = victory ? victorySprite : defeatSprite;
        if (resultSprite == null)
            Debug.LogWarning(
                name + ": chua gan sprite " +
                (victory ? "Victory" : "Defeat") + ".", this);

        resultImage.sprite = resultSprite;
        resultImage.color = Color.white;
        resultImage.preserveAspect = true;
        resultImage.raycastTarget = false;
        resultImage.enabled = resultSprite != null;
        FitImageSize(resultImage, resultSprite);

        resultImage.gameObject.SetActive(true);
        endGamePanel.SetActive(true);
    }

    // Dat kich thuoc anh theo sprite, thu nho neu vuot maxImageSize (giu ti le, khong phong to)
    private void FitImageSize(Image image, Sprite sprite)
    {
        if (sprite == null) return;

        Vector2 size = sprite.rect.size;
        float scale = 1f;
        if (maxImageSize.x > 0f) scale = Mathf.Min(scale, maxImageSize.x / size.x);
        if (maxImageSize.y > 0f) scale = Mathf.Min(scale, maxImageSize.y / size.y);

        image.rectTransform.sizeDelta = size * scale;
    }

    private void ReturnToMenu()
    {
        if (!Application.CanStreamedLevelBeLoaded(menuSceneName))
        {
            Debug.LogError(
                "Scene '" + menuSceneName +
                "' chua co trong Build Settings.", this);
            return;
        }

        // Reset de tran sau con pha Nexus duoc (bien static khong tu reset khi doi scene)
        Time.timeScale = 1f;
        matchEnded = false;

        SceneManager.LoadScene(menuSceneName);
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