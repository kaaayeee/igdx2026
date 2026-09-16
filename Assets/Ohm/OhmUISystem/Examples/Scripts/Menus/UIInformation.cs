using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using Ohm.UISystem;

[System.Serializable]
public struct UIInformationData
{
    public string title;
    public string message;
    public Vector2 position;

    public UIInformationData(string title, string message, Vector2 position = default)
    {
        this.title = title;
        this.message = message;
        this.position = position;
    }
}

public class UIInformation : UIBase, IUIInjectable<UIInformationData>
{
    [Header("References")]
    [SerializeField] private TextMeshProUGUI titleText;
    [SerializeField] private TextMeshProUGUI messageText;

    private RectTransform rectTransform;
    private Vector2 targetPosition;

    private RectTransform RectTransform
    {
        get
        {
            if (rectTransform == null) rectTransform = transform as RectTransform;
            return rectTransform;
        }
    }

    public void Inject(UIInformationData data)
    {
        if(titleText != null) titleText.text = data.title;
        if(messageText != null) messageText.text = data.message;
        targetPosition = data.position;
    }
    public override void Show(bool instant = false)
    {
        RectTransform.anchoredPosition = targetPosition;
        base.Show(instant);
    }
}
