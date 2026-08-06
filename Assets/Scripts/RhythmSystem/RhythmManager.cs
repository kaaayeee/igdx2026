using UnityEngine;

public class RhythmManager : MonoBehaviour
{
    [Header("Audio Setup")]
    public AudioSource audioSource;
    public float songDelay = 2f;

    public float CurrentSongTime { get; private set; }
    private float dspSongTime;
    private bool hasStarted = false;

    private void Start()
    {
        Invoke(nameof(StartSong), songDelay);
    }

    private void Update()
    {
        if (hasStarted)
        {
            CurrentSongTime = (float)(AudioSettings.dspTime - dspSongTime);
        }
    }

    private void StartSong()
    {
        dspSongTime = (float)AudioSettings.dspTime;
        if (audioSource != null && audioSource.clip != null)
        {
            audioSource.Play();
        }
        hasStarted = true;
    }
}
