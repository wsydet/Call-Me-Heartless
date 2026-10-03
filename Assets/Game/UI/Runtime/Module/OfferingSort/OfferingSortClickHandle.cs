using UnityEngine;
using UnityEngine.EventSystems;
namespace Game.UI
{
    public sealed class OfferingSortClickHandle : MonoBehaviour,IPointerClickHandler
    {
        [System.NonSerialized] public EUIOfferingSortItem Owner;
        public int Slot;
        public void OnPointerClick(PointerEventData e)
        {
            if(e.button==PointerEventData.InputButton.Left)Owner?.SelectSlot(Slot);
        }
    }
}
