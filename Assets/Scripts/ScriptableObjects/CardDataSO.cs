using UnityEngine;

//Create Card Values to display in game
[CreateAssetMenu(fileName = "New Card", menuName = "Game/Card Data")]
public class CardDataSO : ScriptableObject
{
    // Card ID used to match
    public int cardID;

    // Card Display Settings
    public Sprite frontSprite;
    public string cardName;
}
