using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

// Thong bao ha guc: avatar nguoi ha (ben trai) - banner - avatar nguoi bi ha (ben phai).
// Phe minh ha dich -> banner Blue. Dich ha phe minh -> banner Red.
// Cac thong bao hien LAN LUOT (moi cai 4 giay), khong de len nhau.
// Gan script vao mot object luon bat (vi du object trong "KillFeed").
public class KillFeed : MonoBehaviour
{
    [Header("Anh banner (Sprites/HUD/Elimination notification)")]
    [SerializeField] private Sprite blueBanner;   // Phe minh ha guc phe dich
    [SerializeField] private Sprite redBanner;    // Phe dich ha guc phe minh

    [Header("Avatar tuong (Sprites/HUD/Characters)")]
    [SerializeField] private Sprite goblinScoutAvatar;
    [SerializeField] private Sprite bladeAssasinAvatar;
    [SerializeField] private Sprite footSoldierAvatar;

    [Header("Hien thi")]
    [Tooltip("Thoi gian moi thong bao ton tai (giay), tinh ca luc hien dan va mo dan")]
    [SerializeField, Min(0.5f)] private float displaySeconds = 4f;
    [SerializeField, Min(0f)] private float fadeSeconds = 0.25f;
    [Tooltip("Chieu cao banner (pixel). Chieu rong tinh theo ti le cua anh. Avatar nam trong vong tron nen can du lon (khoang 100-130)")]
    [SerializeField, Min(10f)] private float bannerHeight = 110f;
    [Tooltip("Tam vong tron avatar, tinh theo ti le chieu rong banner, tu giua banner ra hai ben")]
    [SerializeField, Range(0.2f, 0.5f)] private float avatarCenterRatio = 0.378f;
    [Tooltip("Duong kinh avatar, tinh theo ti le chieu cao banner (lon hon lo tron mot chut de vien khung che mep avatar)")]
    [SerializeField, Range(0.2f, 1f)] private float avatarSizeRatio = 0.48f;
    [Tooltip("So thong bao toi da dang xep hang cho. Vuot qua thi bo cai cu nhat")]
    [SerializeField, Min(1)] private int maxQueued = 8;

    [Header("Vi tri")]
    [Tooltip("De trong: tu tao canvas, thong bao hien o giua phia tren man hinh")]
    [SerializeField] private RectTransform feedRoot;
    [SerializeField] private float topOffset = 170f;

    private struct Entry
    {
        public bool friendly;       // true = phe minh ha guc (banner Blue)
        public Sprite killerAvatar;
        public Sprite victimAvatar;
    }

    private readonly Queue<Entry> queue = new Queue<Entry>();
    private bool running;

    private GameObject autoCanvas;
    private RectTransform itemRect;
    private CanvasGroup itemGroup;
    private Image bannerImage;
    private Image killerImage;
    private Image victimImage;

    private void Awake()
    {
        BuildUI();
    }

    private void OnEnable()
    {
        ScoreManager.ChampionKilled += OnChampionKilled;
    }

    private void OnDisable()
    {
        ScoreManager.ChampionKilled -= OnChampionKilled;
        running = false;
    }

    private void OnDestroy()
    {
        if (autoCanvas != null) Destroy(autoCanvas);
    }

    private void OnChampionKilled(TeamMember killer, TeamMember victim)
    {
        if (killer == null || victim == null) return;

        // "Phe minh" = phe cua nguoi choi
        Team local = MinimapManager.LocalPlayerTeam;
        if (local == Team.Neutral) local = Team.Blue;

        Entry entry = new Entry
        {
            friendly = killer.team == local,
            killerAvatar = GetAvatar(killer),
            victimAvatar = GetAvatar(victim)
        };

        while (queue.Count >= maxQueued) queue.Dequeue();
        queue.Enqueue(entry);

        if (!running) StartCoroutine(RunQueue());
    }

    private Sprite GetAvatar(TeamMember member)
    {
        if (member == null) return null;
        if (member.GetComponent<GoblinScout>() != null) return goblinScoutAvatar;
        if (member.GetComponent<BladeAssasin>() != null) return bladeAssasinAvatar;
        if (member.GetComponent<FootSoldier>() != null) return footSoldierAvatar;
        return null;
    }

    // Hien lan luot tung thong bao: cai nay het han moi den cai tiep theo
    private IEnumerator RunQueue()
    {
        running = true;

        while (queue.Count > 0)
        {
            Entry entry = queue.Dequeue();
            yield return ShowEntry(entry);
        }

        running = false;
    }

    private IEnumerator ShowEntry(Entry entry)
    {
        ApplyEntry(entry);
        itemGroup.alpha = 0f;
        itemRect.gameObject.SetActive(true);

        yield return Fade(0f, 1f, fadeSeconds);

        float hold = Mathf.Max(0f, displaySeconds - fadeSeconds * 2f);
        yield return new WaitForSecondsRealtime(hold);

        yield return Fade(1f, 0f, fadeSeconds);

        itemRect.gameObject.SetActive(false);
    }

    private IEnumerator Fade(float from, float to, float seconds)
    {
        if (seconds <= 0f)
        {
            itemGroup.alpha = to;
            yield break;
        }

        float t = 0f;
        while (t < seconds)
        {
            t += Time.unscaledDeltaTime;
            itemGroup.alpha = Mathf.Lerp(from, to, Mathf.Clamp01(t / seconds));
            yield return null;
        }
        itemGroup.alpha = to;
    }

    private void ApplyEntry(Entry entry)
    {
        Sprite banner = entry.friendly ? blueBanner : redBanner;
        if (banner == null)
            Debug.LogWarning("[KillFeed] Chua gan sprite banner " + (entry.friendly ? "Blue" : "Red") + ".", this);

        float width = bannerHeight * 3.6f;   // Du phong neu chua co anh
        if (banner != null && banner.rect.height > 0f)
            width = bannerHeight * banner.rect.width / banner.rect.height;

        bannerImage.sprite = banner;
        bannerImage.enabled = banner != null;
        bannerImage.rectTransform.sizeDelta = new Vector2(width, bannerHeight);

        // Avatar nam TRONG hai vong tron cua banner
        float avatarSize = bannerHeight * avatarSizeRatio;
        float centerX = width * avatarCenterRatio;
        SetupAvatar(killerImage, entry.killerAvatar, -centerX, avatarSize);   // Nguoi ha: vong tron ben trai
        SetupAvatar(victimImage, entry.victimAvatar, centerX, avatarSize);    // Nguoi bi ha: vong tron ben phai
    }

    private void SetupAvatar(Image image, Sprite sprite, float x, float size)
    {
        image.sprite = sprite;
        image.enabled = sprite != null;
        image.rectTransform.sizeDelta = new Vector2(size, size);
        image.rectTransform.anchoredPosition = new Vector2(x, 0f);
    }

    // ------------------------------------------------------------------
    // Tao UI bang code de khong can dung prefab
    // ------------------------------------------------------------------

    private void BuildUI()
    {
        if (feedRoot == null)
        {
            autoCanvas = new GameObject("KillFeedCanvas", typeof(Canvas), typeof(CanvasScaler));
            Canvas canvas = autoCanvas.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 50;

            CanvasScaler scaler = autoCanvas.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920f, 1080f);
            scaler.matchWidthOrHeight = 0.5f;

            GameObject rootGo = new GameObject("KillFeedRoot", typeof(RectTransform));
            rootGo.transform.SetParent(autoCanvas.transform, false);
            feedRoot = (RectTransform)rootGo.transform;
            feedRoot.anchorMin = new Vector2(0.5f, 1f);
            feedRoot.anchorMax = new Vector2(0.5f, 1f);
            feedRoot.pivot = new Vector2(0.5f, 0.5f);
            feedRoot.anchoredPosition = new Vector2(0f, -topOffset);
            feedRoot.sizeDelta = Vector2.zero;
        }

        GameObject itemGo = new GameObject("KillFeedItem", typeof(RectTransform), typeof(CanvasGroup));
        itemGo.transform.SetParent(feedRoot, false);
        itemRect = (RectTransform)itemGo.transform;
        itemRect.anchorMin = new Vector2(0.5f, 0.5f);
        itemRect.anchorMax = new Vector2(0.5f, 0.5f);
        itemRect.pivot = new Vector2(0.5f, 0.5f);
        itemRect.anchoredPosition = Vector2.zero;
        itemRect.sizeDelta = Vector2.zero;

        itemGroup = itemGo.GetComponent<CanvasGroup>();
        itemGroup.alpha = 0f;
        itemGroup.interactable = false;
        itemGroup.blocksRaycasts = false;

        // Avatar tao truoc, banner tao sau -> banner nam TREN avatar, vien khung che mep avatar
        killerImage = CreateImage("KillerAvatar", itemRect);
        victimImage = CreateImage("VictimAvatar", itemRect);
        bannerImage = CreateImage("Banner", itemRect);

        itemGo.SetActive(false);
    }

    private static Image CreateImage(string objectName, RectTransform parent)
    {
        GameObject go = new GameObject(objectName, typeof(RectTransform), typeof(Image));
        go.transform.SetParent(parent, false);

        RectTransform rect = (RectTransform)go.transform;
        rect.anchorMin = new Vector2(0.5f, 0.5f);
        rect.anchorMax = new Vector2(0.5f, 0.5f);
        rect.pivot = new Vector2(0.5f, 0.5f);

        Image image = go.GetComponent<Image>();
        image.raycastTarget = false;
        image.preserveAspect = true;
        return image;
    }
}