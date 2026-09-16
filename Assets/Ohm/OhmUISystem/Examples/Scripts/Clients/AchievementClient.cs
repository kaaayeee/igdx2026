using UnityEngine;
using Ohm.UISystem;

public class AchievementClient : MonoBehaviour
{
    [SerializeField] UIAchievementData achievementData;
    public void ShowAchievement()
    {
        var data = achievementData;
        UIManager.Instance.ShowUI<UIAchievementData>(UIType.UIAchievement, data);
    }
    public void HideAchievement()
    {
        UIManager.Instance.CloseUI(UIType.UIAchievement);
    }
}
