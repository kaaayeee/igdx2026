using System.Collections.Generic;
using DG.Tweening;
using UnityEngine;
using UnityEngine.UI;

public class MusicCarousel : MonoBehaviour
{
    [Header("Refs")]
    [SerializeField] private ScrollRect scrollRect;
    [SerializeField] private RectTransform viewport;
    [SerializeField] private RectTransform content;
    [SerializeField] private Button upButton;
    [SerializeField] private Button downButton;

    [Header("Movement")]
    [SerializeField] private float moveDuration = 0.25f;
    [SerializeField] private Ease moveEase = Ease.OutCubic;

    [Header("Start")]
    [SerializeField] private bool startAtMiddle = true;
    [SerializeField] private int startIndex = 0;

    private readonly List<MusicItem> items = new();
    private int currentIndex = -1;
    private Tween moveTween;

    public MusicItem CurrentItem => currentIndex >= 0 ? items[currentIndex] : null;
    public MusicTrackDataSO CurrentTrack => CurrentItem != null ? CurrentItem.Track : null;

    private void Awake()
    {
        if (scrollRect != null)
        {
            if (viewport == null) viewport = scrollRect.viewport;
            if (content == null) content = scrollRect.content;
        }

        content.GetComponentsInChildren(true, items);
        LockScrollRect();

        if (upButton != null) upButton.onClick.AddListener(SelectPrevious);
        if (downButton != null) downButton.onClick.AddListener(SelectNext);
    }

    private void Start()
    {
        // layout group baru selesai menghitung posisi setelah rebuild,
        // tanpa ini perhitungan snap pertama meleset
        LayoutRebuilder.ForceRebuildLayoutImmediate(content);

        int index = startAtMiddle ? items.Count / 2 : startIndex;
        SelectIndex(index, instant: true);
    }

    private void OnDestroy()
    {
        moveTween?.Kill();
        if (upButton != null) upButton.onClick.RemoveListener(SelectPrevious);
        if (downButton != null) downButton.onClick.RemoveListener(SelectNext);
    }

    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.UpArrow)) SelectPrevious();
        else if (Input.GetKeyDown(KeyCode.DownArrow)) SelectNext();
    }

    public void SelectPrevious() => SelectIndex(currentIndex - 1);
    public void SelectNext()     => SelectIndex(currentIndex + 1);

    public void SelectIndex(int index, bool instant = false)
    {
        if (items.Count == 0) return;

        index = Mathf.Clamp(index, 0, items.Count - 1);
        if (index == currentIndex) return;

        currentIndex = index;

        for (int i = 0; i < items.Count; i++)
            items[i].SetSelected(i == index, instant);

        MoveTo((RectTransform)items[index].transform, instant);
        RefreshButtons();
    }

    private void MoveTo(RectTransform item, bool instant)
    {
        moveTween?.Kill();

        Vector2 target = GetSnapPosition(item);

        if (instant) content.anchoredPosition = target;
        else moveTween = content.DOAnchorPos(target, moveDuration).SetEase(moveEase);
    }

    private Vector2 GetSnapPosition(RectTransform item)
    {
        Vector2 viewportCenter = content.InverseTransformPoint(viewport.TransformPoint(viewport.rect.center));
        Vector2 itemCenter     = content.InverseTransformPoint(item.TransformPoint(item.rect.center));

        float deltaY = viewportCenter.y - itemCenter.y;
        return new Vector2(content.anchoredPosition.x, content.anchoredPosition.y + deltaY);
    }

    private void RefreshButtons()
    {
        if (upButton != null)   upButton.interactable   = currentIndex > 0;
        if (downButton != null) downButton.interactable = currentIndex < items.Count - 1;
    }

    private void LockScrollRect()
    {
        if (scrollRect == null) return;

        scrollRect.horizontal = false;
        scrollRect.vertical   = false;   // ini yang mematikan drag
        scrollRect.inertia    = false;
        scrollRect.movementType = ScrollRect.MovementType.Unrestricted;
    }
}