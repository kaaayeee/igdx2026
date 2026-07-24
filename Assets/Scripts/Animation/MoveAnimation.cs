using UnityEngine;
using DG.Tweening;

/// <summary>
/// Animasi perpindahan posisi. Menggantikan MoveFromAnimation, UIMoveToAnimation, UISlideAnimation.
///
/// showFrom = titik asal saat muncul. hideTo = titik tujuan saat hilang.
/// hideTo kosong -> pakai showFrom (simetris: masuk kiri, keluar kiri).
/// Titik bisa berupa offset (angka) atau Transform di scene (bisa digeser visual).
/// </summary>
public class MoveAnimation : AnimationBase
{
    public enum PointMode { Offset, TargetTransform }

    [Header("Titik Masuk (Show)")]
    [SerializeField] private PointMode showMode = PointMode.Offset;

    [Tooltip("Offset relatif posisi asli. Masuk dari kiri: (-1920, 0). Dari atas: (0, 1080).")]
    [SerializeField] private Vector2 showFromOffset = new Vector2(-1920f, 0f);

    [Tooltip("Penanda titik asal. Harus SATU PARENT dengan objek ini.")]
    [SerializeField] private Transform showFromTarget;

    [Header("Titik Keluar (Hide)")]
    [Tooltip("Centang = keluar lewat titik yang sama dengan masuk (kasus paling umum).")]
    [SerializeField] private bool useShowPointForHide = true;

    [SerializeField] private PointMode hideMode = PointMode.Offset;
    [SerializeField] private Vector2 hideToOffset = new Vector2(-1920f, 0f);
    [SerializeField] private Transform hideToTarget;

    [Header("Easing")]
    [SerializeField] private Ease showEase = Ease.OutCubic;
    [SerializeField] private Ease hideEase = Ease.InCubic;

    [Header("Debug (read-only)")]
    [SerializeField, ReadOnly] private Vector3 basePosition;

    private RectTransform rect;   // null kalau bukan UI

    // ---------------------------------------------------------------------
    // Base memanggil ini sekali, sebelum animasi pertama.
    // ---------------------------------------------------------------------
    protected override void CaptureBaseline()
    {
        rect = transform as RectTransform;
        basePosition = ReadPosition();
    }

    /// <summary>Panggil kalau posisi "tampil" berubah saat runtime (mis. layout rebuild).</summary>
    public void RecaptureBasePosition()
    {
        EnsureInit();
        basePosition = ReadPosition();
    }

    // ---------------------------------------------------------------------
    // Posisi: RectTransform pakai anchoredPosition (hormati anchor Unity),
    // Transform biasa pakai localPosition.
    // ---------------------------------------------------------------------
    private Vector3 ReadPosition()
        => rect != null ? (Vector3)rect.anchoredPosition : transform.localPosition;

    private void WritePosition(Vector3 pos)
    {
        if (rect != null) rect.anchoredPosition = pos;
        else transform.localPosition = pos;
    }

    private Tween TweenTo(Vector3 pos, Ease ease)
        => rect != null
            ? rect.DOAnchorPos(pos, duration).SetEase(ease)
            : transform.DOLocalMove(pos, duration).SetEase(ease);
    // Catatan: SetUpdate & SetDelay TIDAK dipasang di sini -- AnimationBase yang urus.

    // ---------------------------------------------------------------------
    // Resolusi titik
    // ---------------------------------------------------------------------
    private Vector3 ResolvePoint(PointMode mode, Transform target, Vector2 offset)
    {
        if (mode == PointMode.TargetTransform && target != null)
        {
            if (target.parent != transform.parent)
            {
                Debug.LogWarning(
                    $"[MoveAnimation] '{name}': target '{target.name}' beda parent. " +
                    "Posisi bisa meleset. Taruh target sebagai sibling.", this);
            }

            if (rect != null && target is RectTransform targetRect)
                return targetRect.anchoredPosition;

            return target.localPosition;
        }

        // Fallback offset: selalu aman, tidak pernah null.
        return basePosition + (Vector3)offset;
    }

    private Vector3 ShowPoint => ResolvePoint(showMode, showFromTarget, showFromOffset);

    private Vector3 HidePoint => useShowPointForHide
        ? ShowPoint
        : ResolvePoint(hideMode, hideToTarget, hideToOffset);

    // ---------------------------------------------------------------------
    // Kontrak AnimationBase: bangun tween, kembalikan. Titik.
    // TIDAK ADA: Kill, SetUpdate, SetDelay, SetActive -- semua tugas base/controller.
    // ---------------------------------------------------------------------
    protected override Tween PlayShow()
    {
        WritePosition(ShowPoint);                 // teleport ke titik asal
        return TweenTo(basePosition, showEase);
    }

    protected override Tween PlayHide()
    {
        return TweenTo(HidePoint, hideEase);
    }

    // ---------------------------------------------------------------------
    // Gizmo: hijau = jalur masuk, merah = jalur keluar. Tuning tanpa play mode.
    // ---------------------------------------------------------------------
#if UNITY_EDITOR
    private void OnDrawGizmosSelected()
    {
        if (transform.parent == null) return;

        if (!Application.isPlaying)
        {
            rect = transform as RectTransform;
            basePosition = ReadPosition();
        }

        Vector3 home = transform.parent.TransformPoint(basePosition);
        Vector3 show = transform.parent.TransformPoint(ShowPoint);
        Vector3 hide = transform.parent.TransformPoint(HidePoint);

        Gizmos.color = Color.green;
        Gizmos.DrawLine(show, home);
        Gizmos.DrawWireSphere(show, 20f);

        Gizmos.color = Color.red;
        Gizmos.DrawLine(home, hide);
        Gizmos.DrawWireSphere(hide, 20f);
    }
#endif
}