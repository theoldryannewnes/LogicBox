using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class Card : MonoBehaviour
{
    //Private Members
    private bool _isFlipped;
    private bool _isClickable;

    private CardDataSO _cardData;
    private int _cardValue;
    private int _cardID;

    //Public Members
    public int CardValue => _cardValue;

    //Card's front and back face
    public Image frontFaceImage;
    public Image backFaceImage;
    // Text value to use instead of images
    public TMP_Text valueText;

    // Default image used as the card's back
    public Sprite defaultCardBackSprite;

    // Flip Animtion time
    [SerializeField] private float flipDuration = 0.3f;

    public void Initialize(int id, CardDataSO data)
    {
        _cardID = id; // Index of card added  to grid
        _cardData = data;
        _cardValue = data.cardID; // Use the cardID from the CardDataSO for matching

        // Set the front face sprite from the CardDataSO
        if (frontFaceImage != null && data.frontSprite != null)
        {
            frontFaceImage.sprite = data.frontSprite;
        }
        else
        {
            Debug.Log($"Card: Front face image or CardDataSO sprite is missing.");
        }

        if (valueText != null)
        {
            valueText.text = _cardValue.ToString();
        }

        backFaceImage.sprite = defaultCardBackSprite;

        //Flip card after setup
        FlipToBack(instant: true);

        SetClickable(false);
    }

    public void FlipToBack(bool instant = false)
    {
        if (!_isFlipped && !instant) return;

        _isFlipped = false;
        if (instant)
        {
            frontFaceImage.gameObject.SetActive(false);
            backFaceImage.gameObject.SetActive(true);
        }
        else
        {
            StartCoroutine(FlipRoutine(false));
        }
    }

    public void FlipToFront(bool instant = false)
    {
        if (_isFlipped && !instant) return;

        _isFlipped = true;
        if (instant)
        {
            frontFaceImage.gameObject.SetActive(true);
            backFaceImage.gameObject.SetActive(false);
        }
        else
        {
            StartCoroutine(FlipRoutine(true));
        }
    }

    // Routine to Flip cards back & front
    private IEnumerator FlipRoutine(bool toFront)
    {
        float timer = 0f;
        Vector3 startScale = transform.localScale;
        Vector3 endScale = new Vector3(0, startScale.y, startScale.z); // Scale to 0 on X for half flip

        // First half of flip (to flat)
        while (timer < flipDuration / 2f)
        {
            timer += Time.deltaTime;
            transform.localScale = Vector3.Lerp(startScale, endScale, timer / (flipDuration / 2f));
            yield return null;
        }

        // Switch faces at the halfway point
        if (toFront)
        {
            frontFaceImage.gameObject.SetActive(true);
            backFaceImage.gameObject.SetActive(false);
        }
        else
        {
            frontFaceImage.gameObject.SetActive(false);
            backFaceImage.gameObject.SetActive(true);
        }

        // Second half of flip (from flat to full)
        timer = 0f;
        startScale = new Vector3(0, startScale.y, startScale.z);
        endScale = new Vector3(1, startScale.y, startScale.z); // Scale back to 1 on X

        while (timer < flipDuration / 2f)
        {
            timer += Time.deltaTime;
            transform.localScale = Vector3.Lerp(startScale, endScale, timer / (flipDuration / 2f));
            yield return null;
        }

        transform.localScale = new Vector3(1, transform.localScale.y, transform.localScale.z); // Ensure final scale is 1
    }

    public void SetClickable(bool clickable) => _isClickable = clickable;

    public void OnCardClick()
    {
        if (_isClickable && !_isFlipped)
        {
            // Send card to Gamemanager to check matches
            GameManager.Instance.CardClicked(this);
        }
    }

}
