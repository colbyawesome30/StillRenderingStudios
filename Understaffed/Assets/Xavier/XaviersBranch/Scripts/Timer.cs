using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class Timer : MonoBehaviour
{
    public float countdownTime = 60f;
    private PlayerInfo playerInfo;
    private float currentTime;
    public bool pauseTimer = false;
    public bool pauseTime = false;
    public Tutorial tutorial;
    [SerializeField]private TMP_Text timerText;
    [SerializeField]private TMP_Text fundsText;

    [SerializeField] private TMP_Text starsToWinText;
    [SerializeField] private Image starsToWinImage;
    [SerializeField] private Color losingColor = new Color(0.9f, 0.25f, 0.25f);
    [SerializeField] private Color winningColor = new Color(0.4f, 0.85f, 0.4f);

    // Start is called before the first frame update
    void Start()
    {
        playerInfo = FindAnyObjectByType<PlayerInfo>();
        tutorial = FindAnyObjectByType<Tutorial>();
        currentTime = countdownTime;
        Time.timeScale = 1f;

        if (starsToWinText != null)
            starsToWinText.text = LevelSettings.StarsToWin + " Stars to Win";

        if (tutorial != null && !playerInfo.TutorialComplete)
        {
            pauseTimer = true;
        }


        UpdateTimerDisplay(); 
    }

    void UpdateStarsToWinTint()
    {
        if (starsToWinImage == null) return;

        bool winning = playerInfo.stars >= LevelSettings.StarsToWin;
        starsToWinImage.color = winning ? winningColor : losingColor;
    }

    public void PauseTimer(bool pause)
    {
        pauseTimer = pause;
    }

    public void PauseTime(bool pause)
    {
        pauseTime = pause;
    }

    // Update is called once per frame
    void Update()
    {
        fundsText.text = "$" + playerInfo.playerFunds.ToString() + " left";

        UpdateStarsToWinTint();

        if (pauseTimer) return;

        if (currentTime > 0)
        {
            currentTime -= Time.deltaTime;

            if (currentTime < 0)
            {
                currentTime = 0;
                EndTimeEvent();
            }
        }

        UpdateTimerDisplay();
    }

    public void EndTimeEvent()
    {
        if (tutorial != null && tutorial.tutorialComplete)
        {
            StartCoroutine(DelaySwitchScene(2f, "MainMenu"));
        }
        
        playerInfo.EndGame();
        
    }

    void UpdateTimerDisplay()
    {
        // Use Mathf.Max to prevent displaying negative numbers while rounding
        float timeToDisplay = Mathf.Max(0, currentTime); 
        
        int minutes = Mathf.FloorToInt(timeToDisplay / 60);
        int seconds = Mathf.FloorToInt(timeToDisplay % 60);

        // {0:00}:{1:00} forces a two-digit layout for both minutes and seconds
        timerText.text = "Time Left: " + string.Format("{0:00}:{1:00}", minutes, seconds);
    }

    IEnumerator DelaySwitchScene(float delay, string sceneName)
    {
        yield return new WaitForSecondsRealtime(delay);
        Debug.Log("Attempting to load scene: " + sceneName);
        SceneManager.LoadScene(sceneName);
    }
}