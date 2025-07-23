using UnityEngine;

// Create Settings values for game difficulty
[CreateAssetMenu(fileName = "New Game Difficulty", menuName = "Game/Game Settings")]
public class GameSettingsSO : ScriptableObject
{
    // Grid will have rows & columns
    public int rows;
    public int columns;

    // Name of this difficulty level
    public string gameSizeName;

    // Calculated values
    public int TotalCards => rows * columns;

    public int TotalPairs => TotalCards / 2;

}
