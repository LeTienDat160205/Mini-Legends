using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using TMPro;

public class LoadingScreen : MonoBehaviour
{
    public string gameSceneName = "ARAM";
    public Slider progressBar;
    public TMP_Text progressText;
    [SerializeField] private Image[] blueAvatarFrames;
    [SerializeField] private Image[] redAvatarFrames;

    void Start()
    {
        ShowPickedChampion();
        StartCoroutine(LoadMatchAsync());
    }

    private void ShowPickedChampion()
    {
        ChampionData picked = ChampSelectResult.PickedChampion;
        if (picked == null || picked.portrait == null) return;

        Image[] frames = ChampSelectResult.PickedTeam == Team.Red ? redAvatarFrames : blueAvatarFrames;
        int index = ChampSelectResult.PickedSlotIndex;
        if (frames == null || index < 0 || index >= frames.Length || frames[index] == null) return;

        frames[index].sprite = picked.portrait;
        frames[index].preserveAspect = true;
        frames[index].enabled = true;
    }

    IEnumerator LoadMatchAsync()
    {
        AsyncOperation operation = SceneManager.LoadSceneAsync(gameSceneName);
        operation.allowSceneActivation = false;

        while (!operation.isDone)
        {
            float progress = Mathf.Clamp01(operation.progress / 0.9f);

            if (progressBar != null)
                progressBar.value = progress;

            if (progressText != null)
                progressText.text = Mathf.RoundToInt(progress * 100f) + "%";

            if (operation.progress >= 0.9f)
            {
                if (progressBar != null) progressBar.value = 1f;
                if (progressText != null) progressText.text = "100%";

                yield return new WaitForSeconds(0.5f);
                operation.allowSceneActivation = true;
            }

            yield return null;
        }
    }
}
