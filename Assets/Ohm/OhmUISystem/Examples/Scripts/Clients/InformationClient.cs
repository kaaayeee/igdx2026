using UnityEngine;
using Ohm.UISystem;

public class InformationClient : MonoBehaviour
{
    [SerializeField] UIInformationData informationData;
    [Tooltip("If assigned, overrides informationData.position with this transform's position")]
    [SerializeField] private Transform positionReference;

    public void ShowInformation()
    {
        var data = informationData;
        if (positionReference != null) 
        {
            Camera mainCam = Camera.main;
            if (mainCam != null)
            {
                Vector2 screenPoint = mainCam.WorldToScreenPoint(positionReference.position);
                
                Transform uiParent = UIManager.Instance.Parent != null ? UIManager.Instance.Parent : UIManager.Instance.transform;
                Canvas canvas = uiParent.GetComponentInParent<Canvas>();
                
                Camera uiCamera = null;
                if (canvas != null && canvas.renderMode != RenderMode.ScreenSpaceOverlay)
                {
                    uiCamera = canvas.worldCamera ?? mainCam;
                }

                if (uiParent is RectTransform parentRect)
                {
                    RectTransformUtility.ScreenPointToLocalPointInRectangle(parentRect, screenPoint, uiCamera, out Vector2 localPoint);
                    data.position = localPoint;
                }
                else
                {
                    data.position = screenPoint;
                }
            }
        }

        UIManager.Instance.ShowUI<UIInformationData>(UIType.UIInformation, data);
    }
    public void HideInformation()
    {
        UIManager.Instance.CloseUI(UIType.UIInformation);
    }
}
