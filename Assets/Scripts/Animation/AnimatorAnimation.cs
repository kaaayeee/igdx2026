using UnityEngine;
using DG.Tweening;

/// <summary>
/// Jembatan ke Animator Unity. Menggantikan UseAnimator (file lama namanya tidak cocok
/// dengan nama class -> script mati, tidak pernah bisa di-attach).
///
/// ESCAPE HATCH TINGKAT LANJUT, bukan jalur utama.
/// Dipakai kalau animasi terlalu kompleks untuk ditulis sebagai tween
/// (mis. animasi frame-by-frame, maskot melambai, state machine bertingkat).
///
/// ---------------------------------------------------------------------------
/// BATASAN YANG HARUS KAMU TERIMA (hidden contract -- tidak bisa diotomatiskan):
///
///   Komponen ini TIDAK TAHU berapa lama clip Animator-mu berjalan.
///   Kamu WAJIB menyetel 'duration' di Inspector >= panjang clip Show/Hide-mu.
///
///   Kalau duration < panjang clip:
///     AnimationController akan mematikan GameObject di TENGAH animasi.
///   Kalau duration > panjang clip:
///     Ada jeda kosong sebelum objek dimatikan (tidak fatal, cuma terasa lambat).
///
/// Semua animasi lain (Move/Scale/Fade) tidak punya masalah ini karena tween-nya
/// bisa ditunggu langsung. Animator punya sistem waktu sendiri.
/// ---------------------------------------------------------------------------
/// </summary>
[RequireComponent(typeof(Animator))]
public class AnimatorAnimation : AnimationBase
{
    [Header("Trigger")]
    [SerializeField] private string showTrigger = "Show";
    [SerializeField] private string hideTrigger = "Hide";

    private Animator animator;
    private int showHash;
    private int hideHash;

    // ---------------------------------------------------------------------
    protected override void CaptureBaseline()
    {
        animator = GetComponent<Animator>();

        // StringToHash: hindari typo string dibandingkan tiap kali dipanggil.
        showHash = Animator.StringToHash(showTrigger);
        hideHash = Animator.StringToHash(hideTrigger);

        // Ini yang HILANG TOTAL di versi lama: field runWhilePaused ada tapi
        // tidak pernah dipakai. Akibatnya saat pause (timeScale 0), Animator BEKU
        // sementara semua anim tween lain jalan normal.
        if (animator != null && runWhilePaused)
            animator.updateMode = AnimatorUpdateMode.UnscaledTime;
    }

    // ---------------------------------------------------------------------
    // Mengembalikan null -- SAH.
    // AnimationController akan menyisipkan placeholder delay selama TotalDuration
    // agar Sequence tetap menunggu, dan OnComplete tetap terpanggil.
    // ---------------------------------------------------------------------
    protected override Tween PlayShow()
    {
        if (animator == null) return null;

        // Batalkan trigger lawan yang mungkin masih antre.
        // Animator tidak punya .Kill() seperti tween, jadi ini penggantinya.
        animator.ResetTrigger(hideHash);
        animator.SetTrigger(showHash);

        return null;
    }

    protected override Tween PlayHide()
    {
        if (animator == null) return null;

        animator.ResetTrigger(showHash);
        animator.SetTrigger(hideHash);

        return null;
    }

#if UNITY_EDITOR
    private void OnValidate()
    {
        if (string.IsNullOrEmpty(showTrigger) || string.IsNullOrEmpty(hideTrigger))
            Debug.LogWarning($"[AnimatorAnimation] '{name}': nama trigger kosong.", this);
    }
#endif
}