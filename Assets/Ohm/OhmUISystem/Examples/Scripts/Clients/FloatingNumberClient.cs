using UnityEngine;
using Ohm.UISystem;

/// <summary>
/// Spawns floating "damage numbers" over a world-space point (typically the cube it sits on).
/// Wire <see cref="Spawn"/> to the cube's Interactable ▸ On Click. Every call checks out a
/// pooled instance from the UIManager, so rapid clicks show many numbers at once.
/// </summary>
public class FloatingNumberClient : MonoBehaviour
{
    [Header("UI")]
    [Tooltip("World point the number spawns over. Defaults to this transform.")]
    [SerializeField] private Transform positionReference;

    [Header("Amount")]
    [SerializeField] private int minAmount = 5;
    [SerializeField] private int maxAmount = 30;

    [Header("Crit")]
    [Range(0f, 1f)]
    [SerializeField] private float critChance = 0.2f;
    [SerializeField] private Color normalColor = Color.white;
    [SerializeField] private Color critColor = new Color(1f, 0.85f, 0.2f);

    private void Reset()
    {
        positionReference = transform;
    }

    public void Spawn()
    {
        if (UIManager.Instance == null) return;

        Transform reference = positionReference != null ? positionReference : transform;

        bool crit = Random.value < critChance;
        int amount = Random.Range(minAmount, maxAmount + 1) * (crit ? 2 : 1);

        var data = new FloatingNumberData(
            (crit ? "CRIT " : "") + amount,
            crit ? critColor : normalColor,
            WorldToUILocal(reference.position));

        // ShowUI returns the checked-out pooled instance; the number hides itself after its lifetime.
        UIManager.Instance.ShowUI<FloatingNumberData>(UIType.UIFloatingNumber, data);
    }

    /// <summary>Converts a world position to an anchored position inside the UIManager's canvas rect.</summary>
    private Vector2 WorldToUILocal(Vector3 world)
    {
        Camera mainCam = Camera.main;
        Transform uiParent = UIManager.Instance.Parent != null ? UIManager.Instance.Parent : UIManager.Instance.transform;

        if (mainCam == null || uiParent is not RectTransform parentRect)
            return Vector2.zero;

        Vector2 screenPoint = mainCam.WorldToScreenPoint(world);

        Canvas canvas = uiParent.GetComponentInParent<Canvas>();
        Camera uiCamera = null;
        if (canvas != null && canvas.renderMode != RenderMode.ScreenSpaceOverlay)
            uiCamera = canvas.worldCamera ?? mainCam;

        RectTransformUtility.ScreenPointToLocalPointInRectangle(parentRect, screenPoint, uiCamera, out Vector2 localPoint);
        return localPoint;
    }
}
