using System;
using UnityEngine;
using UnityEngine.EventSystems;
namespace Game.UI
{
    /// <summary>整块路面接收连续手势，清除单个物体不会结束当前刷洗/扫叶动作。</summary>
    public sealed class PathCleaningBoardStroke : MonoBehaviour, IPointerDownHandler, IPointerUpHandler,
        IInitializePotentialDragHandler, IBeginDragHandler, IDragHandler, IEndDragHandler
    {
        private Func<bool> _canStroke;
        private Action<Vector2,Vector2,bool> _stroke;
        private RectTransform _board;
        private bool _held;
        private Vector2 _previous;
        public void Configure(Func<bool> canStroke,Action<Vector2,Vector2,bool> stroke){_board=(RectTransform)transform;_canStroke=canStroke;_stroke=stroke;}
        private bool Position(PointerEventData e,out Vector2 point)
        {
            point=default;if(!RectTransformUtility.ScreenPointToLocalPointInRectangle(_board,e.position,e.pressEventCamera,out var local))return false;
            var r=_board.rect;if(r.width<=0||r.height<=0)return false;point=new Vector2((local.x-r.xMin)/r.width,(local.y-r.yMin)/r.height);return true;
        }
        public void OnPointerDown(PointerEventData e)
        {
            if(e.button!=PointerEventData.InputButton.Left||_canStroke?.Invoke()!=true||!Position(e,out _previous))return;
            _held=true;_stroke?.Invoke(_previous,_previous,true);
        }
        public void OnInitializePotentialDrag(PointerEventData e){e.useDragThreshold=false;}
        public void OnBeginDrag(PointerEventData e){}
        public void OnDrag(PointerEventData e)
        {
            if(!_held)return;if(_canStroke?.Invoke()!=true){_held=false;return;}
            if(!Position(e,out var point))return;var previous=_previous;_previous=point;_stroke?.Invoke(previous,point,false);
        }
        public void OnPointerUp(PointerEventData e){_held=false;}
        public void OnEndDrag(PointerEventData e){_held=false;}
        private void OnDisable(){_held=false;}
        public void Release(){_held=false;_canStroke=null;_stroke=null;}
    }
}
