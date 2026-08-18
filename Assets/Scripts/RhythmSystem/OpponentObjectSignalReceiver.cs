using UnityEngine;

/// <summary>
/// Attach ke GameObject nenek (opponent).
/// - DoAction() → dipanggil oleh Timeline Signal Emitter secara manual
/// - DoMiss()   → subscribe otomatis ke RhythmCore.onMiss saat player miss beat
/// </summary>
[RequireComponent(typeof(Animator))]
public class OpponentObjectSignalReceiver : MonoBehaviour
{
    [Header("Dependencies")]
    [SerializeField] private RhythmCore rhythmCore;

    private Animator _animator;

    private static readonly int HashDoAction = Animator.StringToHash("DoAction");
    private static readonly int HashDoMiss   = Animator.StringToHash("DoMiss");

    private void Awake()
    {
        _animator = GetComponent<Animator>();
        if (rhythmCore == null) rhythmCore = FindObjectOfType<RhythmCore>();
    }

    private void OnEnable()
    {
        if (rhythmCore != null)
            rhythmCore.onMiss.AddListener(DoMiss);
    }

    private void OnDisable()
    {
        if (rhythmCore != null)
            rhythmCore.onMiss.RemoveListener(DoMiss);
    }

    /// <summary>
    /// Dipanggil langsung dari Timeline Signal Emitter.
    /// Trigger animator parameter "DoAction".
    /// </summary>
    public void DoAction()
    {
        _animator.SetTrigger(HashDoAction);
    }

    /// <summary>
    /// Dipanggil otomatis saat player miss beat (via RhythmCore.onMiss).
    /// Trigger animator parameter "DoMiss".
    /// </summary>
    public void DoMiss()
    {
        _animator.SetTrigger(HashDoMiss);
    }
}
