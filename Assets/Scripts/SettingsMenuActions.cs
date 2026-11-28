
using UnityEngine;
using UnityEngine.SceneManagement;

public class SettingsMenuActions : MonoBehaviour
{
    public void ExitMatch()
    {
        // Dat lai toc do game binh thuong
        Time.timeScale = 1f;

        // Quay ve man hinh chon che do
        if (Application.CanStreamedLevelBeLoaded("ModeMenu_Styled"))
        {
            SceneManager.LoadScene("ModeMenu_Styled");
        }
        else
        {
            Debug.LogError(
                "Chua them ModeMenu_Styled vao Scene List!"
            );
        }
    }
}
