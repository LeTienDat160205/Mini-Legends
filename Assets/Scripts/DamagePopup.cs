using TMPro;
using UnityEngine;

/// <summary>
/// Số sát thương bay lên rồi mờ dần.
/// Không cần tạo prefab: gọi DamagePopup.Create(...) là tự sinh ra.
/// </summary>
public class DamagePopup : MonoBehaviour
{
    private const float Lifetime = 0.8f;
    private const float RiseSpeed = 1.5f;

    private TextMeshPro text;
    private Color startColor;
    private float timer;
    private Vector3 drift;

    public static void Create(Vector3 worldPosition, float damage)
    {
        GameObject go = new GameObject("DamagePopup");

        // Lệch ngẫu nhiên một chút để các số không chồng lên nhau
        Vector3 randomOffset = new Vector3(Random.Range(-0.3f, 0.3f), Random.Range(0f, 0.2f), 0f);
        go.transform.position = worldPosition + randomOffset;

        DamagePopup popup = go.AddComponent<DamagePopup>();
        popup.Setup(damage);
    }

    private void Setup(float damage)
    {
        text = gameObject.AddComponent<TextMeshPro>();
        text.text = Mathf.RoundToInt(damage).ToString();
        text.fontSize = 6f;                       // chỉnh to/nhỏ ở đây
        text.fontStyle = FontStyles.Bold;
        text.alignment = TextAlignmentOptions.Center;
        text.color = new Color(1f, 0.85f, 0.2f);  // vàng
        text.outlineWidth = 0.2f;
        text.outlineColor = Color.black;
        text.sortingOrder = 1000;                 // luôn nằm trên sprite khác
        text.textWrappingMode = TextWrappingModes.NoWrap;

        startColor = text.color;
        drift = new Vector3(Random.Range(-0.3f, 0.3f), RiseSpeed, 0f);
    }

    private void Update()
    {
        timer += Time.deltaTime;
        transform.position += drift * Time.deltaTime;

        // Mờ dần trong nửa sau của vòng đời
        float t = Mathf.Clamp01((timer - Lifetime * 0.5f) / (Lifetime * 0.5f));
        Color c = startColor;
        c.a = 1f - t;
        text.color = c;

        if (timer >= Lifetime)
            Destroy(gameObject);
    }
}
