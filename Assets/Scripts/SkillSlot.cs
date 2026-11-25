using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class SkillSlot : MonoBehaviour
{
    public enum SkillType { Q, E }

    [Header("Skill Configuration")]
    public SkillType skillType;

    [Header("Data Source")]
    public CharacterStatData characterData;

    [Header("UI References")]
    public Image cooldownOverlay;
    public TextMeshProUGUI cooldownText;

    private bool isCooldown = false;
    private float currentCooldown = 0f;
    private float maxCooldown = 0f;

    void Start()
    {
        // Khởi tạo trạng thái ban đầu cho UI cooldown
        if (cooldownOverlay != null)
            cooldownOverlay.fillAmount = 0f;

        if (cooldownText != null)
            cooldownText.gameObject.SetActive(false);

        // 1. Tự động lấy dữ liệu thông qua ChampSelectResult khi đi từ màn hình chọn tướng sang
        if (ChampSelectResult.PickedChampion != null && ChampSelectResult.PickedChampion.playerPrefab != null)
        {
            // Tìm script "Stat" nằm trên Prefab của nhân vật
            Stat characterStat = ChampSelectResult.PickedChampion.playerPrefab.GetComponent<Stat>();
            if (characterStat == null)
            {
                characterStat = ChampSelectResult.PickedChampion.playerPrefab.GetComponentInChildren<Stat>();
            }

            // Lấy file CharacterStatData từ biến "Data" của script Stat
            if (characterStat != null && characterStat.Data != null)
            {
                characterData = characterStat.Data;
            }
        }

        // 2. Dự phòng: Trường hợp bạn bấm Play trực tiếp tại GameScene (không qua màn hình chọn tướng)
        if (characterData == null)
        {
            Stat activeStat = FindFirstObjectByType<Stat>();
            if (activeStat != null && activeStat.Data != null)
            {
                characterData = activeStat.Data;
            }
        }

        // Kiểm tra log xem đã nhận được dữ liệu hồi chiêu hay chưa
        if (characterData != null)
        {
            Debug.Log($"[SkillSlot {skillType}] Thành công! Nhận cooldown Q: {characterData.cooldownQ}s, E: {characterData.cooldownE}s");
        }
        else
        {
            Debug.LogWarning($"[SkillSlot {skillType}] Vẫn chưa tìm thấy CharacterStatData trên nhân vật!");
        }
    }

    void Update()
    {
        if (isCooldown)
        {
            currentCooldown -= Time.deltaTime;

            if (currentCooldown <= 0f)
            {
                isCooldown = false;
                currentCooldown = 0f;
                if (cooldownOverlay != null) cooldownOverlay.fillAmount = 0f;
                if (cooldownText != null) cooldownText.gameObject.SetActive(false);
            }
            else
            {
                if (cooldownOverlay != null)
                    cooldownOverlay.fillAmount = currentCooldown / maxCooldown;

                if (cooldownText != null)
                {
                    // Nếu còn trên 1 giây thì làm tròn số nguyên (5, 4, 3, 2, 1)
                    // Nếu dưới 1 giây thì hiện 1 số thập phân (0.9, 0.8... 0.1)
                    if (currentCooldown > 1f)
                    {
                        cooldownText.text = Mathf.CeilToInt(currentCooldown).ToString();
                    }
                    else
                    {
                        cooldownText.text = currentCooldown.ToString("F1");
                    }
                }
            }
        }
    }

    // Hàm công khai để script nhân vật gọi NGAY KHI tung chiêu thành công
    public void TriggerCooldown(float customDuration = -1f)
    {
        // Lấy thời gian từ data (hoặc dùng thời gian truyền vào nếu có tính giảm hồi chiêu)
        maxCooldown = (customDuration > 0f) ? customDuration : GetCooldownTimeFromData();

        if (maxCooldown <= 0f) return;

        isCooldown = true;
        currentCooldown = maxCooldown;

        if (cooldownOverlay != null) cooldownOverlay.fillAmount = 1f;
        if (cooldownText != null) cooldownText.gameObject.SetActive(true);
    }

    // Lấy thời gian hồi chiêu tương ứng từ file asset CharacterStatData
    float GetCooldownTimeFromData()
    {
        if (characterData == null) return 5f; // Mặc định nếu chưa có data

        switch (skillType)
        {
            case SkillType.Q:
                return characterData.cooldownQ;
            case SkillType.E:
                return characterData.cooldownE;
            default:
                return 5f;
        }
    }
}