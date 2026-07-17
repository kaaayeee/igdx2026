using UnityEngine;

public enum NoteType { Tap, Hold }

public class NoteObject : MonoBehaviour
{
    [Header("Note Data")]
    public NoteType noteType = NoteType.Tap;
    public float holdDuration = 0f;
    public float targetHitTime;
    public Vector3 hitZonePosition;

    public bool hasBeenHit = false;
    public bool isBeingHeld = false;

    public float targetReleaseTime => targetHitTime + holdDuration;

    private LineRenderer holdLine;

    private void Start()
    {
        if (noteType == NoteType.Hold && holdDuration > 0 && RhythmManager.Instance != null)
        {
            holdLine = gameObject.AddComponent<LineRenderer>();
            holdLine.startWidth = 0.8f;
            holdLine.endWidth = 0.8f;
            holdLine.material = new Material(Shader.Find("Sprites/Default"));
            holdLine.startColor = new Color(0.5f, 0.8f, 1f, 0.6f); 
            holdLine.endColor = new Color(0.5f, 0.8f, 1f, 0.2f);
            holdLine.sortingOrder = -1;
            holdLine.positionCount = 2;
            
            UpdateHoldVisual(holdDuration);
        }
    }

    private void Update()
    {
        if (RhythmManager.Instance == null) return;
        
        float currentTime = RhythmManager.Instance.songPosition;

        if (isBeingHeld)
        {
            transform.position = hitZonePosition;

            if (holdLine != null)
            {
                float timeRemaining = targetReleaseTime - currentTime;
                if (timeRemaining < 0) timeRemaining = 0;
                UpdateHoldVisual(timeRemaining);
            }

            if (currentTime > targetReleaseTime + 0.2f)
            {
                Miss();
            }
            return;
        }

        if (!hasBeenHit)
        {
            float timeUntilHit = targetHitTime - currentTime;
            transform.position = hitZonePosition + new Vector3(0, timeUntilHit * RhythmManager.Instance.noteSpeed, 0);

            if (holdLine != null)
            {
                UpdateHoldVisual(holdDuration);
            }

            if (timeUntilHit < -0.3f) 
            {
                Miss();
            }
        }
    }

    private void UpdateHoldVisual(float durationToDraw)
    {
        if (holdLine != null)
        {
            float length = durationToDraw * RhythmManager.Instance.noteSpeed;
            holdLine.SetPosition(0, transform.position);
            holdLine.SetPosition(1, transform.position + new Vector3(0, length, 0));
        }
    }

    public void CheckHit(float currentSongTime)
    {
        if (hasBeenHit) return;

        float timeDifference = Mathf.Abs(targetHitTime - currentSongTime);
        string judgment = "";

        if (timeDifference <= 0.083f) judgment = "Perfect";
        else if (timeDifference <= 0.15f) judgment = "Good";
        else judgment = "Miss";
        
        if (judgment == "Miss")
        {
            Miss();
            return;
        }

        if (noteType == NoteType.Tap)
        {
            Hit(judgment);
        }
        else if (noteType == NoteType.Hold)
        {
            isBeingHeld = true;
            hasBeenHit = true;
            
            if (RhythmUIManager.Instance != null)
            {
                RhythmUIManager.Instance.HitFeedback(judgment + " (Hold)");
            }
        }
    }

    public void CheckRelease(float currentSongTime)
    {
        if (!isBeingHeld) return;

        float timeDifference = Mathf.Abs(targetReleaseTime - currentSongTime);
        string judgment = "";

        if (timeDifference <= 0.1f) judgment = "Perfect";
        else if (timeDifference <= 0.2f) judgment = "Good";
        else judgment = "Miss";

        if (judgment == "Miss") Miss();
        else Hit(judgment + " (Release)");
    }

    private void Miss()
    {
        hasBeenHit = true;
        
        if (RhythmUIManager.Instance != null)
        {
            RhythmUIManager.Instance.HitFeedback("Miss");
        }
        
        Destroy(gameObject);
    }

    private void Hit(string judgment)
    {
        hasBeenHit = true;
        
        if (RhythmUIManager.Instance != null)
        {
            RhythmUIManager.Instance.HitFeedback(judgment);
        }
        
        Destroy(gameObject);
    }
}
