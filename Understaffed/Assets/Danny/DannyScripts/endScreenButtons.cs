using UnityEngine;
using UnityEngine.SceneManagement;

public class endScreenButtons : MonoBehaviour
{
    public string mainMenuSceneName = "MainMenu";

    public void Replay()
    {
        Time.timeScale = 1f;
        SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
    } // replay current scene

    public void MainMenu()
    {
        Time.timeScale = 1f;
        SceneManager.LoadScene(mainMenuSceneName);
    }

    public void NextLevel()
    {
        Time.timeScale = 1f;
        SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex + 1);
    } // gets next scene in build index
}