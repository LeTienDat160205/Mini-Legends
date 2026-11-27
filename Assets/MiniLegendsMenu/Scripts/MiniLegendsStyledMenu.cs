using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

// Main menu logic + layout. Keeps gameplay / Champion Select scenes unchanged.
[DisallowMultipleComponent]
public class MiniLegendsStyledMenu : MonoBehaviour
{
    [Header("Navigation")]
    [SerializeField] private string championSelectScene = "ChampionSelect";
    [Header("Menu Content")]
    [SerializeField] private ARAMMenuCatalog catalog;
    [Header("Generated reference illustration")]
    [SerializeField] private Sprite backgroundArt;

    private enum Tab { Play, Champions, Items, History }
    private Tab activeTab;
    [SerializeField, HideInInspector] private GameObject contentOverlay;
    [SerializeField, HideInInspector] private Button beginButton;
    [SerializeField, HideInInspector] private RectTransform entries;
    [SerializeField, HideInInspector] private TMP_Text header;
    [SerializeField, HideInInspector] private TMP_Text detail;
    [SerializeField, HideInInspector] private Image portrait;
    [SerializeField, HideInInspector] private TMP_Text status;
    private readonly List<GameObject> generatedEntries = new List<GameObject>();
    private bool wired;

    private static readonly Color Gold = ColorOf("#E7CF9D");
    private static readonly Color Light = ColorOf("#E6F5FF");
    private static readonly Color Cyan = ColorOf("#51DDFB");
    private static readonly Color Slate = ColorOf("#091C32");
    private static Color ColorOf(string hex) { ColorUtility.TryParseHtmlString(hex, out var v); return v; }

    public void Setup(ARAMMenuCatalog newCatalog, Sprite artwork, string sceneName)
    {
        catalog = newCatalog;
        backgroundArt = artwork;
        championSelectScene = sceneName;
    }

    private void Awake()
    {
        if (transform.Find("MenuCanvas") == null) BuildLayout();
        WireButtons();
        Open(Tab.Play);
    }

    // Called in edit mode by Tools > Mini Legends > Create Styled Menu Scene.
    public void BuildLayout()
    {
        if (transform.Find("MenuCanvas") != null) return;
        GameObject canvasObject = new GameObject("MenuCanvas", typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
        canvasObject.transform.SetParent(transform, false);
        Canvas canvas = canvasObject.GetComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        CanvasScaler scaler = canvasObject.GetComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1672f, 941f);
        scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
        scaler.matchWidthOrHeight = 0.5f;
        RectTransform canvasRect = canvasObject.GetComponent<RectTransform>();
        Stretch(canvasRect);

        Image backdrop = UI("ReferenceArtwork", canvasRect).AddComponent<Image>();
        backdrop.sprite = backgroundArt;
        backdrop.color = Color.white;
        backdrop.raycastTarget = false;
        Stretch(backdrop.rectTransform);

        // Replace central baked artwork with live content only when browsing tabs.
        contentOverlay = UI("InformationOverlay", canvasRect);
        Image overlayBg = contentOverlay.AddComponent<Image>();
        overlayBg.color = new Color(.025f, .085f, .15f, .97f);
        RectTop(contentOverlay.GetComponent<RectTransform>(), 393, 124, 1174, 675);
        Outline outline = contentOverlay.AddComponent<Outline>();
        outline.effectColor = new Color(.28f, .83f, 1f, .9f);
        outline.effectDistance = new Vector2(2, -2);

        header = Text("SectionTitle", contentOverlay.transform, "TƯỚNG", 47, TextAlignmentOptions.Center, Gold);
        RectTop(header.rectTransform, 35, 14, 1100, 70, 1174, 675);
        Image divider = UI("Divider", contentOverlay.transform).AddComponent<Image>();
        divider.color = new Color(.27f, .83f, 1f, .85f);
        RectTop(divider.rectTransform, 42, 93, 1090, 2, 1174, 675);

        // Scrollable list at left.
        GameObject listPanel = UI("ListPanel", contentOverlay.transform);
        Image listBg = listPanel.AddComponent<Image>(); listBg.color = new Color(.07f, .18f, .3f, .9f);
        RectTop(listPanel.GetComponent<RectTransform>(), 38, 122, 385, 490, 1174, 675);
        ScrollRect scroll = listPanel.AddComponent<ScrollRect>();
        scroll.horizontal = false;
        GameObject viewport = UI("Viewport", listPanel.transform);
        RectTransform vpRect = viewport.GetComponent<RectTransform>();
        Stretch(vpRect, 12, 12, -12, -12);
        viewport.AddComponent<RectMask2D>();
        GameObject listObj = UI("ListEntries", viewport.transform);
        entries = listObj.GetComponent<RectTransform>();
        entries.anchorMin = new Vector2(0, 1);
        entries.anchorMax = new Vector2(1, 1);
        entries.pivot = new Vector2(.5f, 1);
        entries.anchoredPosition = Vector2.zero;
        entries.sizeDelta = Vector2.zero;
        var layout = listObj.AddComponent<VerticalLayoutGroup>();
        layout.spacing = 11;
        layout.childControlHeight = true;
        layout.childForceExpandHeight = false;
        layout.childControlWidth = true;
        layout.childForceExpandWidth = true;
        var fitter = listObj.AddComponent<ContentSizeFitter>();
        fitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;
        scroll.viewport = vpRect;
        scroll.content = entries;

        GameObject detailPanel = UI("DetailPanel", contentOverlay.transform);
        Image detailBg = detailPanel.AddComponent<Image>();
        detailBg.color = new Color(.055f, .14f, .23f, .94f);
        RectTop(detailPanel.GetComponent<RectTransform>(), 439, 122, 695, 490, 1174, 675);

        portrait = UI("SelectedPortrait", detailPanel.transform).AddComponent<Image>();
        portrait.preserveAspect = true;
        portrait.raycastTarget = false;
        RectTop(portrait.rectTransform, 472, 20, 196, 160, 695, 490);

        GameObject detailScrollObj = UI("DetailScroll", detailPanel.transform);
        RectTop(detailScrollObj.GetComponent<RectTransform>(), 20, 26, 655, 440, 695, 490);
        ScrollRect detailsScroll = detailScrollObj.AddComponent<ScrollRect>();
        detailsScroll.horizontal = false;
        GameObject detailsVp = UI("Viewport", detailScrollObj.transform);
        RectTransform detailsVpRect = detailsVp.GetComponent<RectTransform>();
        Stretch(detailsVpRect);
        detailsVp.AddComponent<RectMask2D>();
        detail = Text("Details", detailsVp.transform, "", 27, TextAlignmentOptions.TopLeft, Light);
        detail.textWrappingMode = TextWrappingModes.Normal;
        detail.overflowMode = TextOverflowModes.Overflow;
        detail.rectTransform.anchorMin = new Vector2(0, 1);
        detail.rectTransform.anchorMax = new Vector2(1, 1);
        detail.rectTransform.pivot = new Vector2(.5f, 1);
        detail.rectTransform.anchoredPosition = Vector2.zero;
        detail.rectTransform.sizeDelta = Vector2.zero;
        var detailFit = detail.gameObject.AddComponent<ContentSizeFitter>();
        detailFit.verticalFit = ContentSizeFitter.FitMode.PreferredSize;
        detailsScroll.content = detail.rectTransform;
        detailsScroll.viewport = detailsVpRect;
        portrait.transform.SetAsLastSibling();

        status = Text("Status", contentOverlay.transform, "", 19, TextAlignmentOptions.MidlineLeft, Cyan);
        RectTop(status.rectTransform, 43, 623, 1050, 33, 1174, 675);

        // Invisible interaction rectangles atop the exact baked illustration.
        TransparentButton("Nav_Play", canvasRect, 25, 171, 343, 97);
        TransparentButton("Nav_Champions", canvasRect, 25, 279, 296, 102);
        TransparentButton("Nav_Items", canvasRect, 25, 385, 296, 100);
        TransparentButton("Nav_History", canvasRect, 25, 492, 296, 101);
        beginButton = TransparentButton("Begin_3v3_ARAM", canvasRect, 705, 709, 544, 132);
        contentOverlay.SetActive(false);
    }

    private void WireButtons()
    {
        if (wired) return;
        Button play = FindButton("Nav_Play");
        Button champions = FindButton("Nav_Champions");
        Button items = FindButton("Nav_Items");
        Button history = FindButton("Nav_History");
        beginButton = FindButton("Begin_3v3_ARAM");
        if (play == null || champions == null || items == null || history == null || beginButton == null) return;
        play.onClick.AddListener(() => Open(Tab.Play));
        champions.onClick.AddListener(() => Open(Tab.Champions));
        items.onClick.AddListener(() => Open(Tab.Items));
        history.onClick.AddListener(() => Open(Tab.History));
        beginButton.onClick.AddListener(StartMatch);
        wired = true;
    }

    private Button FindButton(string name)
    {
        Transform node = transform.Find("MenuCanvas/" + name);
        return node == null ? null : node.GetComponent<Button>();
    }

    private void StartMatch()
    {
        if (string.IsNullOrEmpty(championSelectScene) || !Application.CanStreamedLevelBeLoaded(championSelectScene))
        {
            Debug.LogError("Mini Legends: Champion Select scene not in Build Profiles: " + championSelectScene);
            Open(Tab.Play);
            return;
        }
        Time.timeScale = 1;
        SceneManager.LoadScene(championSelectScene);
    }

    private void Open(Tab tab)
    {
        activeTab = tab;
        contentOverlay.SetActive(tab != Tab.Play);
        beginButton.gameObject.SetActive(tab == Tab.Play);
        if (tab == Tab.Play) return;
        foreach (Transform child in entries) Destroy(child.gameObject);
        generatedEntries.Clear();
        portrait.enabled = false;
        detail.text = "";
        status.text = "";
        switch (tab)
        {
            case Tab.Champions:
                header.text = "THÔNG TIN TƯỚNG";
                if (catalog == null || catalog.champions == null || catalog.champions.Count == 0)
                { detail.text = "Chưa có dữ liệu tướng."; return; }
                foreach (var hero in catalog.champions)
                {
                    var captured = hero;
                    AddEntry(hero.displayName, () => ShowChampion(captured));
                }
                ShowChampion(catalog.champions[0]);
                break;
            case Tab.Items:
                header.text = "THÔNG TIN TRANG BỊ";
                if (catalog == null || catalog.items == null || catalog.items.Count == 0)
                { detail.text = "Chưa có dữ liệu trang bị.\n\nThêm Items trong ARAMMenuCatalog.asset bằng dữ liệu Shop hiện có."; return; }
                foreach (var item in catalog.items)
                {
                    var captured = item;
                    AddEntry(item.displayName, () => ShowItem(captured));
                }
                ShowItem(catalog.items[0]);
                break;
            case Tab.History:
                header.text = "LỊCH SỬ ĐẤU";
                List<ARAMMatchHistory.MatchEntry> matches = ARAMMatchHistory.Read();
                if (matches.Count == 0)
                { detail.text = "Chưa có trận đấu nào được lưu.\n\nLịch sử sẽ hiện ở đây khi Nexus ghi kết quả vào ARAMMatchHistory."; return; }
                foreach (var match in matches)
                {
                    var captured = match;
                    AddEntry((match.victory ? "THẮNG  " : "THUA  ") + match.champion + "  ·  " + match.utcTime,
                        () => ShowMatch(captured));
                }
                ShowMatch(matches[0]);
                break;
        }
    }

    private void ShowChampion(ARAMMenuCatalog.ChampionInfo hero)
    {
        if (hero == null) return;
        detail.text = hero.displayName + "\n<color=#58DDF8>" + hero.role + "</color>\n\n" + hero.description +
            "\n\n<color=#E8C88F>ĐÁNH THƯỜNG</color>\n" + hero.attackStyle +
            "\n\n<color=#E8C88F>KỸ NĂNG Q</color>\n" + hero.skillQ +
            "\n\n<color=#E8C88F>KỸ NĂNG E</color>\n" + hero.skillE;
        portrait.sprite = hero.portrait;
        portrait.enabled = hero.portrait != null;
    }

    private void ShowItem(ARAMMenuCatalog.ItemInfo item)
    {
        if (item == null) return;
        detail.text = item.displayName + "\n<color=#E8C88F>" + item.price + " vàng</color>\n\n" + item.stats + "\n\n" + item.description;
        portrait.sprite = item.icon;
        portrait.enabled = item.icon != null;
    }

    private void ShowMatch(ARAMMatchHistory.MatchEntry match)
    {
        portrait.enabled = false;
        detail.text = (match.victory ? "<color=#57DCA6>CHIẾN THẮNG</color>" : "<color=#FF8B8B>THẤT BẠI</color>") +
            "\n\nChế độ: " + match.mode + "\nTướng: " + match.champion + "\nNgày: " + match.utcTime +
            "\nThời lượng: " + (match.durationSeconds / 60) + " phút " + (match.durationSeconds % 60) + " giây";
    }

    private void AddEntry(string value, Action clicked)
    {
        GameObject entry = UI("Entry", entries);
        Image bg = entry.AddComponent<Image>();
        bg.color = new Color(.10f, .27f, .41f, 1f);
        Button button = entry.AddComponent<Button>();
        button.targetGraphic = bg;
        ColorBlock colors = button.colors;
        colors.normalColor = Color.white;
        colors.highlightedColor = new Color(.65f, .95f, 1f, 1f);
        colors.pressedColor = new Color(.35f, .8f, 1f, 1f);
        button.colors = colors;
        LayoutElement layout = entry.AddComponent<LayoutElement>();
        layout.preferredHeight = 83;
        TMP_Text caption = Text("EntryName", entry.transform, value, 23, TextAlignmentOptions.MidlineLeft, Gold);
        Stretch(caption.rectTransform, 18, 5, -12, -5);
        button.onClick.AddListener(() => clicked());
    }

    private static Button TransparentButton(string name, Transform parent, float x, float y, float w, float h)
    {
        GameObject go = UI(name, parent);
        RectTop(go.GetComponent<RectTransform>(), x, y, w, h);
        Image image = go.AddComponent<Image>();
        image.color = new Color(1, 1, 1, 0);
        image.raycastTarget = true;
        Button button = go.AddComponent<Button>();
        button.targetGraphic = image;
        button.transition = Selectable.Transition.None;
        return button;
    }

    private static GameObject UI(string name, Transform parent)
    {
        GameObject go = new GameObject(name, typeof(RectTransform));
        go.transform.SetParent(parent, false);
        return go;
    }

    private static TMP_Text Text(string name, Transform parent, string text, int fontSize, TextAlignmentOptions alignment, Color color)
    {
        GameObject go = UI(name, parent);
        TextMeshProUGUI tmp = go.AddComponent<TextMeshProUGUI>();
        tmp.text = text;
        tmp.fontSize = fontSize;
        tmp.alignment = alignment;
        tmp.color = color;
        tmp.raycastTarget = false;
        tmp.textWrappingMode = TextWrappingModes.Normal;
        return tmp;
    }

    // Reference image uses 1672x941, coordinates originate at top-left.
    private static void RectTop(RectTransform r, float x, float y, float w, float h, float W = 1672, float H = 941)
    {
        r.anchorMin = new Vector2(x / W, 1f - (y + h) / H);
        r.anchorMax = new Vector2((x + w) / W, 1f - y / H);
        r.offsetMin = Vector2.zero;
        r.offsetMax = Vector2.zero;
    }

    private static void Stretch(RectTransform r, float left = 0, float bottom = 0, float right = 0, float top = 0)
    {
        r.anchorMin = Vector2.zero;
        r.anchorMax = Vector2.one;
        r.offsetMin = new Vector2(left, bottom);
        r.offsetMax = new Vector2(right, top);
    }
}
