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

    [Header("Long Press")]
    [SerializeField] private float _longPressDuration = 1.0f;

    [Header("Drag / Click Feel")]
    [SerializeField] private float _dragClickLockDelay = 0.04f;
    [SerializeField] private float _minDragDistance = 3.8f;
    
    private bool _isDragging;
    private bool _isDragConfirmed;
    private bool _isLongPressTriggered;
    private bool _isClickAllowed = true;

    private Vector2 _pointerDownPosition;
    private PointerEventData _cachedEventData;

    private Coroutine _longPressCoroutine;
    private Coroutine _dragClickLockCoroutine;

    public void OnPointerClick(PointerEventData eventData)
    {
        if (_isClickAllowed)
            OnClickHandler?.Invoke(eventData);
    }

    public void OnPointerDown(PointerEventData eventData)
    {
        StopLongPressCheck();
        StopDragClickLockCheck();

        _isDragging = false;
        _isDragConfirmed = false;
        _isLongPressTriggered = false;
        _isClickAllowed = true;

        _pointerDownPosition = eventData.position;
        _cachedEventData = eventData;

        OnPointerDownHandler?.Invoke(eventData);

        if (OnLongPressHandler != null)
            _longPressCoroutine = StartCoroutine(CheckLongPress());
    }

    public void OnPointerUp(PointerEventData eventData)
    {
        StopLongPressCheck();
        StopDragClickLockCheck();

        _cachedEventData = null;

        OnPointerUpHandler?.Invoke(eventData);
    }

    public void OnBeginDrag(PointerEventData eventData)
    {
        _isDragging = true;
        _cachedEventData = eventData;

        StopLongPressCheck();

        TryConfirmDrag(eventData);
    }

    public void OnDrag(PointerEventData eventData)
    {
        _cachedEventData = eventData;

        if (!_isDragging)
            return;

        if (!_isDragConfirmed)
            TryConfirmDrag(eventData);

        if (_isDragConfirmed)
            OnDragHandler?.Invoke(eventData);
    }

    public void OnEndDrag(PointerEventData eventData)
    {
        if (_isDragConfirmed)
            OnEndDragHandler?.Invoke(eventData);

        StopDragClickLockCheck();

        _isDragging = false;
        _isDragConfirmed = false;
        _isLongPressTriggered = false;
    }

    private void TryConfirmDrag(PointerEventData eventData)
    {
        if (_isDragConfirmed)
            return;

        float distance = Vector2.Distance(_pointerDownPosition, eventData.position);

        if (distance < _minDragDistance)
            return;

        _isDragConfirmed = true;

        OnBeginDragHandler?.Invoke(eventData);

        StopDragClickLockCheck();
        _dragClickLockCoroutine = StartCoroutine(LockClickAfterDragDelay());
    }

    private IEnumerator CheckLongPress()
    {
        yield return new WaitForSecondsRealtime(_longPressDuration);

        if (_cachedEventData != null && !_isLongPressTriggered && !_isDragConfirmed)
        {
            _isLongPressTriggered = true;
            _isClickAllowed = false;
            OnLongPressHandler?.Invoke(_cachedEventData);
        }

        _longPressCoroutine = null;
    }

    private IEnumerator LockClickAfterDragDelay()
    {
        yield return new WaitForSecondsRealtime(_dragClickLockDelay);

        if (_isDragConfirmed)
            _isClickAllowed = false;

        _dragClickLockCoroutine = null;
    }

    private void StopLongPressCheck()
    {
        if (_longPressCoroutine == null)
            return;

        StopCoroutine(_longPressCoroutine);
        _longPressCoroutine = null;
    }

    private void StopDragClickLockCheck()
    {
        if (_dragClickLockCoroutine == null)
            return;

        StopCoroutine(_dragClickLockCoroutine);
        _dragClickLockCoroutine = null;
    }

    private void OnDisable()
    {
        StopLongPressCheck();
        StopDragClickLockCheck();

        _isDragging = false;
        _isDragConfirmed = false;
        _isLongPressTriggered = false;
        _isClickAllowed = true;
        _cachedEventData = null;
    }
}