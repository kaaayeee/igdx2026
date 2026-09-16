using UnityEngine;
using UnityEngine.UI;
using Ohm.UISystem;

public class UIPauseMenu : UIBase
{
    [SerializeField] private Button resumeButton;
    [SerializeField] private Button settingsButton;

    void Awake()
    {
        if (resumeButton != null)
            resumeButton.onClick.AddListener(ResumeButton);
        if (settingsButton != null)
            settingsButton.onClick.AddListener(ShowSettingsMenu);
    }

    public void ResumeButton()
    {
        UIManager.Instance.OnEscape();
    }
    public void ShowSettingsMenu()
    {
        UIManager.Instance.ShowUI(UIType.UISettings);
    }
}
