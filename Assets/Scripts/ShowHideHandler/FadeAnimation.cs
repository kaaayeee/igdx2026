using UnityEngine;
using DG.Tweening;

/// <summary>
/// Animasi fade in/out. Menggantikan UIFadeAnimation.
///
/// Otomatis mendeteksi target:
///   CanvasGroup    -> UI (sekaligus mengurus blocksRaycasts & interactable)
///   SpriteRenderer -> world object
/// Kalau tidak ada dua-duanya, CanvasGroup ditambahkan otomatis.
/// </summary>
public class FadeAnimation : AnimationBase
{
    [Header("Fade")]
    [Tooltip("Alpha saat tersembunyi. Biasanya 0.")]
    [SerializeField, Range(0f, 1f)] private float hiddenAlpha = 0f;

    [SerializeField] private Ease showEase = Ease.OutQuad;
    [SerializeField] private Ease hideEase = Ease.InQuad;

    [Header("Raycast (khusus CanvasGroup)")]
    [Tooltip("Matikan blocksRaycasts saat Hide agar UI transparan tidak memblokir klik di belakangnya.")]
    [SerializeField] private bool manageRaycasts = true;

    [Header("Debug (read-only)")]
    [SerializeField, ReadOnly] private float baseAlpha = 1f;

    private CanvasGroup canvasGroup;
    private SpriteRenderer spriteRenderer;

    // ---------------------------------------------------------------------
    protected override void CaptureBaseline()
    {
        canvasGroup = GetComponent<CanvasGroup>();
        spriteRenderer = GetComponent<SpriteRenderer>();

        if (canvasGroup == null && spriteRenderer == null)
            canvasGroup = gameObject.AddComponent<CanvasGroup>();

        baseAlpha = ReadAlpha();

        // Jebakan umum: user set alpha prefab ke 0 ("kan mau disembunyikan").
        // Akibatnya baseAlpha = 0 -> Show fade dari 0 ke 0 -> UI TIDAK PERNAH MUNCUL,
        // tanpa error apa pun. Selamatkan + jelaskan.
        if (baseAlpha <= hiddenAlpha + 0.001f)
        {
            Debug.LogWarning(
                $"[FadeAnimation] '{name}': alpha prefab ({baseAlpha:0.00}) sama dengan hiddenAlpha. " +
                "Dipaksa ke 1. Simpan prefab dalam kondisi TAMPIL, bukan tersembunyi.", this);
            baseAlpha = 1f;
        }
    }

    private float ReadAlpha()
    {
        if (canvasGroup != null) return canvasGroup.alpha;
        if (spriteRenderer != null) return spriteRenderer.color.a;
        return 1f;
    }

    private void WriteAlpha(float a)
    {
        if (canvasGroup != null)
        {
            canvasGroup.alpha = a;
        }
        else if (spriteRenderer != null)
        {
            Color c = spriteRenderer.color;
            c.a = a;
            spriteRenderer.color = c;
        }
    }

    private void SetInteractable(bool on)
    {
        if (!manageRaycasts || canvasGroup == null) return;
        canvasGroup.blocksRaycasts = on;
        canvasGroup.interactable = on;
    }

    // ---------------------------------------------------------------------
    // Kontrak AnimationBase: bangun tween, kembalikan.
    // ---------------------------------------------------------------------
    protected override Tween PlayShow()
    {
        WriteAlpha(hiddenAlpha);
        SetInteractable(true);          // langsung bisa diklik, tak perlu tunggu fade selesai

        if (canvasGroup != null)
            return canvasGroup.DOFade(baseAlpha, duration).SetEase(showEase);

        return spriteRenderer.DOFade(baseAlpha, duration).SetEase(showEase);
    }

    protected override Tween PlayHide()
    {
        // PENTING: matikan raycast SEKARANG, bukan di OnComplete.
        // Kalau menunggu selesai, UI yang sedang memudar masih memakan klik selama
        // durasi fade -> user klik tombol di belakangnya dan "tidak ada respons".
        SetInteractable(false);

        if (canvasGroup != null)
            return canvasGroup.DOFade(hiddenAlpha, duration).SetEase(hideEase);

        return spriteRenderer.DOFade(hiddenAlpha, duration).SetEase(hideEase);
    }
}