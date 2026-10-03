using System;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;

namespace Game.UI
{
    /// <summary>Combines held pointer and space input, releasing breath on pause, focus loss or disposal.</summary>
    public sealed class TalismanBreathInput : MonoBehaviour, IPointerDownHandler, IPointerUpHandler, IPointerExitHandler
    {
        private Action<bool> _changed;
        private bool _allowed, _pointerHeld, _keyboardHeld, _sent, _awaitRelease;
        private int _pointerId;

        public void Configure(Action<bool> changed)
        {
            ResetInput();
            _changed = changed;
            _awaitRelease = Keyboard.current?.spaceKey.isPressed ?? false;
        }

        public void SetAllowed(bool allowed)
        {
            if (_allowed == allowed) return;
            _allowed = allowed;
            if (!allowed) ResetInput();
        }

        private void Update() => SampleKeyboard(Keyboard.current?.spaceKey.isPressed ?? false);

        public void SampleKeyboard(bool held)
        {
            if (!held) _awaitRelease = false;
            if (!_allowed && held) _awaitRelease = true;
            _keyboardHeld = _allowed && held && !_awaitRelease;
            Publish();
        }

        public void OnPointerDown(PointerEventData e)
        {
            if (!_allowed || e.button != PointerEventData.InputButton.Left || _pointerHeld) return;
            _pointerId = e.pointerId; _pointerHeld = true; Publish();
        }

        public void OnPointerUp(PointerEventData e)
        {
            if (e.pointerId != _pointerId) return;
            _pointerHeld = false; Publish();
        }

        public void OnPointerExit(PointerEventData e) => OnPointerUp(e);

        private void Publish()
        {
            bool held = _allowed && (_pointerHeld || _keyboardHeld);
            if (held == _sent) return;
            _sent = held;
            _changed?.Invoke(held);
        }

        private void ResetInput()
        {
            _awaitRelease = _awaitRelease || _keyboardHeld;
            _pointerHeld = _keyboardHeld = false;
            Publish();
        }

        private void OnApplicationFocus(bool focused) { if (!focused) ResetInput(); }
        private void OnDisable() => ResetInput();
        public void Clear() { ResetInput(); _changed = null; _allowed = false; }
    }
}
