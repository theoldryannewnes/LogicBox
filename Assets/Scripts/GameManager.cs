using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

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

    [Header("Save Game Settings")]
    // Controls how often the game is saved
    public float saveInterval = 5.0f;
    private const string SaveKey = "SaveState";
    private Coroutine _saveGameRoutine;
    private Dictionary<int, CardDataSO> cardDataLookup;

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

        // Lookup function for availableCards
        cardDataLookup = availableCardData.ToDictionary(cardData => cardData.cardID, cardData => cardData);

        // Check for saved game
        if (PlayerPrefs.HasKey(SaveKey))
        {
            Debug.Log("Saved game found! Loading...");
            LoadGame();
        }
        else
        {
            Debug.Log("No saved game found, starting new game");
            RestartGame();
        }
    }

    public void RestartGame()
    {
        scoreManager._isTimerRunning = false;
        canvasAnimator.Play("RestartGame");
    }

    private void ResetGame()
    {
        // Stop the saving coroutine if it's running
        if (_saveGameRoutine != null)
        {
            StopCoroutine(_saveGameRoutine);
            _saveGameRoutine = null;
        }

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

        DeleteSaveData();

        // Start Coroutine to save game
        _saveGameRoutine = StartCoroutine(SaveGameRoutine());
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

        //Shuffle using Knuth SHuffle
        Shuffle(selectedCardData);

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

    private void Shuffle<T>(List<T> list)
    {
        System.Random rng = new System.Random();
        int n = list.Count;
        while (n > 1)
        {
            n--;
            int k = rng.Next(n + 1);
            T value = list[k];
            list[k] = list[n];
            list[n] = value;
        }
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
            // If matched don't enablke clicks
            if (!card.IsMatched)
            {
                card.SetClickable(clickable);
            }
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

                Debug.Log($"Matches Found: {matchesFound}  |  Total Matches: {totalMatchesNeeded}");

                // Check for Game End
                if (matchesFound >= totalMatchesNeeded)
                {
                    _isGameOver = true;
                    _isGameRunning = false;
                    Debug.Log("Game Over! All matches found!");

                    //Disable all clicks when game is over
                    SetCardsClickable(false);

                    DeleteSaveData();

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

    // Save Game Periodically
    private IEnumerator SaveGameRoutine()
    {
        while (_isGameRunning && !_isGameOver)
        {
            yield return new WaitForSeconds(saveInterval);
            SaveGameState();
        }
    }

    public void SaveGameState()
    {
        if (!_isGameRunning || _isGameOver) return;

        GameStateData data = new GameStateData();

        // Difficulty Setting index
        if (currentSettings == easyGameSettings) data.difficultyIndex = 0;
        else if (currentSettings == mediumGameSettings) data.difficultyIndex = 1;
        else data.difficultyIndex = 2;

        data.matchesFound = this.matchesFound;

        // Store Game State Data
        data.cardDataNames = new List<int>();
        data.matchedCardIndices = new List<int>();
        for (int i = 0; i < allCards.Count; i++)
        {
            data.cardDataNames.Add(allCards[i].CardValue);
            if (allCards[i].IsMatched)
            {
                data.matchedCardIndices.Add(i);
            }
        }

        data.openCardIndices = _openCards.Select(card => allCards.IndexOf(card)).ToList();

        // Store scoreManager data
        data.timeElapsed = scoreManager.GetTimeElapsed();
        data.turnsTaken = scoreManager.GetTurns();
        data.currentScore = scoreManager.GetScore();
        data.currentCombo = scoreManager.GetComboScore();

        //Create JSON and store in PlayterPrefs
        string json = JsonUtility.ToJson(data);
        PlayerPrefs.SetString(SaveKey, json);
        PlayerPrefs.Save();

        Debug.Log("Game State Saved!");
    }

    // Recreates Entire Game State
    public void LoadGame()
    {
        ResetGame();
        ResetGameStates();

        string json = PlayerPrefs.GetString(SaveKey);
        GameStateData data = JsonUtility.FromJson<GameStateData>(json);

        // Restore game settings
        switch (data.difficultyIndex)
        {
            case 0: currentSettings = easyGameSettings; break;
            case 1: currentSettings = mediumGameSettings; break;
            case 2: currentSettings = hardGameSettings; break;
        }
        totalMatchesNeeded = currentSettings.TotalPairs;
        this.matchesFound = data.matchesFound;

        canvasAnimator.Play("StartGame");

        // Restore the map from saved data
        SetupLoadedMap(data);

        // Restore score
        scoreManager.LoadScore(data.currentScore, data.currentCombo, data.turnsTaken, data.timeElapsed);
        _isGameRunning = true;

        // Enable click handling after loading
        SetCardsClickable(true);

        Debug.Log("Game loaded. Cards are now clickable.");

        // If there were open cards, resume processing them
        if (_openCards.Count >= 2 && !_isProcessingMatches)
        {
            StartCoroutine(ProcessPendingMatchesRoutine());
        }

        // Start the background saving coroutine
        _saveGameRoutine = StartCoroutine(SaveGameRoutine());
    }

    // Recreates Card Grid
    private void SetupLoadedMap(GameStateData data)
    {
        for (int i = 0; i < data.cardDataNames.Count; i++)
        {
            GameObject cardGO = Instantiate(cardPrefab, cardParentTransform);
            Card card = cardGO.GetComponent<Card>();

            // Use lookup Dictionary to fetch card data
            CardDataSO cardData = cardDataLookup[data.cardDataNames[i]];
            card.Initialize(i, cardData);
            allCards.Add(card);

            // Restore matched cards
            if (data.matchedCardIndices.Contains(i))
            {
                card.SetMatched(instant: true);
            }

            // Restore open cards
            if (data.openCardIndices.Contains(i))
            {
                card.FlipToFront(instant: true);
                _openCards.Add(card);
            }
        }
    }

    // Delete SaveGame Data is user starts a new game or game over condition
    public void DeleteSaveData()
    {
        PlayerPrefs.DeleteKey(SaveKey);
        Debug.Log("Save data deleted.");
    }

}

[System.Serializable]
public class GameStateData
{
    public int difficultyIndex;
    public int matchesFound;
    public List<int> cardDataNames;
    public List<int> matchedCardIndices;
    public List<int> openCardIndices;
    public float timeElapsed;
    public int turnsTaken;
    public int currentScore;
    public int currentCombo;
}
