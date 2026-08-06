using UnityEngine;
using TMPro; // Menggunakan TextMeshPro karena standar industri

public class RhythmUIManager : MonoBehaviour
{
    public static RhythmUIManager Instance { get; private set; }

    [Header("UI References")]
    [Tooltip("Text untuk menampilkan Perfect, Good, Miss")]
    public TextMeshProUGUI feedbackText; 
    
    [Header("Settings")]
    public float displayDuration = 1f;
    private float hideTimer = 0f;

    private void Awake()
    {
        if (Instance == null) Instance = this;
        else Destroy(gameObject);
    }

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
            
            // Jika punya Animator untuk text, bisa di-trigger di sini
            // contoh: feedbackText.GetComponent<Animator>().SetTrigger("Pop");
        }
        
        Debug.Log("Hit Feedback: " + text);
    }
}
