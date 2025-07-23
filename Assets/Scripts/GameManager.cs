using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class GameManager : MonoBehaviour
{
    //Create Singleton
    public static GameManager Instance { get; private set; }

    //Private Variables
    private GameSettingsSO currentSettings;

    private bool _isGameRunning;

    [Header("Game Settings")]
    // Assign the settings grids we created
    public GameSettingsSO easyGameSettings;
    public GameSettingsSO mediumGameSettings;
    public GameSettingsSO hardGameSettings;

    [Header("Card Settings")]
    public Transform cardParentTransform;
    public GameObject cardPrefab;
    // Assign the card data objects we created
    public List<CardDataSO> availableCardData;

    [Header("Animator Setup")]
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
    }


    public void StartGame(int sizeIndex)
    {
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

        canvasAnimator.Play("StartGame");
        _isGameRunning = true;

        SetupMap();
    }

    private void SetupMap()
    {
        List<CardDataSO> selectedCardData = new List<CardDataSO>();
        List<CardDataSO> shuffledAvailableCards = availableCardData.OrderBy(x => Random.value).ToList();

        selectedCardData = shuffledAvailableCards;

        for (int i = 0; i < 15; i++)
        {
            GameObject cardGO = Instantiate(cardPrefab, cardParentTransform);
            Card card = cardGO.GetComponent<Card>();
            if (card != null)
            {
                card.Initialize(selectedCardData[i]);
            }
        }

    }

}
