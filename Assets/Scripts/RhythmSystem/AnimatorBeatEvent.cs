using UnityEngine;

[RequireComponent(typeof(Animator))]
public class AnimatorBeatEvent : MonoBehaviour
{
    [Header("Settings")]
    [Tooltip("Waktu ancang-ancang dari event terpicu sampai tombol harus ditekan (dalam detik)")]
    public float timeToHit = 1f;

    [Header("Dependencies")]
    [SerializeField] private RhythmCore rhythmCore;

    private void Awake()
    {
        if (rhythmCore == null) rhythmCore = FindObjectOfType<RhythmCore>();
    }

    public void SpawnTapBeat()
    {
        if (rhythmCore != null)
        {
            rhythmCore.RegisterBeat(BeatType.Tap, timeToHit);
        }
        else
        {
            Debug.LogWarning("RhythmCore belum ada di scene!");
        }
    }
    
    public void SpawnHoldBeat(float holdDuration)
    {
        if (rhythmCore != null)
        {
            rhythmCore.RegisterBeat(BeatType.Hold, timeToHit, holdDuration);
        }
    }
}
