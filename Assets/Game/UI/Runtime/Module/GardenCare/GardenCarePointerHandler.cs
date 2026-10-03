using System;
using UnityEngine;
using UnityEngine.EventSystems;

namespace Game.UI
{
    public sealed class GardenCarePointerHandler : MonoBehaviour, IBeginDragHandler, IDragHandler, IEndDragHandler
    {
        private RectTransform _rect, _layer, _refuge;
        private Func<bool> _canDrag;
        private Action<bool> _deliver;
        private Vector2 _origin, _offset;
        private bool _dragging;
        private int _pointerId;

        public void Configure(RectTransform layer, RectTransform refuge, Func<bool> canDrag, Action<bool> deliver)
        {
            ResetHandler();
            _rect = (RectTransform)transform; _layer = layer; _refuge = refuge; _canDrag = canDrag; _deliver = deliver;
        }
        public void OnBeginDrag(PointerEventData e)
        {
            if (e.button != PointerEventData.InputButton.Left || _canDrag?.Invoke() != true) return;
            if (!RectTransformUtility.ScreenPointToLocalPointInRectangle(_layer, e.position, e.pressEventCamera, out var point)) return;
            _origin = _rect.anchoredPosition; _offset = (Vector2)_rect.localPosition - point;
            _dragging = true; _pointerId = e.pointerId; _rect.SetAsLastSibling();
        }
        public void OnDrag(PointerEventData e)
        {
            if (!_dragging || e.pointerId != _pointerId) return;
            if (_canDrag?.Invoke() != true) { CancelDrag(); return; }
            if (!RectTransformUtility.ScreenPointToLocalPointInRectangle(_layer, e.position, e.pressEventCamera, out var point)) return;
            point += _offset;
            var bounds = _layer.rect; var half = _rect.rect.size * .5f;
            _rect.localPosition = new Vector3(Mathf.Clamp(point.x, bounds.xMin + half.x, bounds.xMax - half.x),
                Mathf.Clamp(point.y, bounds.yMin + half.y, bounds.yMax - half.y), _rect.localPosition.z);
        }
        public void OnEndDrag(PointerEventData e)
        {
            if (!_dragging || e.pointerId != _pointerId) return;
            bool allowed = _canDrag?.Invoke() == true;
            bool hit = allowed && RectTransformUtility.RectangleContainsScreenPoint(_refuge, e.position, e.pressEventCamera);
            CancelDrag();
            if (allowed) _deliver?.Invoke(hit);
        }
        public void CancelDrag() { if (_dragging && _rect) _rect.anchoredPosition = _origin; _dragging = false; }
        public void ResetHandler() { CancelDrag(); _canDrag = null; _deliver = null; }
        private void OnDisable() { CancelDrag(); }
    }
}

