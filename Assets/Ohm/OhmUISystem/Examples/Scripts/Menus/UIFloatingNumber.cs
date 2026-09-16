using System.Collections;
using TMPro;
using UnityEngine;
using Ohm.UISystem;

[System.Serializable]
public struct FloatingNumberData
{
    public string text;
    public Color color;
    public Vector2 position; // anchored position in the layer root's rect

    public FloatingNumberData(string text, Color color, Vector2 position)
    {
        this.text = text;
        this.color = color;
        this.position = position;
    }
}

/// <summary>
/// Detached + pooled example UI: a floating "damage number" that rises and fades, then hides
/// itself (which returns it to the UIManager pool). Many can be alive at once. This prefab uses
/// no TransitionController — it drives its own CanvasGroup for the fade.
/// </summary>
public class UIFloatingNumber : UIBase, IUIInjectable<FloatingNumberData>
{
    [Header("References")]
    [SerializeField] private TextMeshProUGUI label;

    [Header("Motion")]
    [Tooltip("How long the number lives before it hides itself (seconds).")]
    [SerializeField] private float lifetime = 1f;
    [Tooltip("How far the number rises over its lifetime (anchored units).")]
    [SerializeField] private float riseDistance = 120f;
    [Tooltip("Random horizontal offset so overlapping numbers don't stack perfectly.")]
    [SerializeField] private float horizontalJitter = 40f;
    [SerializeField] private AnimationCurve alphaOverLife = AnimationCurve.Linear(0f, 1f, 1f, 0f);
    [SerializeField] private AnimationCurve riseOverLife = AnimationCurve.EaseInOut(0f, 0f, 1f, 1f);

    private RectTransform rect;
    private CanvasGroup canvasGroup;
    private Vector2 start;
    private Coroutine animation;

    private void Awake()
    {
        rect = (RectTransform)transform;
        canvasGroup = GetComponent<CanvasGroup>();
    }

    public void Inject(FloatingNumberData data)
    {
        if (label != null)
        {
            label.text = data.text;
            label.color = data.color;
        }

        start = data.position + new Vector2(Random.Range(-horizontalJitter, horizontalJitter), 0f);
    }

    public override void Show(bool instant = false)
    {
        base.Show(instant);

        rect.anchoredPosition = start;
        if (canvasGroup != null) canvasGroup.alpha = 1f;

        if (animation != null) StopCoroutine(animation);
        animation = StartCoroutine(Animate());
    }

    private IEnumerator Animate()
    {
        float t = 0f;
        while (t < lifetime)
        {
            float k = t / lifetime;
            if (canvasGroup != null) canvasGroup.alpha = alphaOverLife.Evaluate(k);
            rect.anchoredPosition = start + Vector2.up * (riseDistance * riseOverLife.Evaluate(k));

            // Unscaled so numbers still animate while the game is paused by a menu.
            t += Time.unscaledDeltaTime;
            yield return null;
        }

        animation = null;
        Hide(); // fires UIBase.Hidden -> UIManager returns this instance to the pool
    }
}
