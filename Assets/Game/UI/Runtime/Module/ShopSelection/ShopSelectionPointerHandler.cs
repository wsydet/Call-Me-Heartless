using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace Game.UI
{
    public sealed class ShopSelectionPointerHandler : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler, ISelectHandler, IDeselectHandler
    {
        private bool _hovered, _focused;
        private Vector3 _scale;
        private bool _configured;
        public void RefreshHighlight()
        {
            if (!_configured) { _scale = transform.localScale; _configured = true; }
            var button = GetComponent<Button>();
            bool active = button && button.IsInteractable() && (_hovered || _focused);
            transform.localScale = _scale * (active ? 1.08f : 1);
            var outline = GetComponent<Outline>(); if (outline) outline.enabled = active;
        }
        public void OnPointerEnter(PointerEventData e) { _hovered = true; RefreshHighlight(); }
        public void OnPointerExit(PointerEventData e) { _hovered = false; RefreshHighlight(); }
        public void OnSelect(BaseEventData e) { _focused = true; RefreshHighlight(); }
        public void OnDeselect(BaseEventData e) { _focused = false; RefreshHighlight(); }
        private void OnDisable() { _hovered = _focused = false; if (_configured) transform.localScale = _scale; }
    }
}
