using System;
using System.Collections.Generic;
using Game.PathCleaning;
using Game.Narrative;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.InputSystem;
namespace Game.UI
{
    public partial class EUIPathCleaningItem
    {
        private readonly List<RectTransform> _spots=new();
        private readonly List<RectTransform> _tools=new();
        private PathCleaningGame _game;
        private bool[] _wasCleared;
        private PathCleaningBoardStroke _boardStroke;
        private Vector2 _sweepDirection;
        private int _heldTrash=-1;
        private Image _heldTrashVisual;
        private RectTransform _tongsVisual;
        private Button _trashBinButton;
        private UnityEngine.Events.UnityAction _depositTrash;
        private readonly List<CleanEffect> _effects=new();
        private sealed class CleanEffect { public RectTransform Root;public Vector2 Origin;public double Started; }
        private static void DestroyObject(UnityEngine.Object value){if(!value)return;if(Application.isPlaying)UnityEngine.Object.Destroy(value);else UnityEngine.Object.DestroyImmediate(value);}
        private bool _cursorOwned,_previousVisible;
        private CursorLockMode _previousLock;
        private static string Text(string key,string fallback)=>NovelLocalization.Runtime(key,fallback);
        private static Color ColorOf(string value){ColorUtility.TryParseHtmlString(value,out var c);return c;}
        private static void Skin(Image image,string color,string spritePath)
        {image.color=ColorOf(color);if(!string.IsNullOrEmpty(spritePath)){image.sprite=Resources.Load<Sprite>(spritePath);if(!image.sprite)throw new InvalidOperationException("小游戏图标资源缺失："+spritePath);image.preserveAspect=true;}}
        public void Configure(PathCleaningGame game,Action<string> select,Action<int> clean,Action<int,string> deliver,Action<int,Vector2,Vector2> wipe,Action<Vector2,Vector2,bool> sweep)
        {
            var hintBackground=Hint.transform.parent.GetComponent<Image>();
            if(hintBackground){hintBackground.enabled=false;hintBackground.raycastTarget=false;}
            Hint.raycastTarget=false;
            Canvas.ForceUpdateCanvases();
            var corners=new Vector3[4];LeafPile.GetWorldCorners(corners);var low=Board.InverseTransformPoint(corners[0]);var high=Board.InverseTransformPoint(corners[2]);var bounds=Board.rect;
            game.ConfigureSweepArea(new Rect((low.x-bounds.xMin)/bounds.width,(low.y-bounds.yMin)/bounds.height,(high.x-low.x)/bounds.width,(high.y-low.y)/bounds.height),bounds.width/bounds.height);
            Board.GetComponent<Image>().raycastTarget=true;
            _game=game;_wasCleared=new bool[game.Spots.Count];Title.text=Text("ui.pathCleaning.Title","清扫山路");
            _previousVisible=Cursor.visible;_previousLock=Cursor.lockState;_cursorOwned=true;Cursor.lockState=CursorLockMode.None;
            for(int i=0;i<game.Spots.Count;i++)
            {
                int index=i;var spot=game.Spots[i];var rt=UnityEngine.Object.Instantiate(SpotTemplate,Board);rt.name="Spot"+i;rt.gameObject.SetActive(true);
                rt.anchorMin=rt.anchorMax=new Vector2((spot.Cell%4+.5f)/4,(spot.Cell/4+.5f)/5);rt.anchoredPosition=Vector2.zero;
                rt.anchorMin=rt.anchorMax=new Vector2((spot.Cell%4+.5f)/4,.23f+(spot.Cell/4+.5f)/5*.74f);
                Skin(rt.GetComponent<Image>(),spot.Type.Color,spot.Type.SpritePath);
                if(spot.Type.Id=="mud")
                {
                    rt.GetComponent<Button>().enabled=false;
                    rt.gameObject.AddComponent<PathCleaningWipeHandle>().Configure(game,index,()=>false,(from,to)=>{});
                    rt.GetComponent<Image>().raycastTarget=false;
                }
                else if(spot.Type.Id=="leaves") {rt.GetComponent<Button>().enabled=false;rt.GetComponent<Image>().raycastTarget=false;}
                else if(PathCleaningGame.CollectionTarget(spot)==null) rt.GetComponent<Button>().onClick.AddListener(()=>clean(index));
                else rt.GetComponent<Button>().onClick.AddListener(()=>
                {
                    if(game.Settled||spot.Cleared||_heldTrash>=0)return;
                    if(game.SelectedTool!=spot.Type.ToolId){clean(index);return;}
                    _heldTrash=index;Refresh(game);
                });
                var label=UnityEngine.Object.Instantiate(ToolTemplate.GetComponentInChildren<TMPro.TMP_Text>(),rt,false);label.name="Kind";label.text=spot.Type.Id=="leaves"?"落叶":spot.Type.Id=="trash"?"垃圾":"泥渍";label.fontSize=17;label.raycastTarget=false;label.color=Color.black;label.alignment=TMPro.TextAlignmentOptions.Center;label.rectTransform.anchorMin=Vector2.zero;label.rectTransform.anchorMax=Vector2.one;label.rectTransform.offsetMin=label.rectTransform.offsetMax=Vector2.zero;label.gameObject.SetActive(spot.Type.Id!="mud");
                _spots.Add(rt);
            }
            _boardStroke=Board.gameObject.GetComponent<PathCleaningBoardStroke>()??Board.gameObject.AddComponent<PathCleaningBoardStroke>();
            _boardStroke.Configure(()=>!game.Settled&&(game.SelectedTool=="brush"||game.SelectedTool=="broom"),
                (from,to,begin)=>
                {
                    if(game.SelectedTool=="broom"){_sweepDirection=to-from;sweep(from,to,begin);return;}
                    for(int i=0;i<_spots.Count&&!game.Settled;i++)
                    {
                        if(game.Spots[i].Cleared||game.Spots[i].Type.Id!="mud")continue;
                        var rt=_spots[i];var r=rt.rect;var currentBounds=Board.rect;
                        var a=rt.InverseTransformPoint(Board.TransformPoint(new Vector3(currentBounds.xMin+from.x*currentBounds.width,currentBounds.yMin+from.y*currentBounds.height,0)));
                        var b=rt.InverseTransformPoint(Board.TransformPoint(new Vector3(currentBounds.xMin+to.x*currentBounds.width,currentBounds.yMin+to.y*currentBounds.height,0)));
                        wipe(i,new Vector2((a.x-r.xMin)/r.width,(a.y-r.yMin)/r.height),new Vector2((b.x-r.xMin)/r.width,(b.y-r.yMin)/r.height));
                    }
                });
            ToolCursor.GetComponent<Image>().raycastTarget=false;
            var tongsObject=new GameObject("TongsVisual",typeof(RectTransform));tongsObject.transform.SetParent(ToolCursor,false);_tongsVisual=(RectTransform)tongsObject.transform;
            for(int side=-1;side<=1;side+=2)
            {
                var arm=new GameObject("Arm",typeof(RectTransform),typeof(Image));arm.transform.SetParent(_tongsVisual,false);
                var rect=(RectTransform)arm.transform;rect.sizeDelta=new Vector2(7,54);rect.anchoredPosition=new Vector2(side*12,15);rect.localRotation=Quaternion.Euler(0,0,-side*12);
                var image=arm.GetComponent<Image>();image.color=new Color(.76f,.82f,.8f);image.raycastTarget=false;
            }
            tongsObject.SetActive(false);
            var heldObject=new GameObject("HeldTrash",typeof(RectTransform),typeof(Image));heldObject.transform.SetParent(ToolCursor,false);
            _heldTrashVisual=heldObject.GetComponent<Image>();_heldTrashVisual.raycastTarget=false;heldObject.SetActive(false);
            _trashBinButton=TrashBin.GetComponent<Button>()??TrashBin.gameObject.AddComponent<Button>();
            _trashBinButton.targetGraphic=TrashBin.GetComponent<Image>();_trashBinButton.transition=Selectable.Transition.None;
            TrashBin.GetComponent<Image>().raycastTarget=true;
            _depositTrash=()=>
            {
                if(game.Settled||_heldTrash<0)return;
                int index=_heldTrash;_heldTrash=-1;deliver(index,"trashBin");
                if(_game==game)Refresh(game);
            };
            _trashBinButton.onClick.AddListener(_depositTrash);
            for(int i=0;i<game.Tools.Count;i++)
            {
                var data=game.Tools[i];var rt=UnityEngine.Object.Instantiate(ToolTemplate,Toolbar);rt.name="Tool_"+data.Id;rt.gameObject.SetActive(true);
                float height=1f/game.Tools.Count;rt.anchorMin=new Vector2(.07f,1-(i+1)*height+.025f);rt.anchorMax=new Vector2(.93f,1-i*height-.025f);rt.offsetMin=rt.offsetMax=Vector2.zero;
                Skin(rt.GetComponent<Image>(),data.Color,data.SpritePath);var label=rt.GetComponentInChildren<TMPro.TMP_Text>();label.text=Text(data.NameKey,data.Id=="broom"?"扫帚":data.Id=="brush"?"刷子":data.Id=="tongs"?"夹子":data.Id);label.color=new Color(.10f,.12f,.10f);
                rt.GetComponent<Button>().onClick.AddListener(()=>{if(data.Id!=game.SelectedTool)_heldTrash=-1;select(data.Id);if(_game==game)Refresh(game);});_tools.Add(rt);
            }
            Refresh(game);
            for(int i=0;i<_spots.Count;i++)
            {
                var size=new Vector2(_spots[i].rect.width/bounds.width,_spots[i].rect.height/bounds.height);
                if(game.Spots[i].Type.Id=="mud")game.ConfigureMudFootprint(i,new Vector2(size.x/game.Spots[i].MudBounds.width,size.y/game.Spots[i].MudBounds.height));
                if(game.Spots[i].Type.Id=="leaves")game.ConfigureLeafFootprint(i,size);
            }
        }
        public void Refresh(PathCleaningGame game)
        {
            LeafPile.GetComponentInChildren<TMPro.TMP_Text>().text="落叶收集区 · "+System.Linq.Enumerable.Count(game.Spots,s=>s.Cleared&&s.Type.Id=="leaves");
            TrashBin.GetComponentInChildren<TMPro.TMP_Text>().text="垃圾桶 · "+System.Linq.Enumerable.Count(game.Spots,s=>s.Cleared&&s.Type.Id=="trash");
            _game=game;bool unlimited=game.Config.TimeLimitSeconds==0;
            if(_heldTrash>=0&&(game.Settled||game.Spots[_heldTrash].Cleared||game.SelectedTool!=game.Spots[_heldTrash].Type.ToolId))_heldTrash=-1;
            int seconds=(int)(unlimited?Math.Floor(game.Elapsed):Math.Ceiling(Math.Max(0,game.Config.TimeLimitSeconds-game.Elapsed)));
            Timer.text=Text(unlimited?"ui.offeringSort.Unlimited":"ui.offeringSort.Remaining",unlimited?"不限时 · 已用":"剩余")+" "+seconds+Text("ui.offeringSort.Seconds","秒");
            Instruction.text=Text("ui.pathCleaning.Progress","已清理")+" "+game.ClearedCount+" / "+game.Spots.Count+"   "+Text("ui.pathCleaning.Instruction","夹子：点垃圾夹起，再点垃圾桶放下；扫帚：扫动落叶；刷子：拖动擦除泥渍");
            Hint.text=game.Settled&&game.Result.Outcome=="timeout"?Text("ui.minigame.TimeUp","时间到了！"):game.Elapsed<game.SmearUntil?Text("ui.pathCleaning.SmearedMud","污渍被扫开了，换刷子擦干净吧！"):!string.IsNullOrEmpty(game.HintKey)?Text(game.HintKey,game.HintKey):game.ErrorSpot>=0&&game.Elapsed<game.ErrorUntil?Text(game.SelectedTool==null?"ui.pathCleaning.SelectTool":"ui.pathCleaning.WrongTool",game.SelectedTool==null?"先选一件工具吧。":"这个工具似乎不太合适……"):"";
            for(int i=0;i<_spots.Count;i++)
            {
                var spot=game.Spots[i];var rt=_spots[i];
                if(spot.Cleared&&!_wasCleared[i]&&spot.Type.Id=="mud")
                {
                    var effect=UnityEngine.Object.Instantiate(CleanEffectTemplate,Board);effect.name="MudCleanEffect";effect.anchorMin=rt.anchorMin;effect.anchorMax=rt.anchorMax;effect.anchoredPosition=rt.anchoredPosition;effect.gameObject.SetActive(true);effect.SetAsLastSibling();
                    _effects.Add(new CleanEffect{Root=effect,Origin=effect.anchoredPosition,Started=Time.realtimeSinceStartupAsDouble});
                }
                _wasCleared[i]=spot.Cleared;rt.gameObject.SetActive(i!=_heldTrash&&(!spot.Cleared||spot.Type.Id=="leaves"));
                if(spot.Type.Id=="leaves"){rt.anchorMin=rt.anchorMax=spot.Position;rt.anchoredPosition=Vector2.zero;rt.localRotation=Quaternion.Euler(0,0,spot.LeafRotation);}
                float size=Mathf.Min(spot.Type.Id=="mud"?spot.Type.Size*1.8f:spot.Type.Size,Board.rect.width/4*.70f,Board.rect.height/5*.72f);float aspect=spot.Type.Id=="mud"?spot.MudAspectRatio:1;rt.sizeDelta=aspect>=1?new Vector2(size,size/aspect):new Vector2(size*aspect,size);
                if(spot.Type.Id=="mud")
                {
                    var mud=spot.MudBounds;rt.sizeDelta=new Vector2(rt.sizeDelta.x*mud.width,rt.sizeDelta.y*mud.height);
                    rt.anchorMin=rt.anchorMax=spot.Position+new Vector2((mud.center.x-.5f)*spot.MudFootprint.x,(mud.center.y-.5f)*spot.MudFootprint.y);rt.anchoredPosition=Vector2.zero;
                }
                rt.GetComponent<Image>().color=game.ErrorSpot==i&&game.Elapsed<game.ErrorUntil?new Color(1,.35f,.3f):spot.Type.Id=="mud"?Color.white:ColorOf(spot.Type.Color);
                rt.GetComponent<PathCleaningWipeHandle>()?.Render(spot);
                if(spot.Type.Id=="trash")rt.GetComponent<Image>().raycastTarget=game.SelectedTool==null||game.SelectedTool==spot.Type.ToolId;
            }
            for(int i=_effects.Count-1;i>=0;i--)
            {
                var effect=_effects[i];float progress=(float)((Time.realtimeSinceStartupAsDouble-effect.Started)/.75);
                if(progress>=1){DestroyObject(effect.Root.gameObject);_effects.RemoveAt(i);continue;}
                effect.Root.anchoredPosition=effect.Origin+Vector2.up*(30*progress);
                effect.Root.localScale=Vector3.one*(.7f+.4f*Mathf.Min(1,progress*4));
                effect.Root.GetComponent<CanvasGroup>().alpha=1-Mathf.Clamp01((progress-.3f)/.7f);
            }
            for(int i=0;i<_tools.Count;i++)
            {
                bool selected=game.Tools[i].Id==game.SelectedTool;_tools[i].localScale=Vector3.one*(selected?1.04f:1f);
                _tools[i].GetComponentInChildren<TMPro.TMP_Text>().fontStyle=selected?TMPro.FontStyles.Bold:TMPro.FontStyles.Normal;
            }
            Hint.transform.parent.gameObject.SetActive(!string.IsNullOrEmpty(Hint.text));
            if(_heldTrashVisual)
            {
                _heldTrashVisual.gameObject.SetActive(_heldTrash>=0);
                if(_heldTrash>=0)
                {
                    var source=_spots[_heldTrash].GetComponent<Image>();
                    _heldTrashVisual.sprite=source.sprite;_heldTrashVisual.color=source.color;_heldTrashVisual.preserveAspect=source.preserveAspect;
                    _heldTrashVisual.rectTransform.sizeDelta=_spots[_heldTrash].sizeDelta;
                    _heldTrashVisual.rectTransform.anchoredPosition=new Vector2(0,-ToolCursor.rect.height*.3f);
                }
            }
            bool show=!game.Settled&&game.SelectedTool!=null&&Mouse.current!=null&&Application.isFocused;
            ToolCursor.gameObject.SetActive(show);Cursor.visible=show?false:_previousVisible;
            if(show)
            {
                ToolCursor.localRotation=Quaternion.Euler(0,0,game.SelectedTool=="broom"&&_sweepDirection.sqrMagnitude>0?Mathf.Clamp(_sweepDirection.x*1000,-25,25):0);
                var tool=System.Linq.Enumerable.First(game.Tools,t=>t.Id==game.SelectedTool);Skin(ToolCursor.GetComponent<Image>(),tool.Color,tool.SpritePath);
                bool broom=tool.Id=="broom"&&string.IsNullOrEmpty(tool.SpritePath);BroomVisual.gameObject.SetActive(broom);if(broom)ToolCursor.GetComponent<Image>().color=Color.clear;
                bool tongs=tool.Id=="tongs"&&string.IsNullOrEmpty(tool.SpritePath);_tongsVisual.gameObject.SetActive(tongs);if(tongs)ToolCursor.GetComponent<Image>().color=Color.clear;
                var canvas=ToolCursor.GetComponentInParent<Canvas>();var camera=canvas.renderMode==RenderMode.ScreenSpaceOverlay?null:canvas.worldCamera;
                if(RectTransformUtility.ScreenPointToWorldPointInRectangle((RectTransform)ToolCursor.parent,Mouse.current.position.ReadValue(),camera,out var world))ToolCursor.position=world;
                ToolCursor.SetAsLastSibling();
            }
        }
        public override void OnClose()
        {
            _heldTrash=-1;
            if(_trashBinButton&&_depositTrash!=null)_trashBinButton.onClick.RemoveListener(_depositTrash);
            _trashBinButton=null;_depositTrash=null;
            if(_heldTrashVisual)DestroyObject(_heldTrashVisual.gameObject);_heldTrashVisual=null;
            if(_tongsVisual)DestroyObject(_tongsVisual.gameObject);_tongsVisual=null;
            if(_cursorOwned){Cursor.visible=_previousVisible;Cursor.lockState=_previousLock;_cursorOwned=false;}
            if(ToolCursor)ToolCursor.gameObject.SetActive(false);_boardStroke?.Release();_boardStroke=null;_sweepDirection=Vector2.zero;_game=null;
            foreach(var effect in _effects)if(effect.Root)DestroyObject(effect.Root.gameObject);_effects.Clear();_wasCleared=null;
            foreach(var list in new[]{_spots,_tools}){foreach(var rt in list)if(rt){rt.GetComponent<PathCleaningWipeHandle>()?.Release();rt.GetComponent<Button>().onClick.RemoveAllListeners();if(Application.isPlaying)UnityEngine.Object.Destroy(rt.gameObject);else UnityEngine.Object.DestroyImmediate(rt.gameObject);}list.Clear();}base.OnClose();
        }
        public override void OnDispose(){OnClose();base.OnDispose();}
    }
}
