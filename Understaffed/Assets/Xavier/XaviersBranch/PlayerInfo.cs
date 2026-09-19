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
        Debug.Log(ratio + " current rating, " + stars + " stars");

    }
}