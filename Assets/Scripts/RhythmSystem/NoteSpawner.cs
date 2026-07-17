using UnityEngine;
using System.Collections.Generic;

[System.Serializable]
public class NoteData
{
    public float targetTime;
    public NoteType type;
    public float holdDuration;
}

public enum DifficultyMode { Easy, Medium, Hard, Brutal }

public class NoteSpawner : MonoBehaviour
{
    [Header("References")]
    public GameObject notePrefab;
    public GameObject holdNotePrefab;
    public RhythmInput rhythmInput;
    
    [Header("Beatmap Data")]
    public List<NoteData> beatMapData = new List<NoteData>(); 
    
    [Header("Difficulty Settings")]
    public DifficultyMode difficulty = DifficultyMode.Medium;

    [Header("Auto Generate (Audio Analysis)")]
    public bool useAudioAnalysis = true;
    public bool autoCalibrateThresholds = true;
    
    [Range(0.01f, 1f)]
    public float beatThreshold = 0.1f;
    public float minTimeBetweenNotes = 0.25f;
    
    [Range(0.01f, 1f)]
    public float holdSustainThreshold = 0.05f;
    public float minHoldDuration = 0.3f;
    
    [Header("Spawn Settings")]
    public Transform spawnParent;
    public float spawnAdvanceTime = 2f; 
    
    private int currentNoteIndex = 0;

    private void Start()
    {
        if (useAudioAnalysis && RhythmManager.Instance != null && RhythmManager.Instance.audioSource.clip != null)
        {
            AnalyzeAudioForBeats();
        }
    }

    private void Update()
    {
        if (RhythmManager.Instance == null || currentNoteIndex >= beatMapData.Count) return;

        float currentTime = RhythmManager.Instance.songPosition;
        NoteData nextNote = beatMapData[currentNoteIndex];

        if (currentTime >= nextNote.targetTime - spawnAdvanceTime)
        {
            SpawnNote(nextNote);
            currentNoteIndex++;
        }
    }

    private void AnalyzeAudioForBeats()
    {
        AudioClip clip = RhythmManager.Instance.audioSource.clip;
        beatMapData.Clear();

        float[] samples = new float[clip.samples * clip.channels];
        clip.GetData(samples, 0);

        int sampleRate = clip.frequency;
        int channels = clip.channels;
        int windowSize = 1024;
        
        float beatMultiplier = 0.4f;
        float holdMultiplier = 0.15f;

        switch (difficulty)
        {
            case DifficultyMode.Easy:
                minTimeBetweenNotes = 0.5f;
                beatMultiplier = 0.6f;
                holdMultiplier = 0.25f;
                break;
            case DifficultyMode.Medium:
                minTimeBetweenNotes = 0.25f;
                beatMultiplier = 0.4f;
                holdMultiplier = 0.15f;
                break;
            case DifficultyMode.Hard:
                minTimeBetweenNotes = 0.125f;
                beatMultiplier = 0.25f;
                holdMultiplier = 0.1f;
                break;
            case DifficultyMode.Brutal:
                minTimeBetweenNotes = 0.05f;
                beatMultiplier = 0.1f;
                holdMultiplier = 0.05f;
                break;
        }

        if (autoCalibrateThresholds)
        {
            CalibrateThresholds(samples, windowSize, beatMultiplier, holdMultiplier);
        }
        
        DetectBeats(samples, windowSize, sampleRate, channels);
    }

    private void CalibrateThresholds(float[] samples, int windowSize, float beatMultiplier, float holdMultiplier)
    {
        float totalRms = 0;
        float maxRms = 0;
        int totalWindows = 0;
        
        for (int i = 0; i < samples.Length; i += windowSize)
        {
            float sum = 0;
            for (int j = 0; j < windowSize && i + j < samples.Length; j++)
            {
                sum += samples[i + j] * samples[i + j];
            }
            float rms = Mathf.Sqrt(sum / windowSize);
            
            totalRms += rms;
            if (rms > maxRms) maxRms = rms;
            totalWindows++;
        }
        
        float averageRms = totalWindows > 0 ? (totalRms / totalWindows) : 0;
        
        beatThreshold = averageRms + (maxRms - averageRms) * beatMultiplier;
        holdSustainThreshold = averageRms + (maxRms - averageRms) * holdMultiplier;
    }

    private void DetectBeats(float[] samples, int windowSize, int sampleRate, int channels)
    {
        bool isTrackingSound = false;
        float currentSoundStartTime = 0;
        float lastNoteTime = -minTimeBetweenNotes;

        for (int i = 0; i < samples.Length; i += windowSize)
        {
            float sum = 0;
            for (int j = 0; j < windowSize && i + j < samples.Length; j++)
            {
                sum += samples[i + j] * samples[i + j];
            }
            float rms = Mathf.Sqrt(sum / windowSize);

            float currentTime = (float)i / (sampleRate * channels);

            if (!isTrackingSound)
            {
                if (rms > beatThreshold && (currentTime - lastNoteTime >= minTimeBetweenNotes))
                {
                    isTrackingSound = true;
                    currentSoundStartTime = currentTime;
                    lastNoteTime = currentTime;
                }
            }
            else
            {
                if (rms < holdSustainThreshold || i + windowSize >= samples.Length)
                {
                    float duration = currentTime - currentSoundStartTime;
                    
                    NoteData newNote = new NoteData();
                    newNote.targetTime = currentSoundStartTime;

                    if (duration >= minHoldDuration)
                    {
                        newNote.type = NoteType.Hold;
                        newNote.holdDuration = duration;
                    }
                    else
                    {
                        newNote.type = NoteType.Tap;
                        newNote.holdDuration = 0;
                    }

                    beatMapData.Add(newNote);
                    isTrackingSound = false;
                }
            }
        }
    }

    private void SpawnNote(NoteData noteData)
    {
        Transform parentTransform = spawnParent != null ? spawnParent : transform;
        
        GameObject prefabToUse = (noteData.type == NoteType.Hold && holdNotePrefab != null) ? holdNotePrefab : notePrefab;
        GameObject newNoteObj = Instantiate(prefabToUse, parentTransform);
        NoteObject newNote = newNoteObj.GetComponent<NoteObject>();
        
        newNote.targetHitTime = noteData.targetTime;
        newNote.noteType = noteData.type;
        newNote.holdDuration = noteData.holdDuration;

        if (rhythmInput != null)
        {
            rhythmInput.activeNotes.Add(newNote);
        }
    }
}
