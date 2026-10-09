using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;

public class MainMenu : MonoBehaviour
{
    [System.Serializable]
    public class LevelEntry
    {
        public string sceneName;   // exactly the scene name, e.g. "ArtScene"
        public Image buttonImage;  // the Image on that level's button
    }

    public GameObject levelsUI;
    public GameObject mainMenuUI;
    public GameObject settingsUI;

    public List<LevelEntry> levels;
    public Color completedColor = new Color(0.4f, 0.85f, 0.4f);
    public Color defaultColor = Color.white;

    private void Start()
    {
        ChangeUI("mainMenuUI");
        RefreshLevelColors();
    }

    public void ChangeUI(string chosenUI)
    {
        levelsUI.SetActive(chosenUI == "levelsUI");
        mainMenuUI.SetActive(chosenUI == "mainMenuUI");
        settingsUI.SetActive(chosenUI == "settingsUI");

        if (chosenUI == "levelsUI")
            RefreshLevelColors();
    }

    public void RefreshLevelColors()
    {
        foreach (LevelEntry level in levels)
        {
            if (level.buttonImage == null) continue;
            level.buttonImage.color = LevelProgress.IsComplete(level.sceneName)
                ? completedColor
                : defaultColor;
        }
    }

    public void OpenLevel() => SceneManager.LoadScene("ArtScene");
    public void ChooseLevel(string levelName) => SceneManager.LoadScene(levelName);
    public void QuitGame() => Application.Quit();

    public void ResetSave()
    {
        LevelProgress.ResetAll();
        RefreshLevelColors();
    }
}