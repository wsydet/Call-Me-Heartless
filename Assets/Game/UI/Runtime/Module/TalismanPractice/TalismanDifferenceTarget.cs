using System;
using UnityEngine;
using UnityEngine.EventSystems;
namespace Game.UI
{
    public sealed class TalismanDifferenceTarget : MonoBehaviour, IPointerClickHandler
    {
        public int difference=-1;
        private Action<int> _click;
        public void Configure(Action<int> click) { _click=click; }
        public void OnPointerClick(PointerEventData e)
        { if(e.button == PointerEventData.InputButton.Left) _click?.Invoke(difference); }
    }
}
