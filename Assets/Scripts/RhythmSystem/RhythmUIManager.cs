using UnityEngine;
using TMPro; 

public class RhythmUIManager : MonoBehaviour
{
    [Header("UI References")]
    [Tooltip("Text untuk menampilkan Perfect, Good, Miss")]
    public TextMeshProUGUI feedbackText; 
    
    [Header("Settings")]
    public float displayDuration = 1f;
    private float hideTimer = 0f;

    private void Start()
    {
        if (feedbackText != null) feedbackText.text = "";
    }

    private void Update()
    {
        if (hideTimer > 0)
        {
            hideTimer -= Time.deltaTime;
            if (hideTimer <= 0 && feedbackText != null)
            {
                feedbackText.text = "";
            }
        }
    }

    public void ShowFeedback(string text)
    {
        if (feedbackText != null)
        {
            feedbackText.text = text;
            hideTimer = displayDuration;
        }
        
        Debug.Log("Hit Feedback: " + text);
    }
}
