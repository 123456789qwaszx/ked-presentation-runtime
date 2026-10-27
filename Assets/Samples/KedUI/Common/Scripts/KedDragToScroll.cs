using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace Ked.Samples.UI
{
    /// <summary>
    /// 스크롤 목록 안의 버튼(백로그 줄의 이동 버튼)에서 시작한 끌기를 부모 ScrollRect에 넘긴다.
    /// 버튼 피드백(UIPF UI_EventHandler)이 끌기 사건을 받으면 거기서 멈춰 목록이 끌리지 않는다.
    /// 같은 오브젝트의 사건 받는 쪽은 모두 불리므로, 이것은 피드백과 나란히 받아 목록에도 전한다. 휠은 원래 올라간다.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class KedDragToScroll : MonoBehaviour,
        IInitializePotentialDragHandler,
        IBeginDragHandler,
        IDragHandler,
        IEndDragHandler
    {
        private ScrollRect _scroll;

        private ScrollRect Scroll
        {
            get
            {
                if (_scroll == null && transform.parent != null)
                    _scroll = transform.parent.GetComponentInParent<ScrollRect>();

                return _scroll;
            }
        }

        public void OnInitializePotentialDrag(PointerEventData eventData)
        {
            ScrollRect scroll = Scroll;
            if (scroll != null)
                scroll.OnInitializePotentialDrag(eventData);
        }

        public void OnBeginDrag(PointerEventData eventData)
        {
            ScrollRect scroll = Scroll;
            if (scroll != null)
                scroll.OnBeginDrag(eventData);
        }

        public void OnDrag(PointerEventData eventData)
        {
            ScrollRect scroll = Scroll;
            if (scroll != null)
                scroll.OnDrag(eventData);
        }

        public void OnEndDrag(PointerEventData eventData)
        {
            ScrollRect scroll = Scroll;
            if (scroll != null)
                scroll.OnEndDrag(eventData);
        }
    }
}
