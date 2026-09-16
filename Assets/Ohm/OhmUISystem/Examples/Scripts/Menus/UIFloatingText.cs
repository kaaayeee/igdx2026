using System.Collections;
using TMPro;
using UnityEngine;
using Ohm.UISystem;

[System.Serializable]
public struct FloatingTextData
{
    public string text;
    public Color color;
    public Vector2 position; // anchored position (in the layer root's rect) the banner settles at

    public FloatingTextData(string text, Color color, Vector2 position)
    {
        this.text = text;
        this.color = color;
        this.position = position;
    }
}

/// <summary>
/// Example UI for the <c>UIManager.ShowUIForDuration</c> API: a centered status banner that fades
/// and rises into place, then simply holds. Unlike <see cref="UIFloatingNumber"/>, this UI does
/// NOT time its own hide — it never calls <c>Hide()</c>. The UIManager hides it automatically when
/// the duration passed to <c>ShowUIForDuration</c> elapses. Drives its own CanvasGroup for the
/// fade-in (no TransitionController).
/// </summary>
public class UIFloatingText : UIBase, IUIInjectable<FloatingTextData>
{
    [Header("References")]
    [SerializeField] private TextMeshProUGUI label;
    private RectTransform rect;
    private Vector2 position;

    private void Awake()
    {
        rect = (RectTransform)transform;
    }

    public void Inject(FloatingTextData data)
    {
        if (label != null)
        {
            label.text = data.text;
            label.color = data.color;
        }
        position = data.position;
    }

    public override void Show(bool instant = false)
    {
        if (rect != null && position != null) 
        {
            rect.anchoredPosition = position;
        }
        base.Show(instant);
    }

}
