using UnityEngine;
using DG.Tweening;
using System.Collections;
using UnityEngine.UI;


public class UIBase : MonoBehaviour
{
    [SerializeField] private AnimationController animationController;
    [SerializeField] private Button firstSelected;
    [ReadOnly] public bool isActive;

    public AnimationController AnimationController => animationController;
    public virtual float AnimationDuration => animationController != null ? animationController.Duration : 0f;
    
    public virtual void Show()
    {
        isActive = true;
        gameObject.SetActive(true);
        Debug.Log("Show UI: " + gameObject.name);
        if(animationController != null) animationController.Show();
    }

    public virtual void Hide()
    {
        if (animationController != null) animationController.Hide();
        else gameObject.SetActive(false);
        
        isActive = false;
    }
}
