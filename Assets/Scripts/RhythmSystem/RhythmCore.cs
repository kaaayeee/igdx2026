using UnityEngine;
using System.Collections.Generic;
using UnityEngine.InputSystem;

public enum BeatType { Tap, Hold }

public class BeatData
{
    public BeatType type;
    public float targetHitTime;
    public float holdDuration;
    public float targetReleaseTime => targetHitTime + holdDuration;
    
    public bool isHit = false;
    public bool isBeingHeld = false;
}

public class RhythmCore : MonoBehaviour
{
    [Header("Dependencies")]
    [SerializeField] private RhythmManager rhythmManager;
    [SerializeField] private RhythmUIManager uiManager;

    [Header("Input")]
    [Tooltip("Aksi input untuk Tap. Default: Spasi, Gamepad Bawah, atau Sentuh/Klik")]
    public InputAction tapAction = new InputAction("Tap", type: InputActionType.Button);

    [Header("Judgement Windows")]
    [Tooltip("Waktu dalam detik untuk Perfect")]
    public float perfectWindow = 0.08f;
    [Tooltip("Waktu dalam detik untuk Good")]
    public float goodWindow = 0.15f;
    [Tooltip("Batas waktu sebelum otomatis dianggap Miss")]
    public float missWindow = 0.3f; 

    private List<BeatData> activeBeats = new List<BeatData>();
    private BeatData heldBeat = null;

    private void Awake()
    {
        if (rhythmManager == null) rhythmManager = FindObjectOfType<RhythmManager>();
        if (uiManager == null) uiManager = FindObjectOfType<RhythmUIManager>();

        // Tambahkan binding default jika kosong agar cross-platform
        if (tapAction.bindings.Count == 0)
        {
            tapAction.AddBinding("<Keyboard>/space");
            tapAction.AddBinding("<Gamepad>/buttonSouth"); // Tombol A(Xbox)/Cross(PS)
            tapAction.AddBinding("<Pointer>/press");       // Touchscreen tap atau Mouse klik kiri
        }
    }

    private void OnEnable()
    {
        tapAction?.Enable();
    }

    private void OnDisable()
    {
        tapAction?.Disable();
    }

    private void Update()
    {
        if (rhythmManager == null) return;
        float currentTime = rhythmManager.CurrentSongTime;

        // Auto Miss Check
        for (int i = activeBeats.Count - 1; i >= 0; i--)
        {
            BeatData beat = activeBeats[i];
            if (beat.isHit)
            {
                activeBeats.RemoveAt(i);
                continue;
            }

            // Jika kelewatan
            if (currentTime > beat.targetHitTime + missWindow && !beat.isBeingHeld)
            {
                Miss();
                activeBeats.RemoveAt(i);
            }
        }

        // Hold Release Auto Miss Check
        if (heldBeat != null)
        {
            if (currentTime > heldBeat.targetReleaseTime + missWindow)
            {
                Miss();
                heldBeat = null;
            }
        }

        // Input Handling (Cross-Platform)
        if (tapAction.WasPressedThisFrame()) HandleTap(currentTime);
        else if (tapAction.WasReleasedThisFrame()) HandleRelease(currentTime);
    }

    public void RegisterBeat(BeatType type, float timeToHit, float holdDuration = 0f)
    {
        float targetTime = rhythmManager.CurrentSongTime + timeToHit;
        
        activeBeats.Add(new BeatData
        {
            type = type,
            targetHitTime = targetTime,
            holdDuration = holdDuration,
            isHit = false,
            isBeingHeld = false
        });
    }

    private void HandleTap(float currentTime)
    {
        if (activeBeats.Count == 0) return;

        BeatData closestBeat = null;
        float minTimeDiff = float.MaxValue;

        foreach (var beat in activeBeats)
        {
            if (beat.isHit) continue;

            float diff = Mathf.Abs(beat.targetHitTime - currentTime);
            if (diff < minTimeDiff)
            {
                minTimeDiff = diff;
                closestBeat = beat;
            }
        }

        // Cek jika beat ada di dalam jendela input
        if (closestBeat != null && minTimeDiff <= missWindow)
        {
            EvaluateHit(minTimeDiff, closestBeat.type == BeatType.Hold);
            
            if (closestBeat.type == BeatType.Hold)
            {
                closestBeat.isBeingHeld = true;
                heldBeat = closestBeat;
            }
            else
            {
                closestBeat.isHit = true;
            }
        }
    }

    private void HandleRelease(float currentTime)
    {
        if (heldBeat != null)
        {
            float timeDiff = Mathf.Abs(heldBeat.targetReleaseTime - currentTime);
            EvaluateHit(timeDiff, false, true);
            
            heldBeat.isHit = true;
            heldBeat = null;
        }
    }

    private void EvaluateHit(float timeDifference, bool isHoldStart, bool isHoldRelease = false)
    {
        string judgment = "";
        
        if (timeDifference <= perfectWindow) judgment = "Perfect";
        else if (timeDifference <= goodWindow) judgment = "Good";
        else judgment = "Miss";

        if (judgment == "Miss")
        {
            Miss();
        }
        else
        {
            string suffix = isHoldStart ? " (Hold Start)" : (isHoldRelease ? " (Hold Release)" : "");
            if (uiManager != null) uiManager.ShowFeedback(judgment + suffix);
        }
    }

    private void Miss()
    {
        if (uiManager != null) uiManager.ShowFeedback("Miss");
    }
}
