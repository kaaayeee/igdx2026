using UnityEngine;

[RequireComponent(typeof(Animator))]
public class AnimatorBeatEvent : MonoBehaviour
{
    [Header("Settings")]
    [Tooltip("Waktu ancang-ancang dari event terpicu sampai tombol harus ditekan (dalam detik)")]
    public float timeToHit = 1f;

    /// <summary>
    /// Panggil dari Animation Event untuk Tap biasa.
    /// </summary>
    public void SpawnTapBeat()
    {
        if (RhythmCore.Instance != null)
        {
            RhythmCore.Instance.RegisterBeat(BeatType.Tap, timeToHit);
        }
        else
        {
            Debug.LogWarning("RhythmCore belum ada di scene!");
        }
    }

    /// <summary>
    /// Panggil dari Animation Event untuk Hold.
    /// Isi parameter float pada event dengan durasi hold.
    /// </summary>
    public void SpawnHoldBeat(float holdDuration)
    {
        if (RhythmCore.Instance != null)
        {
            RhythmCore.Instance.RegisterBeat(BeatType.Hold, timeToHit, holdDuration);
        }
    }
}
