
using System;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class HotkeysSettings : MonoBehaviour
{
    [Header("Cac nut doi phim")]
    public Button qKeyButton;
    public Button eKeyButton;
    public Button pKeyButton;
    public Button bKeyButton;

    private string waiting = null;
    private int clickFrame;

    public static bool IsMenuOpen { get; private set; }

    // Lay phim nguoi choi da luu
    public static KeyCode SkillQ => GetKey("Q", KeyCode.Q);
    public static KeyCode SkillE => GetKey("E", KeyCode.E);
    public static KeyCode Shop => GetKey("P", KeyCode.P);
    public static KeyCode Recall => GetKey("B", KeyCode.B);

    static KeyCode GetKey(string id, KeyCode defaultKey)
    {
        string saved = PlayerPrefs.GetString(
            "Hotkey_" + id,
            defaultKey.ToString()
        );

        if (Enum.TryParse(saved, out KeyCode key))
            return key;

        return defaultKey;
    }

    void OnEnable()
    {
        IsMenuOpen = true;

        if (qKeyButton != null)
            qKeyButton.onClick.AddListener(ChangeQ);

        if (eKeyButton != null)
            eKeyButton.onClick.AddListener(ChangeE);

        if (pKeyButton != null)
            pKeyButton.onClick.AddListener(ChangeP);

        if (bKeyButton != null)
            bKeyButton.onClick.AddListener(ChangeB);

        RefreshButtons();
    }

    void OnDisable()
    {
        IsMenuOpen = false;
        waiting = null;

        if (qKeyButton != null)
            qKeyButton.onClick.RemoveListener(ChangeQ);

        if (eKeyButton != null)
            eKeyButton.onClick.RemoveListener(ChangeE);

        if (pKeyButton != null)
            pKeyButton.onClick.RemoveListener(ChangeP);

        if (bKeyButton != null)
            bKeyButton.onClick.RemoveListener(ChangeB);
    }

    void ChangeQ() => StartChange("Q");
    void ChangeE() => StartChange("E");
    void ChangeP() => StartChange("P");
    void ChangeB() => StartChange("B");

    void StartChange(string id)
    {
        waiting = id;
        clickFrame = Time.frameCount;
        RefreshButtons();
    }

    void Update()
    {
        if (waiting == null)
            return;

        if (Time.frameCount <= clickFrame)
            return;

        // ESC: Huy doi phim
        if (Input.GetKeyDown(KeyCode.Escape))
        {
            waiting = null;
            RefreshButtons();
            return;
        }

        // Nhan mot phim tu A den Z
        for (KeyCode key = KeyCode.A; key <= KeyCode.Z; key++)
        {
            if (Input.GetKeyDown(key))
            {
                SaveNewKey(key);
                return;
            }
        }

        // Nhan phim so 0 den 9
        for (KeyCode key = KeyCode.Alpha0;
             key <= KeyCode.Alpha9; key++)
        {
            if (Input.GetKeyDown(key))
            {
                SaveNewKey(key);
                return;
            }
        }
    }

    void SaveNewKey(KeyCode key)
    {
        // Khong cho dung WASD
        if (key == KeyCode.W || key == KeyCode.A ||
            key == KeyCode.S || key == KeyCode.D)
        {
            ShowMessage("KHONG DUNG WASD");
            return;
        }

        // Khong cho hai chuc nang trung phim
        bool duplicate =
            (waiting != "Q" && key == SkillQ) ||
            (waiting != "E" && key == SkillE) ||
            (waiting != "P" && key == Shop) ||
            (waiting != "B" && key == Recall);

        if (duplicate)
        {
            ShowMessage("TRUNG PHIM");
            return;
        }

        PlayerPrefs.SetString(
            "Hotkey_" + waiting,
            key.ToString()
        );

        PlayerPrefs.Save();

        Debug.Log("Doi phim " + waiting + " thanh " + key);

        waiting = null;
        RefreshButtons();
    }

    void ShowMessage(string message)
    {
        Button button = GetWaitingButton();

        if (button == null)
            return;

        TMP_Text label =
            button.GetComponentInChildren<TMP_Text>(true);

        if (label != null)
            label.text = message;
    }

    Button GetWaitingButton()
    {
        switch (waiting)
        {
            case "Q": return qKeyButton;
            case "E": return eKeyButton;
            case "P": return pKeyButton;
            case "B": return bKeyButton;
        }

        return null;
    }

    void RefreshButtons()
    {
        UpdateButton(qKeyButton, "Q", SkillQ);
        UpdateButton(eKeyButton, "E", SkillE);
        UpdateButton(pKeyButton, "P", Shop);
        UpdateButton(bKeyButton, "B", Recall);
    }

    void UpdateButton(Button button, string id, KeyCode key)
    {
        if (button == null)
            return;

        TMP_Text label =
            button.GetComponentInChildren<TMP_Text>(true);

        if (label == null)
            return;

        label.text = waiting == id
            ? "NHAN PHIM..."
            : key.ToString().Replace("Alpha", "");
    }

    // Dung trong script gameplay
    public static bool Pressed(KeyCode key)
    {
        return !IsMenuOpen && Input.GetKeyDown(key);
    }

    public static bool Released(KeyCode key)
    {
        return !IsMenuOpen && Input.GetKeyUp(key);
    }
}
