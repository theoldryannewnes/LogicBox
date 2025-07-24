using System.Collections;
using TMPro;
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
    private int currentTurns = 0;
    private int currentScore = 0;
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
            scoreText.text = $"Score: {currentScore}  Combo: {currentMultiplier}";
        }
    }

    private void UpdateTurnsDisplay()
    {
        if (turnsText != null)
        {
            turnsText.text = $"Turns: {currentTurns}";
        }
    }
    // End Helpers

    public void ResetScore()
    {
        _gameTimeSeconds = 0;
        _isTimerRunning = false;

        if (gameTimerText != null)
        {
            gameTimerText.text = $"Time: 0:00";
        }

        currentTurns = 0;
        currentScore = 0;

        ResetComboMultiplier();
        UpdateGameTimerDisplay(true);
    }

    public void ResetComboMultiplier()
    {
        consecutiveMatches = 0;
        currentMultiplier = 1.0f;
        UpdateScoreDisplay();
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
        currentScore += pointsEarned;
        UpdateScoreDisplay();

        Debug.Log($"Points: {pointsEarned} (Base: {baseMatchPoints}, Multiplier: {currentMultiplier:F1}). Total Score: {currentScore}");
    }

    public void AddTurn()
    {
        currentTurns++;
        UpdateTurnsDisplay();
    }

}
