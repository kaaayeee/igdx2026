using UnityEngine;
using DG.Tweening;
using System.Collections;

public class UICircleTransition : UIBase
{
    [Header("References")]
    [SerializeField] private RectTransform Icon;
    [SerializeField] private RectTransform blackPanel; 
    [SerializeField] private GameObject PanelBackground;
    [SerializeField] private float duration = 2f;
    [SerializeField] private float delayBeforeSceneChange = 0.5f;

    public override float AnimationDuration => duration + delayBeforeSceneChange;
    
    [Header("Scale Settings")]
    [SerializeField] private float openedScale = 10f; // Skala saat layar terbuka (sangat besar)
    [SerializeField] private float closedScale = 0f;  // Skala saat menutup (nol)

    private void Update()
    {
        if (blackPanel != null && Icon != null)
        {
            // 1. Tetap kunci posisi di tengah layar
            blackPanel.position = new Vector3(Screen.width / 2f, Screen.height / 2f, 0);
            
            // 2. COUNTER-SCALE LOGIC
            // Kita harus menghitung 'Global Scale' dari parent si blackPanel (yaitu Icon)
            // Jika Icon adalah child dari script ini, maka skala totalnya adalah (transform.scale * Icon.scale)
            float totalScaleX = transform.localScale.x * Icon.localScale.x;
            float totalScaleY = transform.localScale.y * Icon.localScale.y;

            // Terapkan invers skala agar visual blackPanel tetap 1:1 di layar
            blackPanel.localScale = new Vector3(
                totalScaleX != 0 ? 1f / totalScaleX : 100f, 
                totalScaleY != 0 ? 1f / totalScaleY : 100f, 
                1f
            );
        }
    }

    public override void Show()
    {
        isActive = true;
        gameObject.SetActive(true);

        // Mulai dari terbuka (skala besar)
        Icon.localScale = Vector3.one * openedScale;

        // Animasi mengecil untuk menutup layar
        StartCoroutine(SetPanel(true, duration));
        Icon.DOScale(Vector3.one * closedScale, duration)
            .SetEase(Ease.InOutQuart)
            .SetUpdate(true);
    }

    public override void Hide() 
    {
        StartCoroutine(SetPanel(false, duration ));
        Icon.DOScale(Vector3.one * openedScale, duration)
            .SetEase(Ease.InOutQuart)
            .SetUpdate(true);
    }

    public IEnumerator SetPanel(bool isActive, float delay)
    {
        yield return new WaitForSeconds(delay);
        Debug.Log("Set Panel Background Active: " + isActive);
        PanelBackground.SetActive(isActive);
    }
    /// <summary>
    /// Pindahkan posisi lubang ke koordinat klik/UI sebelum transisi dimulai
    /// </summary>
    public void SetTransitionPosition(Vector2 screenPos)
    {
        Icon.position = screenPos;
    }
}