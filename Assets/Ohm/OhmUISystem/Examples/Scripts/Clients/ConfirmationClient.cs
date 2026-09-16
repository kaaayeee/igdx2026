using UnityEngine;
using Ohm.UISystem;

public class ConfirmationClient : MonoBehaviour
{
    [SerializeField] UIConfirmationData confirmationData = new();

    public void OnEnable()
    {
        confirmationData.onConfirm.AddListener(OnConfirm);
        confirmationData.onCancel.AddListener(OnCancel);
    }
    public void OnDisable()
    {
        confirmationData.onConfirm.RemoveListener(OnConfirm);
        confirmationData.onCancel.RemoveListener(OnCancel);
    }

    public void ShowConfirmation()
    {
        UIManager.Instance.ShowUI<UIConfirmationData>(UIType.UIConfirmation, confirmationData);
    }

    private void OnConfirm()
    {
        Debug.Log("Confirmed!");
    }
    private void OnCancel()
    {
        Debug.Log("Cancelled!");
    }
}
