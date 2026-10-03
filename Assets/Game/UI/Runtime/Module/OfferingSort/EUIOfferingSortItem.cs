using System;
using System.Collections.Generic;
using Game.OfferingSort;
using Game.Narrative;
using UnityEngine;
using UnityEngine.UI;
namespace Game.UI
{
    public partial class EUIOfferingSortItem
    {
        private readonly List<RectTransform> _cards=new();
        private OfferingSortGame _game;
        private Action<int,int> _swap;
        private int _selectedSlot=-1;
        public void Configure(OfferingSortGame game,Action<int,int> swap)
        {
            _game=game;_swap=swap;
            Title.text=NovelLocalization.Runtime("ui.offeringSort.Title","整理祭品");
            Instruction.text=NovelLocalization.Runtime("ui.offeringSort.Instruction","点选两个祭品，交换位置；再点已选祭品可取消");
            // Keep the reader's dialogue skin visible; hints need no second opaque panel.
            var hintBackground=Hint.transform.parent.GetComponent<Image>();
            if(hintBackground)hintBackground.enabled=false;
            Canvas.ForceUpdateCanvases();
            for(int i=0;i<game.Config.ItemCount;i++)
            {
                var rt=UnityEngine.Object.Instantiate(OfferingTemplate,Board);rt.name="OfferingSlot"+i;rt.gameObject.SetActive(true);
                var handle=rt.gameObject.AddComponent<OfferingSortClickHandle>();handle.Owner=this;handle.Slot=i;
                var outline=rt.gameObject.AddComponent<Outline>();outline.effectColor=new Color(1f,.85f,.2f,1f);outline.effectDistance=new Vector2(4,4);outline.enabled=false;_cards.Add(rt);
            }
            Refresh(game);
        }
        private void Place(int i)
        {
            var rt=_cards[i];int rank=_game.Order[i];float width=Board.rect.width/_cards.Count;
            float scale=Mathf.Min(1.8f,width*.70f/_game.Config.MaximumSize);
            float size=Mathf.Lerp(_game.Config.MinimumSize,_game.Config.MaximumSize,(float)rank/(_cards.Count-1))*scale;
            rt.anchorMin=rt.anchorMax=new Vector2((i+.5f)/_cards.Count,0);rt.pivot=new Vector2(.5f,0);rt.anchoredPosition=new Vector2(0,8);rt.sizeDelta=new Vector2(size,size);
            rt.GetComponent<Outline>().enabled=i==_selectedSlot;
            rt.GetComponent<Image>().color=Color.HSVToRGB(Mathf.Repeat(.12f+rank*.618034f,1),.38f,.90f);
        }
        public void Refresh(OfferingSortGame game)
        {
            _game=game;
            Instruction.gameObject.SetActive(game.IsInteractive);
            Timer.gameObject.SetActive(game.IsInteractive);
            string key=game.Config.TimeLimitSeconds==0?"ui.offeringSort.Unlimited":"ui.offeringSort.Remaining";
            string label=NovelLocalization.Runtime(key,game.Config.TimeLimitSeconds==0?"不限时 · 已用":"剩余");
            int seconds=(int)(game.Config.TimeLimitSeconds==0?Math.Floor(game.Elapsed):Math.Ceiling(Math.Max(0,game.Config.TimeLimitSeconds-game.Elapsed)));
            Timer.text=label+" "+seconds+NovelLocalization.Runtime("ui.offeringSort.Seconds","秒");
            Hint.text=game.Settled?game.Result.Outcome=="timeout"?NovelLocalization.Runtime("ui.minigame.TimeUp","时间到了！"):NovelLocalization.Runtime("ui.offeringSort.Completed","祭品已经整理好了！"):game.HintVisible?NovelLocalization.Runtime(game.Config.HintKey,"是不是需要将他按照顺序排列"):"";
            Hint.transform.parent.gameObject.SetActive(!string.IsNullOrEmpty(Hint.text));
            if(game.Settled)_selectedSlot=-1;
            for(int i=0;i<_cards.Count;i++)Place(i);
        }
        public void SelectSlot(int slot)
        {
            if(_game==null||!_game.IsInteractive||_game.Settled||slot<0||slot>=_cards.Count)return;
            if(_selectedSlot<0){_selectedSlot=slot;Refresh(_game);return;}
            int source=_selectedSlot;_selectedSlot=-1;
            if(source!=slot)_swap?.Invoke(source,slot);
            if(_game!=null)Refresh(_game);
        }
        public override void OnClose()
        {
            _selectedSlot=-1;_swap=null;_game=null;
            foreach(var card in _cards)if(card){if(Application.isPlaying)UnityEngine.Object.Destroy(card.gameObject);else UnityEngine.Object.DestroyImmediate(card.gameObject);}_cards.Clear();base.OnClose();
        }
        public override void OnDispose(){OnClose();base.OnDispose();}
    }
}

