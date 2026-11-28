using System.Collections;
using TMPro;
using UnityEngine;

/// <summary>
/// Bia tập đánh: hiện thanh máu và số HP trên đầu, tự hồi sinh sau khi bị phá.
/// Đặt script này ở object cha luôn bật; object con "Dummy" có TeamMember + Stat (bị tắt khi hết máu).
/// </summary>
public class TrainingDummy : MonoBehaviour
{
    [SerializeField] private TeamMember dummy;
    [SerializeField] private Stat stat;
    [SerializeField] private float respawnDelay = 3f;
    [SerializeField] private Vector2 barSize = new Vector2(1.6f, 0.18f);
    [SerializeField] private Vector3 barOffset = new Vector3(0f, 1.5f, 0f);

    private Transform barFill;
    private TMP_Text hpText;
    private Vector3 startPosition;
    private bool respawning;
    private static Sprite whiteSprite;

    private void Awake()
    {
        if (dummy == null) dummy = GetComponentInChildren<TeamMember>(true);
        if (dummy == null) return;
        if (stat == null) stat = dummy.GetComponent<Stat>();

        startPosition = dummy.transform.position;
        BuildBar();
    }

    private void Update()
    {
        if (dummy == null || stat == null || barFill == null) return;

        // Bia bị phá: TeamMember tắt object, ta đợi rồi hồi sinh.
        if (!dummy.gameObject.activeSelf)
        {
            if (!respawning) StartCoroutine(RespawnRoutine());
            return;
        }

        float max = Mathf.Max(1f, stat.Heart);
        float ratio = Mathf.Clamp01(dummy.CurrentHeart / max);
        barFill.localScale = new Vector3(barSize.x * ratio, barSize.y, 1f);
        barFill.localPosition = barOffset + new Vector3(-barSize.x * (1f - ratio) * 0.5f, 0f, 0f);
        if (hpText != null)
            hpText.text = Mathf.CeilToInt(dummy.CurrentHeart) + " / " + Mathf.CeilToInt(max);
    }

    private IEnumerator RespawnRoutine()
    {
        respawning = true;
        yield return new WaitForSeconds(respawnDelay);

        dummy.gameObject.SetActive(true);
        dummy.Respawn();                           // hồi đầy máu
        dummy.transform.position = startPosition;  // Respawn đưa về khu hồi sinh nên trả lại chỗ cũ
        respawning = false;
    }

    // Thanh máu và chữ HP nằm dưới object Dummy nên tự ẩn khi bia bị tắt.
    private void BuildBar()
    {
        Sprite sprite = GetWhiteSprite();

        GameObject back = new GameObject("HpBarBack");
        back.transform.SetParent(dummy.transform, false);
        back.transform.localPosition = barOffset;
        back.transform.localScale = new Vector3(barSize.x + 0.06f, barSize.y + 0.06f, 1f);
        SpriteRenderer backRenderer = back.AddComponent<SpriteRenderer>();
        backRenderer.sprite = sprite;
        backRenderer.color = new Color(0f, 0f, 0f, 0.75f);
        backRenderer.sortingOrder = 100;

        GameObject fill = new GameObject("HpBarFill");
        fill.transform.SetParent(dummy.transform, false);
        fill.transform.localPosition = barOffset;
        fill.transform.localScale = new Vector3(barSize.x, barSize.y, 1f);
        SpriteRenderer fillRenderer = fill.AddComponent<SpriteRenderer>();
        fillRenderer.sprite = sprite;
        fillRenderer.color = new Color(0.9f, 0.2f, 0.2f, 1f);
        fillRenderer.sortingOrder = 101;
        barFill = fill.transform;

        GameObject textGo = new GameObject("HpText");
        textGo.transform.SetParent(dummy.transform, false);
        textGo.transform.localPosition = barOffset + new Vector3(0f, 0.32f, 0f);
        TextMeshPro text = textGo.AddComponent<TextMeshPro>();
        text.alignment = TextAlignmentOptions.Center;
        text.fontSize = 3f;
        text.color = Color.white;
        text.sortingOrder = 102;
        text.rectTransform.sizeDelta = new Vector2(4f, 1f);
        hpText = text;
    }

    // Sprite vuông trắng 1x1 đơn vị, tạo lúc chạy để khỏi phụ thuộc asset.
    private static Sprite GetWhiteSprite()
    {
        if (whiteSprite != null) return whiteSprite;

        Texture2D texture = new Texture2D(1, 1);
        texture.SetPixel(0, 0, Color.white);
        texture.Apply();
        whiteSprite = Sprite.Create(texture, new Rect(0f, 0f, 1f, 1f), new Vector2(0.5f, 0.5f), 1f);
        return whiteSprite;
    }
}
