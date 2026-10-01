using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;

public class Timer : MonoBehaviour
{
    public float countdownTime = 60f;
    private PlayerInfo playerInfo;
    private float currentTime;
    public bool pauseTimer = false;
    public Tutorial tutorial;


    [SerializeField]private TMP_Text timerText;

    // Start is called before the first frame update
    void Start()
    {
        playerInfo = FindAnyObjectByType<PlayerInfo>();
        tutorial = FindAnyObjectByType<Tutorial>();
        currentTime = countdownTime;

        if (tutorial != null && !playerInfo.TutorialComplete)
        {
            pauseTimer = true;
        }

        UpdateTimerDisplay(); 
    }

    public void PauseTimer(bool pause)
    {
        pauseTimer = pause;
    }

    // Update is called once per frame
    void Update()
    {
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