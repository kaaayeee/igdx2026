using UnityEngine;
using UnityEngine.UI;
using Ohm.UISystem.Transitions;

[RequireComponent(typeof(Image))]
public class MusicItem : MonoBehaviour
{
    [SerializeField] private MusicTrackDataSO track;
    [SerializeField] private ScaleTransition scaleTransition;

    private Image image;
    private bool isSelected;
    private bool hasAppliedOnce;

    public MusicTrackDataSO Track => track;
    public bool IsSelected => isSelected;

    private void Awake()
    {
        CacheImage();
        ApplyVisual(instant: true);   // state awal jangan dianimasi
    }

    public void Bind(MusicTrackDataSO data)
    {
        track = data;
        hasAppliedOnce = false;
        ApplyVisual(instant: true);
    }

    public void SetSelected(bool value, bool instant = false)
    {
        if (hasAppliedOnce && isSelected == value) return;

        isSelected = value;
        hasAppliedOnce = true;
        ApplyVisual(instant);
    }

    private void ApplyVisual(bool instant = false)
    {
        CacheImage();
        if (track == null) return;

        image.sprite = isSelected ? track.SelectedSprite : track.UnselectedSprite;

        if (scaleTransition == null) return;

        if (isSelected) scaleTransition.TriggerShow(instant);
        else            scaleTransition.TriggerHide(instant);
    }

    private void CacheImage()
    {
        if (image == null) image = GetComponent<Image>();
    }
}