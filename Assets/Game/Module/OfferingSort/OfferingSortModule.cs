using System;
using System.Collections.Generic;
using System.Linq;
using Ember.Basic;
using Ember.Core;
using Ember.Table;
using Game.Narrative;
using Game.Table;
using Game.Table.Generated;
using UnityEngine;
namespace Game.OfferingSort
{
    [EmberModule(ModulePhase.Gameplay, Enabled=true)]
    public sealed class OfferingSortModule : EmberSingleton<OfferingSortModule>,IEmberModule,IEmberUpdate,IOfferingSortService
    {
        private string _id;
        private OfferingSortGame _game;
        private IOfferingSortPresenter _view;
        private Action<string> _complete,_fail;
        private bool _ready;
        private bool _preview;
        private NovelSession _previewSession;
        private double _lastTime;
        private double _completionShownAt;
        private bool _completionShown;
        public OfferingSortGame CurrentGame => _game;
        public static string BakedFingerprint()
        {
            var asset=Resources.Load<TextAsset>("Config/Tables/offering_sort");if(!asset)throw new InvalidOperationException("小游戏配表产物缺失");
            using(var hash=System.Security.Cryptography.SHA256.Create())return BitConverter.ToString(hash.ComputeHash(asset.bytes)).Replace("-","").ToLowerInvariant();
        }
        public static OfferingSortConfigRow[] LoadConfig()
        {
            using(var engine=new EmberTableEngine())
            {
                var catalog=GameTables.CreateCatalog();var bytes=new Dictionary<string,byte[]>();
                foreach(var entry in catalog.Entries){var asset=Resources.Load<TextAsset>(entry.ResourcePath);if(asset)bytes[entry.TableId]=asset.bytes;}
                if(!engine.Load(catalog,bytes).Succeeded || !engine.Database.TryGetTable("offering_sort",out EmberTable<OfferingSortConfigRow> table))throw new InvalidOperationException("小游戏配表加载失败");
                var rows=table.OrderBy(r=>r.VisitFrom).ToArray();foreach(var row in rows)OfferingSortGame.Validate(row);
                long next=1;
                for(int i=0;i<rows.Length;i++){if(rows[i].VisitFrom!=next || (rows[i].VisitTo==0 && i!=rows.Length-1))throw new InvalidOperationException("难度区间必须从1开始连续且不重叠");next=rows[i].VisitTo==0?0:(long)rows[i].VisitTo+1;}
                if(rows.Length==0 || next!=0)throw new InvalidOperationException("最后难度行visitTo必须填0以覆盖后续次数");
                return rows;
            }
        }
        public void OnInit(){EmberServiceLocator.Register<IOfferingSortService>(this);}
        public void Invoke(string requestKey,string executionId,string payload,Action<string> complete,Action<string> fail)
        {
            bool preview=requestKey=="cmh.a001.offering_sort.preview";
            if(!preview && requestKey!="cmh.a001.offering_sort"){fail?.Invoke("未知小游戏请求："+requestKey);return;}
            if(_id!=null && (! _preview || preview)){fail?.Invoke("小游戏已有执行中的请求");return;}
            try
            {
                var request=JsonUtility.FromJson<OfferingSortRequest>(payload);
                var matches=LoadConfig().Where(c=>request.Visit>=c.VisitFrom&&(c.VisitTo==0||request.Visit<=c.VisitTo)).ToArray();
                if(matches.Length!=1)throw new InvalidOperationException("当前次数必须匹配唯一难度行："+request.Visit);
                if(_preview && !preview)
                {
                    if(!_ready || _game.Config.Id!=matches[0].Id)throw new InvalidOperationException("祭品展示与开始阶段不匹配");
                    _id=executionId;_preview=false;_previewSession=null;_complete=complete;_fail=fail;
                    _game.IsInteractive=true;_lastTime=Time.realtimeSinceStartupAsDouble;_view.Refresh(_game);return;
                }
                _view=EmberServiceLocator.TryResolve<IOfferingSortPresenter>();if(_view==null)throw new InvalidOperationException("小游戏UI服务不可用");
                _id=executionId;_complete=complete;_fail=fail;_ready=false;_preview=preview;
                if(preview && EmberModuleCollector.Instance.TryGetModule(out NarrativeModule narrative))_previewSession=narrative.Session;
                _game=new OfferingSortGame(matches[0],Guid.NewGuid().GetHashCode());_game.IsInteractive=!preview;
                _view.Open(_game,(a,b)=>{if(_id!=null)Swap(a,b);},()=>{
                    if(_id!=executionId||_ready)return;_ready=true;_lastTime=Time.realtimeSinceStartupAsDouble;
                    if(_preview){var callback=_complete;_complete=null;_fail=null;callback?.Invoke("{}");}
                },e=>Fail(executionId,e));
            }
            catch(Exception e){if(_id==executionId || _preview) {Cleanup();fail?.Invoke(e.Message);}else fail?.Invoke(e.Message);}
        }
        private void Swap(int from,int to)
        {
            if(!_ready || _preview || _game==null)return;AdvanceClock();
            if(_game==null)return;_game.Swap(from,to);Publish();
        }
        private void AdvanceClock(){double now=Time.realtimeSinceStartupAsDouble;_game.Tick(Math.Max(0,now-_lastTime));_lastTime=now;}
        private void Publish()
        {
            if(_game==null)return;
            if(!_game.Settled){_view.Refresh(_game);return;}
            if(_game.Result.Outcome=="success" || _game.Result.Outcome=="timeout")
            {
                if(!_completionShown){_view.Refresh(_game);_completionShown=true;_completionShownAt=Time.realtimeSinceStartupAsDouble;return;}
                double duration=_game.Result.Outcome=="timeout"?_game.Config.TimeoutResultSeconds:_game.Config.SuccessResultSeconds;
                if(Time.realtimeSinceStartupAsDouble-_completionShownAt<duration)return;
            }
            var result=JsonUtility.ToJson(_game.Result);var callback=_complete;Cleanup();callback?.Invoke(result);
        }
        public void Update(){if(_preview){if(_previewSession!=null && (_previewSession.IsDisposed || !EmberModuleCollector.Instance.TryGetModule(out NarrativeModule owner) || owner.Session!=_previewSession))Cleanup();return;}if(_id==null||!_ready)return;try{AdvanceClock();Publish();}catch(Exception e){Fail(_id,e.Message);}}
        private void Fail(string id,string error){if(id!=_id)return;var callback=_fail;Cleanup();callback?.Invoke(error);}
        private void Cleanup(){var view=_view;_id=null;_game=null;_view=null;_complete=null;_fail=null;_ready=false;_preview=false;_previewSession=null;_completionShown=false;try{view?.Close();}catch(Exception e){EmberDebug.LogError("Game.OfferingSort","小游戏UI清理失败："+e.Message);}}
        public void Abort(string executionId){if(executionId==_id)Cleanup();}
        void IEmberModule.OnDestroy(){if(_id!=null)Fail(_id,"小游戏模块已退出");EmberServiceLocator.Unregister<IOfferingSortService>();}
        public void ResetModuleData(){((IEmberModule)this).OnDestroy();}
    }
}
