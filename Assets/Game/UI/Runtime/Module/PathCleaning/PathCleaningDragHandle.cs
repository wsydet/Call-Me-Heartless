using System;
using UnityEngine;
using UnityEngine.EventSystems;
namespace Game.UI
{
    public sealed class PathCleaningDragHandle : MonoBehaviour, IBeginDragHandler, IDragHandler, IEndDragHandler
    {
        private Func<bool> _canDrag;
        private Action<Vector2,Camera> _drop;
        private RectTransform _rect;
        private Vector2 _origin;
        public bool IsDragging {get;private set;}
        public void Configure(Func<bool> canDrag, Action<Vector2,Camera> drop) { _canDrag=canDrag;_drop=drop;_rect=(RectTransform)transform; }
        public void OnBeginDrag(PointerEventData e) { if(e.button!=PointerEventData.InputButton.Left||_canDrag?.Invoke()!=true)return;_origin=_rect.anchoredPosition;IsDragging=true;transform.SetAsLastSibling(); }
        public void OnDrag(PointerEventData e) { if(!IsDragging)return;if(RectTransformUtility.ScreenPointToWorldPointInRectangle((RectTransform)_rect.parent,e.position,e.pressEventCamera,out var world))_rect.position=world; }
        public void OnEndDrag(PointerEventData e) { if(!IsDragging)return;IsDragging=false;_rect.anchoredPosition=_origin;_drop?.Invoke(e.position,e.pressEventCamera); }
        private void OnDisable() { if(IsDragging&&_rect)_rect.anchoredPosition=_origin;IsDragging=false; }
    }
}
