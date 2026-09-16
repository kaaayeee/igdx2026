using UnityEngine;
using UnityEngine.Events;
using UnityEngine.EventSystems; // Ditambahkan untuk mendukung IPointer*
using Ohm.UISystem;

[RequireComponent(typeof(Collider))]
[RequireComponent(typeof(Renderer))]
public class Interactable : MonoBehaviour, 
    IPointerEnterHandler, 
    IPointerExitHandler, 
    IPointerDownHandler, 
    IPointerUpHandler, 
    IPointerClickHandler
{
    [Header("Visuals")]
    [SerializeField] private Material normalMaterial;
    [SerializeField] private Material hoverMaterial;
    [SerializeField] private Material clickMaterial;

    [Header("Unity Events")]
    public UnityEvent onClick;
    public UnityEvent onHoverEnter;
    public UnityEvent onHoverExit;

    private Renderer _renderer;
    private bool _isHovering = false;
    private bool _isClicking = false;

    private void Awake()
    {
        _renderer = GetComponent<Renderer>();
        
        if (normalMaterial == null)
        {
            normalMaterial = _renderer.sharedMaterial;
        }
        else
        {
            _renderer.material = normalMaterial;
        }
    }

    private void UpdateVisuals()
    {
        if (_isClicking && _isHovering)
        {
            if (clickMaterial != null) _renderer.material = clickMaterial;
        }
        else if (_isHovering)
        {
            if (hoverMaterial != null) _renderer.material = hoverMaterial;
        }
        else
        {
            if (normalMaterial != null) _renderer.material = normalMaterial;
        }
    }


    public void OnPointerEnter(PointerEventData eventData)
    {
        _isHovering = true;
        UpdateVisuals();
        onHoverEnter?.Invoke();
    }


    public void OnPointerExit(PointerEventData eventData)
    {
        _isHovering = false;
        UpdateVisuals();
        onHoverExit?.Invoke();
    }


    public void OnPointerDown(PointerEventData eventData)
    {
        _isClicking = true;
        UpdateVisuals();
    }


    public void OnPointerUp(PointerEventData eventData)
    {
        _isClicking = false;
        UpdateVisuals();
    }


    public void OnPointerClick(PointerEventData eventData)
    {
        onClick?.Invoke();
    }
}