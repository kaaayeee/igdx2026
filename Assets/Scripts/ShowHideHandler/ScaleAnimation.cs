using UnityEngine;
using DG.Tweening;

public class ScaleAnimation : AnimationBase
{
    [Tooltip("Pengali dari scale asli prefab. 1 = kembali ke ukuran desain.")]
    [SerializeField] private float shownMultiplier = 1f;
    [SerializeField] private Vector3 hiddenScale = Vector3.zero;
    [SerializeField] private Ease showEase = Ease.OutBack;   // pop overshoot
    [SerializeField] private Ease hideEase = Ease.InBack;

    [SerializeField, ReadOnly] private Vector3 baseScale;

    protected override void CaptureBaseline()
    {
        baseScale = transform.localScale;
    }

    protected override Tween PlayShow()
    {
        transform.localScale = hiddenScale;
        return transform.DOScale(baseScale * shownMultiplier, duration).SetEase(showEase);
    }

    protected override Tween PlayHide()
    {
        return transform.DOScale(hiddenScale, duration).SetEase(hideEase);
    }
}
