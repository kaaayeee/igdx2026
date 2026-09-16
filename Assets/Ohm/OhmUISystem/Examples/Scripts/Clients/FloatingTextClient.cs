using UnityEngine;
using Ohm.UISystem;

/// <summary>
/// Shows a centered status banner for a fixed duration via <c>UIManager.ShowUIForDuration</c>.
/// Wire <see cref="Spawn"/> to any trigger (a cube's Interactable ▸ On Click, a UI Button, …).
/// The manager auto-hides the banner when the duration elapses — this client starts no coroutine.
/// </summary>
public class FloatingTextClient : MonoBehaviour
{
    [Header("UI")]
    [SerializeField] private float duration = 2f;

    [Tooltip("Anchored position in the layer root the banner settles at (0,0 = center).")]
    [SerializeField] private Vector2 anchoredPosition = new Vector2(0f, 150f);

    [Header("Message")]
    [SerializeField] private string[] messages =
    {
        "Checkpoint reached",
        "Level Up!",
        "Item acquired",
        "Quest updated"
    };
    [SerializeField] private Color color = Color.white;

    /// <summary>Shows a random message from the list.</summary>
    public void Spawn()
    {
        string message = (messages != null && messages.Length > 0)
            ? messages[Random.Range(0, messages.Length)]
            : "";
        Spawn(message);
    }

    /// <summary>Shows a specific message.</summary>
    public void Spawn(string message)
    {
        if (UIManager.Instance == null) return;

        var data = new FloatingTextData(message, color, anchoredPosition);

        // The manager shows the banner and hides it automatically after `duration` seconds —
        // no timer/coroutine needed here.
        UIManager.Instance.ShowUIForDuration<FloatingTextData>(UIType.UIFloatingText, data, duration);
    }
}
