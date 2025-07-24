using System.Collections;
using System.Collections.Generic;
using System.Linq;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;

public class GameManager : MonoBehaviour
{
    //Create Singleton
    public static GameManager Instance { get; private set; }

    //Private Variables
    private GameSettingsSO currentSettings;
    private int totalMatchesNeeded;

    private List<Card> allCards = new List<Card>();
    private List<Card> _openCards = new List<Card>();

    private int matchesFound = 0;

    //GameState Variables
    private bool _isGameRunning;
    private bool _isInitialRevealPhase;
    private bool _isProcessingMatches;
    public bool _isGameOver;

    [Header("Game Settings")]
    // Assign the settings grids we created
    public GameSettingsSO easyGameSettings;
    public GameSettingsSO mediumGameSettings;
    public GameSettingsSO hardGameSettings;

    // Seconds for whiuch cards are revealed at start
    public float initialRevealTime = 3f;

    //Secods for which matched cards are displayed
    public float matchProcessingDelay = 0.5f;

    //Seconds to flip cards to back if not a match
    public float noMatchFlipBackDelay = 1f;

    [Header("Card Settings")]
    public Transform cardParentTransform;
    public GameObject cardPrefab;

    // Assign the card data objects we created
    public List<CardDataSO> availableCardData;

    [Header("Scene Setup")]
    [SerializeField] private ScoreManager scoreManager;
    [SerializeField] private Animator canvasAnimator;

    void Awake()
    {
        // Implement Singleton pattern
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject); // Destroy duplicate GameManager
            return;
        }
        Instance = this;

        RestartGame();
    }

    public void RestartGame()
    {
        scoreManager._isTimerRunning = false;
        canvasAnimator.Play("RestartGame");
    }

    private void ResetGame()
    {
        //Clear open cards
        _openCards.Clear();

        //Reset matchesFound count
        matchesFound = 0;

        foreach (Card card in allCards)
        {
            if (card != null && card.gameObject != null)
            {
                Destroy(card.gameObject);
            }
        }
        //Clear Cards in grid
        allCards.Clear();
    }

    private void ResetGameStates()
    {
        _isGameRunning = false;
        _isInitialRevealPhase = false;
        _isGameOver = false;
        _isProcessingMatches = false;
    }


    public void StartGame(int sizeIndex)
    {
        //Reset all variables
        ResetGame();
        ResetGameStates();

        // Set current game settings based on sizeIndex
        switch (sizeIndex)
        {
            case 0: // Easy
                currentSettings = easyGameSettings;
                break;
            case 1: // Medium
                currentSettings = mediumGameSettings;
                break;
            case 2: // Hard
                currentSettings = hardGameSettings;
                break;
            default:
                Debug.Log("Invalid GameSize index!");
                return;
        }

        Debug.Log($"New Game >> Mode:{currentSettings.gameSizeName} ({currentSettings.rows}x{currentSettings.columns})");

        totalMatchesNeeded = currentSettings.TotalPairs;

        //Check if we have enough cards in Available Cards
        if (availableCardData == null || availableCardData.Count < totalMatchesNeeded)
        {
            Debug.LogError("Not enough unique CardDataSOs available!");
            return;
        }

        canvasAnimator.Play("StartGame");
        _isGameRunning = true;

        //Reset Score Variables
        scoreManager.ResetScore();

        SetupMap();
    }

    private void SetupMap()
    {
        // Randonly select a subset of the card data we setup
        List<CardDataSO> selectedCardData = new List<CardDataSO>();
        List<CardDataSO> shuffledAvailableCards = availableCardData.OrderBy(x => Random.value).ToList();

        // Generate a list of pairs
        for (int i = 0; i < totalMatchesNeeded; i++)
        {
            selectedCardData.Add(shuffledAvailableCards[i]);
            selectedCardData.Add(shuffledAvailableCards[i]);
        }

        // TODO: We need to shuffle this grid

        // Populate the grid
        int cardIndex = 0;
        for (int row = 0; row < currentSettings.rows; row++)
        {
            for (int col = 0; col < currentSettings.columns; col++)
            {
                GameObject cardGO = Instantiate(cardPrefab, cardParentTransform);
                Card card = cardGO.GetComponent<Card>();
                if (card != null)
                {
                    card.Initialize(cardIndex, selectedCardData[cardIndex]);
                    allCards.Add(card);
                }
                else
                {
                    Debug.Log("Card prefab is missing a Card component!");
                }
                cardIndex++;
            }
        }

        // Reveal Cards before game starts
        StartCoroutine(InitialCardRevealRoutine());
    }

    private IEnumerator InitialCardRevealRoutine()
    {
        _isInitialRevealPhase = true;

        // Temporarily disable input on all cards during reveal phase
        SetCardsClickable(false);

        foreach (Card card in allCards)
        {
            card.FlipToFront(instant: true);
        }

        yield return new WaitForSeconds(initialRevealTime);

        foreach (Card card in allCards)
        {
            card.FlipToBack(instant: true);
        }

        _isInitialRevealPhase = false;
        // Re-enable input for the game to start
        SetCardsClickable(true);

        Debug.Log("Game started! Player can now flip cards.");

        //Start Game Timer Routine
        scoreManager.StartGameTimer();
    }

    // Function to enable/disable card input
    private void SetCardsClickable(bool clickable)
    {
        foreach (Card card in allCards)
        {
            card.SetClickable(clickable);
        }
    }

    public void CardClicked(Card clickedCard)
    {
        // cHECK game state before going forward
        if (!_isGameRunning || _isInitialRevealPhase || _isGameOver)
        {
            Debug.Log($"Ignoring click. Game not in active state.");
            return;
        }

        // Flip the card to show front
        clickedCard.FlipToFront();

        // Add each flipped card to a List
        _openCards.Add(clickedCard);

        // If we have at least two open cards, start/continue processing matches
        if (_openCards.Count >= 2 && !_isProcessingMatches)
        {
            StartCoroutine(ProcessPendingMatchesRoutine());
        }
    }

    private IEnumerator ProcessPendingMatchesRoutine()
    {
        _isProcessingMatches = true;

        while (_openCards.Count >= 2)
        {
            //Increment Turn Counter
            scoreManager.AddTurn();

            // Take last 2 cards in _openCards
            Card c1 = _openCards[0];
            Card c2 = _openCards[1];

            //Remove last 2 cards in _openCards
            _openCards.RemoveAt(0);
            _openCards.RemoveAt(0);

            Debug.Log($"Processing matches between: {c1.CardValue} and {c2.CardValue}");

            //Small delay to see cards
            yield return new WaitForSeconds(matchProcessingDelay);

            if (c1.CardValue == c2.CardValue)
            {
                // Match
                Debug.Log($"Match Found between {c1.CardValue} and {c2.CardValue}!");
                matchesFound++;

                scoreManager.AddPoints();

                // Fade out & remove cards from grid
                c1.SetMatched();
                c2.SetMatched();

                // Check for Game End
                if (matchesFound >= totalMatchesNeeded)
                {
                    _isGameOver = true;
                    _isGameRunning = false;
                    Debug.Log("Game Over! All matches found!");

                    //Disable all clicks when game is over
                    SetCardsClickable(false);

                    //Exit if Game ends
                    yield break;
                }
            }
            else
            {
                // No Match
                Debug.Log($"No Match between {c1.CardValue} and {c2.CardValue}. Flipping cards back.");

                scoreManager.ResetComboMultiplier();

                // Wait before flipping back
                yield return new WaitForSeconds(noMatchFlipBackDelay);

                c1.FlipToBack();
                c2.FlipToBack();
            }
        }

        _isProcessingMatches = false;

    }

}
