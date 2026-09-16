using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections;
using Ohm.UISystem;

[System.Serializable]
public struct UIAchievementData
{
    public Sprite titleImage;
    public string title;
    public string message;

    public UIAchievementData(Sprite titleImage, string title, string message, Vector2 position = default)
    {
        this.titleImage = titleImage;
        this.title = title;
        this.message = message;
    }
}
public class UIAchievement : UIBase, IUIInjectable<UIAchievementData>
{
    [Header("References")]
    [SerializeField] private Image titleImage;
    [SerializeField] private TextMeshProUGUI titleText;
    [SerializeField] private TextMeshProUGUI messageText;

    [Header("Settings")]
    [SerializeField] private float displayDuration = 3f;
    

    public void Inject(UIAchievementData data)
    {
        if(titleImage != null) titleImage.sprite = data.titleImage;
        if(titleText != null) titleText.text = data.title;
        if(messageText != null) messageText.text = data.message;
    }
    public override void Show(bool instant = false)
    {
        base.Show(instant);
        StartCoroutine(ShowAndHideCoroutine(displayDuration));
    }

    public IEnumerator ShowAndHideCoroutine(float duration)
    {
        yield return new WaitForSecondsRealtime(duration);
        Hide();
    }
}
