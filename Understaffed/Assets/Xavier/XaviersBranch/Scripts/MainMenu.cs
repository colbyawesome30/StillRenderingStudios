using UnityEngine;
using UnityEngine.SceneManagement;

public class MainMenu : MonoBehaviour
{
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        
    }

    public void StartGame()
    {
        SceneManager.LoadScene("ArtScene");
    }

    public void ClickTutorial()
    {
        SceneManager.LoadScene("Tutorial");
    }

    public void ClickSettings()
    {
        
    }

    public void QuitGame()
    {
        Application.Quit();
    }
}
