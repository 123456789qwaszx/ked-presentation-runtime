using System;
using System.Collections;
using UnityEngine;
using UnityEngine.EventSystems;

public class UI_EventHandler : MonoBehaviour, 
    IPointerClickHandler, 
    IPointerDownHandler, 
    IPointerUpHandler, 
    IDragHandler, 
    IBeginDragHandler, 
    IEndDragHandler
{
    
    public Action<PointerEventData> OnClickHandler;
    public Action<PointerEventData> OnPointerDownHandler;
    public Action<PointerEventData> OnPointerUpHandler;
    public Action<PointerEventData> OnDragHandler;
    public Action<PointerEventData> OnBeginDragHandler;
    public Action<PointerEventData> OnEndDragHandler;
    public Action<PointerEventData> OnLongPressHandler;

    // Settings
    [SerializeField] private float _longPressDuration = 1.0f;

    // State
    private bool _isDragging;
    private bool _isLongPressTriggered;
    private bool _isClickAllowed = true;
    private PointerEventData _cachedEventData;
    
    // Coroutine tracking
    private Coroutine _longPressCoroutine;

    // Pointer Events
    public void OnPointerClick(PointerEventData eventData)
    {
        if (_isClickAllowed)
        {
            OnClickHandler?.Invoke(eventData);
        }
    }

    public void OnPointerDown(PointerEventData eventData)
    {
        _isLongPressTriggered = false;
        _isClickAllowed = true;
        _cachedEventData = eventData;
        
        OnPointerDownHandler?.Invoke(eventData);
        
        // 롱프레스 체크 시작 (필요할 때만)
        if (OnLongPressHandler != null)
        {
            StopLongPressCheck(); // 기존 코루틴 정리
            _longPressCoroutine = StartCoroutine(CheckLongPress());
        }
    }

    public void OnPointerUp(PointerEventData eventData)
    {
        StopLongPressCheck(); // 롱프레스 체크 중단
        
        _cachedEventData = null;
        OnPointerUpHandler?.Invoke(eventData);
    }

    public void OnBeginDrag(PointerEventData eventData)
    {
        _isDragging = true;
        _cachedEventData = eventData;
        
        StopLongPressCheck(); // 드래그 시작하면 롱프레스 취소
        
        OnBeginDragHandler?.Invoke(eventData);
    }

    public void OnDrag(PointerEventData eventData)
    {
        _cachedEventData = eventData;
        
        // 드래그 핸들러가 등록된 경우에만 호출
        if (_isDragging)
        {
            OnDragHandler?.Invoke(eventData);
        }
    }

    public void OnEndDrag(PointerEventData eventData)
    {
        _isDragging = false;
        _isLongPressTriggered = false;
        
        OnEndDragHandler?.Invoke(eventData);
    }

    // ────────────────────────────────────────────────────────────
    // Long Press Logic
    // ────────────────────────────────────────────────────────────

    private IEnumerator CheckLongPress()
    {
        yield return new WaitForSecondsRealtime(_longPressDuration);

        if (_cachedEventData != null && !_isLongPressTriggered)
        {
            _isLongPressTriggered = true;
            _isClickAllowed = false;
            OnLongPressHandler?.Invoke(_cachedEventData);
        }

        _longPressCoroutine = null;
    }

    private void StopLongPressCheck()
    {
        if (_longPressCoroutine != null)
        {
            StopCoroutine(_longPressCoroutine);
            _longPressCoroutine = null;
        }
    }

    // ────────────────────────────────────────────────────────────
    // Cleanup
    // ────────────────────────────────────────────────────────────

    private void OnDisable()
    {
        StopLongPressCheck();
        _isDragging = false;
        _cachedEventData = null;
    }
}