using System;
using System.Collections.Generic;
using DG.Tweening;
using UnityEngine;

/// <summary>
/// Orkestrator animasi Show/Hide untuk satu layar/panel.
///
/// Tanggung jawab (dan HANYA ini):
///   1. Merangkai semua AnimationBase jadi satu Sequence.
///   2. Menyalakan GameObject saat Show.
///   3. Mematikan GameObject saat Hide selesai -- SATU-SATUNYA pemilik keputusan ini.
///   4. Memberi tahu pemanggil kapan animasi benar-benar selesai (onComplete).
///
/// Tidak pakai coroutine. Coroutine ikut mati kalau GameObject dinonaktifkan,
/// dan WaitForSeconds beku saat pause -- dua sumber bug "UI nyangkut aktif tapi
/// tak terlihat, dan memblokir klik". Sequence DOTween hidup di luar GameObject
/// dan bisa unscaled, jadi OnComplete selalu terpanggil.
/// </summary>
public class AnimationController : MonoBehaviour, IDisableHandler
{
    [Header("Animation Control")]
    [SerializeField] private bool playOnEnable = false;

    [Tooltip("Matikan GameObject setelah animasi hide selesai.")]
    [SerializeField] private bool disableAfterHide = true;

    [SerializeField] private List<AnimationBase> animations = new();

    private Sequence sequence;

    /// <summary>Sedang menganimasi (show atau hide).</summary>
    public bool IsPlaying => sequence != null && sequence.IsActive() && sequence.IsPlaying();

    /// <summary>
    /// Durasi terpanjang di antara semua animasi (sudah termasuk delay).
    /// Dipakai kalau ada sistem lain yang butuh angka (mis. TransitionManager).
    /// TAPI: lebih baik pakai callback onComplete daripada menebak lewat angka ini.
    /// </summary>
    public float Duration
    {
        get
        {
            float longest = 0f;
            foreach (var anim in animations)
            {
                if (anim == null) continue;                 // slot kosong di Inspector itu normal
                if (anim.TotalDuration > longest) longest = anim.TotalDuration;
            }
            return longest;
        }
    }

    private void OnEnable()
    {
        if (playOnEnable) Show();
    }

    // ---------------------------------------------------------------------
    // Show / Hide
    // ---------------------------------------------------------------------
    public void Show(Action onComplete = null)
    {
        KillSequence();

        gameObject.SetActive(true);

        sequence = BuildSequence(show: true);
        sequence.OnComplete(() =>
        {
            sequence = null;
            onComplete?.Invoke();
        });
    }

    public void Hide(Action onComplete = null)
    {
        // Sudah mati duluan -> tidak ada yang perlu dianimasi, TAPI callback tetap
        // harus dipanggil. Kalau tidak, pemanggil (mis. scene loader) menunggu selamanya.
        if (!gameObject.activeInHierarchy)
        {
            KillSequence();
            onComplete?.Invoke();
            return;
        }

        KillSequence();

        sequence = BuildSequence(show: false);
        sequence.OnComplete(() =>
        {
            sequence = null;
            if (disableAfterHide) gameObject.SetActive(false);   // SATU-SATUNYA SetActive(false)
            onComplete?.Invoke();
        });
    }

    /// <summary>Langsung ke kondisi akhir tanpa animasi (mis. skip transisi).</summary>
    public void CompleteInstantly()
    {
        if (sequence != null && sequence.IsActive())
            sequence.Complete(true);   // true = ikut selesaikan callback OnComplete
    }

    public void HandleDisable() => Hide();

    // ---------------------------------------------------------------------
    // Internal
    // ---------------------------------------------------------------------
    private Sequence BuildSequence(bool show)
    {
        // SetUpdate(true) di level Sequence supaya OnComplete tetap jalan saat pause,
        // walau ada anggota anim yang runWhilePaused = false.
        var seq = DOTween.Sequence().SetUpdate(true);

        bool hasAny = false;

        foreach (var anim in animations)
        {
            if (anim == null) continue;

            Tween t = show ? anim.BuildShow() : anim.BuildHide();

            if (t != null)
            {
                seq.Join(t);            // semua mulai barengan; stagger diatur lewat 'delay' per-anim
                hasAny = true;
            }
            else
            {
                // Anim tanpa tween (mis. AnimatorAnimation): tidak bisa ditunggu tween-nya.
                // Sisipkan placeholder selama TotalDuration agar Sequence tetap menunggu.
                seq.Insert(0f, DOVirtual.DelayedCall(anim.TotalDuration, () => { }, true));
                hasAny = true;
            }
        }

        // List kosong / semua null -> Sequence 0 detik. OnComplete tetap dipanggil
        // di frame berikutnya, jadi disableAfterHide tetap bekerja.
        if (!hasAny) seq.AppendInterval(0f);

        return seq;
    }

    private void KillSequence()
    {
        if (sequence != null && sequence.IsActive())
            sequence.Kill();           // false = JANGAN jalankan OnComplete tween yang dibatalkan
        sequence = null;
    }

    private void OnDestroy() => KillSequence();

    // ---------------------------------------------------------------------
    // Editor
    // ---------------------------------------------------------------------
    [ContextMenu("Assign Animation")]
    public void AssignAnimation()
    {
        animations.Clear();

        // InChildren + includeInactive: anim sering ditaruh di child (title, panel, tombol),
        // dan child bisa saja nonaktif saat setup.
        animations.AddRange(GetComponentsInChildren<AnimationBase>(true));

#if UNITY_EDITOR
        UnityEditor.EditorUtility.SetDirty(this);
#endif
    }
}