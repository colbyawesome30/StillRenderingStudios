using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class Timer : MonoBehaviour
{
    private PlayerInfo playerInfo;
    public bool pauseTimer = false;
    public bool pauseTime = false;
    public Tutorial tutorial;

    [SerializeField] private TMP_Text waveText;
    [SerializeField] private TMP_Text fundsText;

    [SerializeField] private TMP_Text starsToWinText;
    [SerializeField] private Image starsToWinImage;
    [SerializeField] private Color losingColor = new Color(0.9f, 0.25f, 0.25f);
    [SerializeField] private Color winningColor = new Color(0.4f, 0.85f, 0.4f);

    private CustomerSpawner spawner;
    private bool dayEnded;
    private bool endPending;   //all waves finished while paused

    public List<GameObject> hideOnEnd; //UI to turn off when the day ends

    void Start()
    {
        playerInfo = FindAnyObjectByType<PlayerInfo>();
        tutorial = FindAnyObjectByType<Tutorial>();
        spawner = FindAnyObjectByType<CustomerSpawner>();
        Time.timeScale = 1f;

        if (starsToWinText != null)
            starsToWinText.text = LevelSettings.StarsToWin + " Stars to Win";

        if (tutorial != null && !playerInfo.TutorialComplete)
            pauseTimer = true;

        if (spawner != null)
        {
            spawner.WaveStarted += OnWaveStarted;
            spawner.AllWavesCompleted += OnAllWavesCompleted;
        }

        if (waveText != null) waveText.text = "";
    }

    void OnDestroy()
    {
        if (spawner != null)
        {
            spawner.WaveStarted -= OnWaveStarted;
            spawner.AllWavesCompleted -= OnAllWavesCompleted;
        }
    }

    void OnWaveStarted(int index)
    {
        if (waveText == null || spawner == null) return;

        waveText.text = spawner.endlessMode
            ? "Wave " + (index + 1)
            : "Wave " + (index + 1) + "/" + spawner.waves.Count;
    }

    //Fired by the spawner once the last wave's customers have all left
    void OnAllWavesCompleted()
    {
        if (dayEnded) return;

        if (pauseTimer) { endPending = true; return; }

        dayEnded = true;
        EndTimeEvent();
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
        Time.timeScale = pause ? 0f : 1f;
    }

    void Update()
    {
        fundsText.text = "$" + playerInfo.playerFunds.ToString() + " left";
        UpdateStarsToWinTint();

        if (endPending && !pauseTimer && !dayEnded)
        {
            dayEnded = true;
            endPending = false;
            EndTimeEvent();
        }
    }

    public void EndTimeEvent()
    {
        foreach (GameObject obj in hideOnEnd)
        {
            if (obj != null) obj.SetActive(false);
        }

        playerInfo.EndGame();
    }
}