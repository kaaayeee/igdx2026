using UnityEngine;
using UnityEngine.UI;
using Ohm.UISystem;
public class UIGameplay : UIBase
{
    [SerializeField] private Button pauseButton;
    [SerializeField] private Button rotateLeftButton;
    [SerializeField] private Button rotateRightButton;

    void Awake()
    {
        if (pauseButton != null)
            pauseButton.onClick.AddListener(OpenPauseMenu);
        if (rotateLeftButton != null)
            rotateLeftButton.onClick.AddListener(RotateLeft);
        if (rotateRightButton != null)
            rotateRightButton.onClick.AddListener(RotateRight);
    }

    public void OpenPauseMenu()
    {
        if (UIManager.Instance == null) return;
        // Recorded on the Popup layer, so backing out of Settings returns here rather than
        // falling through to Gameplay. UISettings is the one that opts out of history.
        UIManager.Instance.ShowUI(UIType.UIPauseMenu);
    }
    public void RotateLeft()
    {
        if (OhmUISystemDemoManager.Instance == null) return;
        OhmUISystemDemoManager.Instance.RotateLeft();
    }

    public void RotateRight()
    {
        if (OhmUISystemDemoManager.Instance == null) return;
        OhmUISystemDemoManager.Instance.RotateRight();
    }
}
