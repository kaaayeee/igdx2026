using UnityEngine;
using System.Collections.Generic;
using UnityEngine.InputSystem;

public class RhythmInput : MonoBehaviour
{
    [Header("Active Notes in Scene")]
    public List<NoteObject> activeNotes = new List<NoteObject>(); 

    private NoteObject heldNote = null;

    private void Update()
    {
        if (Keyboard.current == null) return;

        if (Keyboard.current.spaceKey.wasPressedThisFrame)
        {
            HandleTap();
        }
        else if (Keyboard.current.spaceKey.wasReleasedThisFrame)
        {
            HandleRelease();
        }
    }

    private void HandleTap()
    {
        if (activeNotes.Count == 0 || RhythmManager.Instance == null) return;

        NoteObject closestNote = null;
        float minTimeDiff = float.MaxValue;
        float currentTime = RhythmManager.Instance.songPosition;

        for (int i = activeNotes.Count - 1; i >= 0; i--)
        {
            NoteObject note = activeNotes[i];
            
            if (note == null || note.hasBeenHit)
            {
                activeNotes.RemoveAt(i);
                continue;
            }
            
            float diff = Mathf.Abs(note.targetHitTime - currentTime);
            if (diff < minTimeDiff)
            {
                minTimeDiff = diff;
                closestNote = note;
            }
        }

        if (closestNote != null && minTimeDiff <= 0.3f)
        {
            closestNote.CheckHit(currentTime);
            
            if (closestNote.noteType == NoteType.Hold)
            {
                heldNote = closestNote;
            }
            else
            {
                activeNotes.Remove(closestNote);
            }
        }
    }

    private void HandleRelease()
    {
        if (heldNote != null && RhythmManager.Instance != null)
        {
            heldNote.CheckRelease(RhythmManager.Instance.songPosition);
            activeNotes.Remove(heldNote);
            heldNote = null;
        }
    }
}
