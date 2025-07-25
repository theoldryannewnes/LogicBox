using System.Collections;
using TMPro;
using Unity.VisualScripting;
using UnityEngine;

public class ScoreManager : MonoBehaviour
{
    //Create Singleton
    public static ScoreManager Instance { get; private set; }

    //GameTimer Variables
    private float _gameTimeSeconds = 0f;
    private string _formattedGameTime = "00:00";
    public bool _isTimerRunning = false;

    //Score Variables
    private int _currentTurns = 0;
    private int _currentScore = 0;
    private float currentMultiplier = 1.0f;
    private int consecutiveMatches = 0;

    [Header("UI References")]
    public TMP_Text gameTimerText;
    public TMP_Text scoreText;
    public TMP_Text turnsText;

    [Header("Score Increment Values")]
    public int baseMatchPoints = 1;
    public float consecutiveMultiplierIncrease = 1.0f;

    private void Awake()
    {
        // Implement Singleton pattern
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
    }

    private IEnumerator GameTimerRoutine()
    {
        while (_isTimerRunning && ! GameManager.Instance._isGameOver)
        {
            yield return new WaitForSeconds(1f);
            _gameTimeSeconds++;

            // Format the time into MM:SS
            int minutes = Mathf.FloorToInt(_gameTimeSeconds / 60);
            int seconds = Mathf.FloorToInt(_gameTimeSeconds % 60);
            _formattedGameTime = string.Format("{0:00}:{1:00}", minutes, seconds);

            UpdateGameTimerDisplay();
        }
    }

    // Helper functions to update the timer UI
    private void UpdateGameTimerDisplay(bool forceZero = false)
    {
        if (gameTimerText != null)
        {
            if (forceZero)
            {
                gameTimerText.text = $"Time: 0:00";
            }
            else
            {
                gameTimerText.text = $"Time: {_formattedGameTime}";
            }
        }
    }

    private void UpdateScoreDisplay()
    {
        if (scoreText != null)
        {
            scoreText.text = $"Score: {_currentScore} \n Combo: {consecutiveMatches}";
        }
    }

    private void UpdateTurnsDisplay()
    {
        if (turnsText != null)
        {
            turnsText.text = $"Turns: {_currentTurns}";
        }
    }
    // End Helpers

    public void ResetScore()
    {
        _isTimerRunning = false;
        _gameTimeSeconds = 0;
        _currentTurns = 0;
        _currentScore = 0;

        if (gameTimerText != null)
        {
            gameTimerText.text = $"Time: 0:00";
        }

        ResetComboMultiplier();
    }

    public void ResetComboMultiplier()
    {
        consecutiveMatches = 0;
        currentMultiplier = 1.0f;

        //Update ALL UI Text
        UpdateScoreDisplay();
        UpdateTurnsDisplay();
        UpdateGameTimerDisplay(true);
    }

    public void StartGameTimer()
    {
        _isTimerRunning = true;
        StartCoroutine(GameTimerRoutine());
    }

    public void AddPoints()
    {
        consecutiveMatches++;
        currentMultiplier = 1.0f + (consecutiveMatches - 1) * consecutiveMultiplierIncrease;

        int pointsEarned = Mathf.RoundToInt(baseMatchPoints * currentMultiplier);
        _currentScore += pointsEarned;
        UpdateScoreDisplay();

        Debug.Log($"Points: {pointsEarned} (Base: {baseMatchPoints}, Multiplier: {currentMultiplier:F1}). Total Score: {_currentScore}");
    }

    public void AddTurn()
    {
        _currentTurns++;
        UpdateTurnsDisplay();
    }

    public float GetTimeElapsed() { return _gameTimeSeconds; }
    public int GetTurns() { return _currentTurns; }
    public int GetScore() { return _currentScore; }
    public int GetComboScore() { return consecutiveMatches; }

    public void LoadScore(int score, int combo, int turns, float time)
    {
        _currentScore = score;
        consecutiveMatches = combo;
        _currentTurns = turns;
        _gameTimeSeconds = time;

        //Recalculate Combo multiplier
        if (consecutiveMatches > 0)
        {
            currentMultiplier = 1.0f + (consecutiveMatches - 1) * consecutiveMultiplierIncrease;
        }
        else
        {
            currentMultiplier = 1.0f;
        }

        // Update UI text fields here
        UpdateGameTimerDisplay();
        UpdateScoreDisplay();
        UpdateTurnsDisplay();

        // Resume the timer
        StartGameTimer();
    }

}
