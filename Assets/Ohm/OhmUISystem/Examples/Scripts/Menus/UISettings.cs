using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using Ohm.UISystem;

public class UISettings : UIBase
{
    [SerializeField] private Button closeButton;
    [Header("Menu & Button Setup")]
    [SerializeField] private List<TransitionBase> settingMenus = new List<TransitionBase>();
    [SerializeField] private List<Button> menuButtons = new List<Button>();

    private void Start()
    {
        SetupButtons();
        if(closeButton != null)
            closeButton.onClick.AddListener(CloseButton);
    }

    private void SetupButtons()
    {
        for (int i = 0; i < menuButtons.Count; i++)
        {
            int index = i; 
            if (menuButtons[index] != null)
            {
                menuButtons[index].onClick.RemoveAllListeners();
                menuButtons[index].onClick.AddListener(() => OpenSettingMenu(index));
            }
        }
    }

    public void OpenSettingMenu(int targetIndex)
    {
        for (int i = 0; i < settingMenus.Count; i++)
        {
            if (settingMenus[i] == null) continue;

            if (i == targetIndex)
            {
                settingMenus[i].TriggerShow();
            }
            else
            {
                settingMenus[i].TriggerHide();
            }
        }
    }

    public void CloseButton()
    {
        UIManager.Instance.OnEscape();
    }
}