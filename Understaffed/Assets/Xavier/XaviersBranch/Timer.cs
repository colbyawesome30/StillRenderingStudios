using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class Timer : MonoBehaviour
{
    public float countdownTime = 60f;

    private float currentTime;

    [SerializeField]private TMP_Text timerText;

    // Start is called before the first frame update
    void Start()
    {
        currentTime = countdownTime;
    }

    // Update is called once per frame
    void Update()
    {
        if (currentTime > 0)
        {
            currentTime -= Time.deltaTime;
            timerText.text = "Time Left: " + ((int)currentTime);

            if (currentTime < 0)
            {
                currentTime = 0;
                EndTimeEvent();
            }
            UpdateTimerDisplay();
        }
    }

    public void EndTimeEvent()
    {
        
    }

    void UpdateTimerDisplay()
    {
        // Use Mathf.Max to prevent displaying negative numbers while rounding
        float timeToDisplay = Mathf.Max(0, currentTime); 
        
        int minutes = Mathf.FloorToInt(timeToDisplay / 60);
        int seconds = Mathf.FloorToInt(timeToDisplay % 60);

        // {0:00}:{1:00} forces a two-digit layout for both minutes and seconds
        timerText.text = string.Format("{0:00}:{1:00}", minutes, seconds);
    }
}