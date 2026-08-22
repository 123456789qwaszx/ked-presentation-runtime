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
    // 지금 이 값은 동작하지 않음.
    // TryConfirmDrag는 유니티의 OnBeginDrag / OnDrag에서만 불리는데,
    // 유니티는 EventSystem.pixelDragThreshold(기본값 10)를 넘어야 OnBeginDrag를 보낸다.
    // 즉 거리 검사에 도달한 시점에 이미 10px 이상이므로, 값을 바꿔도 조작감이 변하지 않음.
    //
    // 실제로 쓰려면 둘 중 하나가 선행:
    //  [1] EventSystem의 pixelDragThreshold를 이 값보다 작게 내린다.
    //    가장 싸지만 전역 설정이라 스크롤뷰 등 다른 드래그에 같이 영향을 줌.
    //  [2] 드래그 확정을 유니티의 OnBeginDrag에 얹지 말고,
    //    OnPointerDown 이후 포인터 위치를 이 클래스가 직접 추적해 판정.
    [SerializeField] private float _minDragDistance = 3.8f;

    private bool _isDragging;
    private bool _isDragConfirmed;
    private bool _isLongPressTriggered;

    private Vector2 _pointerDownPosition;
    private PointerEventData _cachedEventData;

    private Coroutine _longPressCoroutine;

    // 유니티는 press와 drag를 같은 오브젝트가 받으면 eligibleForClick을 내리지 않음.
    // 그래서 드래그나 롱프레스 뒤에도 OnPointerClick이 그대로 옴.
    //
    // 유니티의 릴리즈 순서는 PointerUp → PointerClick → EndDrag다.
    // 두 플래그를 리셋하는 곳이 OnEndDrag / OnPointerDown이므로, 클릭이 도착하는 시점에
    // 둘 다 아직 살아 있다. 시간(지연 코루틴)이 아니라 상태로 판정해야함.
    public void OnPointerClick(PointerEventData eventData)
    {
        if (_isDragConfirmed || _isLongPressTriggered)
            return;

        OnClickHandler?.Invoke(eventData);
    }

    public void OnPointerDown(PointerEventData eventData)
    {
        StopLongPressCheck();

        _isDragging = false;
        _isDragConfirmed = false;
        _isLongPressTriggered = false;

        _pointerDownPosition = eventData.position;
        _cachedEventData = eventData;

        OnPointerDownHandler?.Invoke(eventData);

        if (OnLongPressHandler != null)
            _longPressCoroutine = StartCoroutine(CheckLongPress());
    }

    public void OnPointerUp(PointerEventData eventData)
    {
        StopLongPressCheck();

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

    // 클릭보다 뒤에 오므로, 여기서 리셋해도 OnPointerClick의 판정은 이미 끝나 있음.
    public void OnEndDrag(PointerEventData eventData)
    {
        if (_isDragConfirmed)
            OnEndDragHandler?.Invoke(eventData);

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
    }

    private IEnumerator CheckLongPress()
    {
        yield return new WaitForSecondsRealtime(_longPressDuration);

        if (_cachedEventData != null && !_isLongPressTriggered && !_isDragConfirmed)
        {
            _isLongPressTriggered = true;
            OnLongPressHandler?.Invoke(_cachedEventData);
        }

        _longPressCoroutine = null;
    }

    private void StopLongPressCheck()
    {
        if (_longPressCoroutine == null)
            return;

        StopCoroutine(_longPressCoroutine);
        _longPressCoroutine = null;
    }

    private void OnDisable()
    {
        StopLongPressCheck();

        _isDragging = false;
        _isDragConfirmed = false;
        _isLongPressTriggered = false;
        _cachedEventData = null;
    }
}