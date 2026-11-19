using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Một ô người chơi ở cột trái / phải (avatar tròn, vai trò, tên, trạng thái).
/// </summary>
public class PlayerSlot : MonoBehaviour
{
    [SerializeField] private Image portrait;       // Ảnh tướng nằm trong avatar tròn (tắt khi chưa chọn)
    [SerializeField] private TMP_Text roleText;
    [SerializeField] private TMP_Text nameText;
    [SerializeField] private TMP_Text statusText;
    [SerializeField] private GameObject highlight; // Nền sáng khi đến lượt người này

    public void SetHighlight(bool active)
    {
        if (highlight != null) highlight.SetActive(active);
    }

    public void SetStatus(string status)
    {
        if (statusText != null) statusText.text = status;
    }

    // Hiện tướng đang chọn lên avatar.
    public void ShowChampion(ChampionData data)
    {
        if (data == null || portrait == null) return;

        portrait.sprite = data.portrait;
        portrait.enabled = data.portrait != null;
    }
}
