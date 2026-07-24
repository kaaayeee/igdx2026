using UnityEngine;
using DG.Tweening;

/// <summary>
/// Dasar semua animasi Show/Hide.
///
/// KONTRAK (penting):
///   - PlayShow/PlayHide MENGEMBALIKAN Tween, tidak menjalankan sendiri sampai tuntas.
///     AnimationController yang merangkai semua tween jadi satu Sequence, sehingga
///     dia tahu PERSIS kapan animasi selesai -- bukan menebak lewat WaitForSeconds.
///   - Turunan TIDAK BOLEH memanggil gameObject.SetActive(false).
///     Mematikan GameObject adalah hak eksklusif AnimationController.
///   - Turunan TIDAK PERLU memanggil .SetUpdate() / .SetDelay() -- base yang urus.
///   - Turunan TIDAK PERLU meng-Kill tween lama -- base yang urus.
///   - Turunan capture nilai asli di CaptureBaseline(), BUKAN di Awake().
///     Awake tidak jalan kalau objek di-instantiate dalam keadaan nonaktif.
/// </summary>
public abstract class AnimationBase : MonoBehaviour
{
    [Header("Base Animation")]
    [Tooltip("Jalan terus saat game di-pause (Time.timeScale = 0). Untuk UI, hampir selalu true.")]
    [SerializeField] protected bool runWhilePaused = true;

    [Tooltip("Lama animasi (detik).")]
    [SerializeField] protected float duration = 0.5f;

    [Tooltip("Tunda sebelum animasi mulai. Dipakai untuk efek stagger (mis. bintang muncul satu-satu).")]
    [SerializeField, Min(0f)] protected float delay = 0f;

    public float Duration => duration;
    public float Delay => delay;

    /// <summary>Total waktu dari perintah sampai animasi ini benar-benar selesai.</summary>
    public float TotalDuration => delay + duration;

    private Tween current;
    private bool initialized;

    // ---------------------------------------------------------------------
    // Init lazy. JANGAN pindahkan ke Awake.
    // ---------------------------------------------------------------------
    protected void EnsureInit()
    {
        if (initialized) return;
        initialized = true;
        CaptureBaseline();
    }

    /// <summary>
    /// Simpan nilai "tampil" (posisi/scale/alpha asli prefab) dan cache komponen.
    /// Dipanggil sekali, saat animasi pertama kali dipakai.
    /// </summary>
    protected virtual void CaptureBaseline() { }

    // ---------------------------------------------------------------------
    // Dipanggil AnimationController
    // ---------------------------------------------------------------------
    public Tween BuildShow() => Build(true);
    public Tween BuildHide() => Build(false);

    private Tween Build(bool show)
    {
        EnsureInit();
        current?.Kill();

        current = show ? PlayShow() : PlayHide();

        // Null itu SAH: AnimatorAnimation tidak menghasilkan tween sama sekali.
        // Controller akan menanganinya lewat placeholder delay.
        current?.SetUpdate(runWhilePaused).SetDelay(delay);

        return current;
    }

    /// <summary>Bangun tween untuk memunculkan. JANGAN SetActive/SetUpdate/Kill di sini.</summary>
    protected abstract Tween PlayShow();

    /// <summary>Bangun tween untuk menyembunyikan. JANGAN SetActive/SetUpdate/Kill di sini.</summary>
    protected abstract Tween PlayHide();

    // ---------------------------------------------------------------------
    // WAJIB: tanpa ini, tween memegang referensi transform yang sudah mati
    // saat scene reload -> "DOTween Tween startup failed (NULL target)".
    // ---------------------------------------------------------------------
    protected virtual void OnDestroy() => current?.Kill();
}