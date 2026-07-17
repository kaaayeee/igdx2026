using UnityEngine;

public class RhythmManager : MonoBehaviour
{
    public static RhythmManager Instance;

    [Header("Song Settings")]
    public float bpm = 120f;
    public float songDelay = 2f;
    public AudioSource audioSource;
    
    [Header("Note Settings")]
    public float noteSpeed = 5f;
    
    [HideInInspector] public float songPosition;
    [HideInInspector] public float songPositionInBeats;
    
    private float secPerBeat;
    private float dspSongTime;
    private bool hasStarted = false;

    private void Awake()
    {
        if (Instance == null) Instance = this;
        else Destroy(gameObject);
    }

    private void Start()
    {
        secPerBeat = 60f / bpm;
        Invoke(nameof(StartSong), songDelay);
    }

    private void Update()
    {
        if (hasStarted)
        {
            songPosition = (float)(AudioSettings.dspTime - dspSongTime);
            songPositionInBeats = songPosition / secPerBeat;
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
