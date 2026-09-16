using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using Ohm.UISystem;
using UnityEngine.Events;
[System.Serializable]
public struct UIConfirmationData
{
    public string title;
    public string message;
    public UnityEvent onConfirm;
    public UnityEvent onCancel;

    public UIConfirmationData(string title, string message, UnityEvent onConfirm, UnityEvent onCancel = null)
    {
        this.title = title;
        this.message = message;
        this.onConfirm = onConfirm;
        this.onCancel = onCancel;
    }
}

public class UIConfirmation : UIBase, IUIInjectable<UIConfirmationData>
{
    [Header("References")]
    [SerializeField] private TextMeshProUGUI titleText;
    [SerializeField] private TextMeshProUGUI messageText;
    [SerializeField] private Button confirmButton;
    [SerializeField] private Button cancelButton;

    private Action onConfirm;
    private Action onCancel;

    public void Inject(UIConfirmationData data)
    {
        titleText.text = data.title;
        messageText.text = data.message;
        onConfirm = data.onConfirm.Invoke;
        onCancel = data.onCancel.Invoke;
    }
    void OnEnable()
    {
        confirmButton.onClick.AddListener(Confirm);
        cancelButton.onClick.AddListener(Cancel);
    }

    void OnDisable()
    {
        confirmButton.onClick.RemoveAllListeners();
        cancelButton.onClick.RemoveAllListeners();
    }

    public void Confirm()
    {
        onConfirm?.Invoke();
        CloseUI();
    }
    public void Cancel()
    {
        onCancel?.Invoke();
        CloseUI();
    }


    public void CloseUI()
    {
        UIManager.Instance.OnEscape();
    }
}
