using UnityEngine;
using System;

public class PlayerInfo : MonoBehaviour
{
    //Player funds and stats here
    public int playerFunds;

    public int difficulty;
    public int day;
    //Could expand to save unlocked workers

    public int currentCustomers;
    public event EventHandler CurrentDayScoreChanged;
    public float stars;

    public int maxScorePerCustomer = 100;
    public float maxStars = 5f;

    private bool _gameEnd = false;

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

    public void Result(int points)
    {
        if (_gameEnd) return;                 // game over: nothing changes

        currentCustomers++;                   // count first so the average uses the new total
        currentDayScore += points;            // setter fires the event and refreshes stars if the score changed
        playerFunds = Mathf.Clamp(playerFunds + points, 0, int.MaxValue);
        UpdateStarRating();                   // also refresh when points is 0, since the setter won't fire
    }

    public void EndGame()
    {
        gameEnd = true;
    }

    public void UpdateStarRating()
    {
        if (currentCustomers <= 0)
        {
            stars = 0f;   // nobody finished yet, and this avoids dividing by zero
            return;
        }

        float averageScore = (float)_currentDayScore / currentCustomers;   // float cast stops integer division
        float ratio = Mathf.Clamp01(averageScore / maxScorePerCustomer);

        stars = Mathf.FloorToInt(ratio * maxStars * 10f) / 10f;   // one decimal place
        Debug.Log(ratio + " current rating, " + stars + " stars, customers " + currentCustomers);

    }
}