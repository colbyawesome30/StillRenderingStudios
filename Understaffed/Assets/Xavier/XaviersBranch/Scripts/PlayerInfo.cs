using UnityEngine;
using System;
using UnityEngine.UI;
using TMPro;
using System.Collections;

public class PlayerInfo : MonoBehaviour
{
    //Player funds and stats here
    public int playerFunds;
    public bool TutorialComplete;
    
    public int day;
    //Could expand to save unlocked workers

    public int currentCustomers;
    public event EventHandler CurrentDayScoreChanged;
    public float stars;
    private float _totalSatisfaction;
    public int maxScorePerCustomer = 100;
    public float maxStars = 5f;
    [SerializeField] private TMP_Text starsText;
    [SerializeField] private Image fillImage;
    public GameObject winScreen;
    public GameObject loseScreen;

    private bool _gameEnd = false;

    public static event Action<int> DifficultyChanged;
    public static int CurrentDifficulty { get; private set; }
    [SerializeField] private int _difficulty = 1;

    public int difficulty
    {
        get => _difficulty;
        set
        {
            if (_difficulty != value)
            {
                _difficulty = value;
                DifficultyUpdate();
            }
        }
    }

    public void Start()
    {
        Result(100, 1f);
    }

    private void Awake()
    {
        CurrentDifficulty = _difficulty;
    }

    public void DifficultyUpdate()
    {
        CurrentDifficulty = _difficulty;
        DifficultyChanged?.Invoke(_difficulty);
    }

    private int _currentDayScore;
     
    public int currentDayScore
    {
        get => _currentDayScore;
        set
        {
            if (_currentDayScore != value)
            {
                _currentDayScore = value;
                CurrentDayScoreChanged?.Invoke(this, EventArgs.Empty);
                UpdateStarRating();
            }
        }
    }

    public bool gameEnd
    {
        get => _gameEnd;
        set
        {
            if (_gameEnd != value)
            {
                _gameEnd = value;
                CurrentDayScoreChanged?.Invoke(this, EventArgs.Empty);
                UpdateStarRating();
            }
        }
    }

    public void Result(int points, float patienceRatio)
    {
        if (_gameEnd) return;

        currentCustomers++;
        _totalSatisfaction += Mathf.Clamp01(patienceRatio);
        currentDayScore += points;
        playerFunds = Mathf.Clamp(playerFunds + points, 0, int.MaxValue);
        UpdateStarRating();
    }

    public void AddFunds(int amount)
    {
        playerFunds = Mathf.Max(0, playerFunds + amount);
        CurrentDayScoreChanged?.Invoke(this, EventArgs.Empty); // lets the UI refresh
    }

    public void EndGame()
    {
        if (_gameEnd) return; 
        gameEnd = true;
        decideWin();
    }

    private void decideWin()
    {
        // your original check: more than 4 stars is a win
        bool won = stars >= LevelSettings.StarsToWin;

        if (won)
        {
            Debug.Log("LEVEL COMPLETE");
            LevelProgress.MarkComplete(UnityEngine.SceneManagement.SceneManager.GetActiveScene().name);
        }

        // your original code put both screens inside the win check, so the lose screen could never show alone
        if (winScreen != null) winScreen.SetActive(won);
        if (loseScreen != null) loseScreen.SetActive(!won);

        GameObject screen = won ? winScreen : loseScreen;
        Time.timeScale = 0.1f;
        // finds the text on the screen object or any child (true = include inactive children)
        TMP_Text text = screen.GetComponentInChildren<TMP_Text>(true);
        if (text != null) StartCoroutine(PopText(text, 1f, 250f));

        // StartCoroutine(DelaySwitchScene(2f, "MainMenu")); game ending will spawn the win/loss buttons instead now
    }

    private IEnumerator PopText(TMP_Text text, float duration, float targetSize)
    {
        text.fontSize = 0f;

        float elapsed = 0f;
        while (elapsed < duration)
        {
            //ignores time scale
            elapsed += Time.unscaledDeltaTime;
            float lerpSize = Mathf.Clamp01(elapsed / duration);

            //sets size over 250 slightly and goes back
            const float extraSize = 1.9f;
            const float size = extraSize + 1f;
            float eased = 1f + size * Mathf.Pow(lerpSize - 1f, 3f) + extraSize * Mathf.Pow(lerpSize - 1f, 2f);

            text.fontSize = targetSize * eased;
            yield return null;
        }

        text.fontSize = targetSize; //Target size is 250
    }

/*    private IEnumerator DelaySwitchScene(float delay, string sceneName)
    {
        yield return new WaitForSecondsRealtime(delay);
        Time.timeScale = 1f;
        UnityEngine.SceneManagement.SceneManager.LoadScene(sceneName);
    } no longer used*/


    public void UpdateStarRating()
    {
        if (currentCustomers <= 0)
        {
            stars = 0f;
            return;
        }

        float averageSatisfaction = _totalSatisfaction / currentCustomers;

        stars = Mathf.FloorToInt(averageSatisfaction * maxStars * 10f) / 10f;
        Debug.Log(averageSatisfaction + " current satisfaction, " + stars + " stars, customers " + currentCustomers);

        starsText.text = stars + " Stars";
        fillImage.fillAmount = stars / maxStars;
    }
}