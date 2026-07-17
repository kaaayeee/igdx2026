using UnityEngine;
using TMPro;
using UnityEngine.UI;

public class RhythmUIManager : MonoBehaviour
{
    public static RhythmUIManager Instance;

    [Header("UI References")]
    public TextMeshProUGUI feedbackText;
    public Slider styleMeterSlider;

    [Header("Style Meter Settings")]
    public float maxStyle = 100f;
    public float startingStyle = 50f;
    
    private float currentStyle;

    private void Awake()
    {
        if (Instance == null) Instance = this;
        else Destroy(gameObject);
    }

    private void Start()
    {
        currentStyle = startingStyle;

        if (styleMeterSlider != null)
        {
            styleMeterSlider.maxValue = maxStyle;
            styleMeterSlider.value = currentStyle;
        }
        
        if (feedbackText != null)
        {
            feedbackText.text = "";
        }
    }

    public void HitFeedback(string judgment)
    {
        if (feedbackText != null)
        {
            feedbackText.text = judgment + "!";
            
            if (judgment.Contains("Perfect")) feedbackText.color = Color.yellow;
            else if (judgment.Contains("Good")) feedbackText.color = Color.green;
            else if (judgment.Contains("Miss")) feedbackText.color = Color.red;
        }

        if (judgment.Contains("Perfect")) currentStyle += 5f;
        else if (judgment.Contains("Good")) currentStyle += 2f;
        else if (judgment.Contains("Miss")) currentStyle -= 10f;

        currentStyle = Mathf.Clamp(currentStyle, 0, maxStyle);

        if (styleMeterSlider != null)
        {
            styleMeterSlider.value = currentStyle;
        }
    }
}
