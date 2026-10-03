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
namespace Game.PathCleaning
{
    [EmberModule(ModulePhase.Gameplay, Enabled=true)]
    public sealed class PathCleaningModule : EmberSingleton<PathCleaningModule>,IEmberModule,IEmberUpdate,IPathCleaningService
    {
        private string _id;
        private PathCleaningGame _game;
        private IPathCleaningPresenter _view;
        private Action<string> _complete,_fail;
        private bool _ready;
        private double _lastTime;
        private bool _completionShown;
        private double _completionShownAt;
        public PathCleaningGame CurrentGame => _game;
        public static string BakedFingerprint()
        {
            var all=new List<byte>();foreach(var key in new[]{"path_cleaning","path_cleaning_types","path_cleaning_tools"}){var asset=Resources.Load<TextAsset>("Config/Tables/"+key);if(!asset)throw new InvalidOperationException("小游戏配表产物缺失："+key);all.AddRange(BitConverter.GetBytes(asset.bytes.Length));all.AddRange(asset.bytes);}
            using(var hash=System.Security.Cryptography.SHA256.Create())return BitConverter.ToString(hash.ComputeHash(all.ToArray())).Replace("-","").ToLowerInvariant();
        }
        public static PathCleaningConfigRow[] LoadConfig()
        {
            using(var engine=new EmberTableEngine())
            {
                var catalog=GameTables.CreateCatalog();var bytes=new Dictionary<string,byte[]>();
                foreach(var entry in catalog.Entries){var asset=Resources.Load<TextAsset>(entry.ResourcePath);if(asset)bytes[entry.TableId]=asset.bytes;}
                if(!engine.Load(catalog,bytes).Succeeded || !engine.Database.TryGetTable("path_cleaning",out EmberTable<PathCleaningConfigRow> table))throw new InvalidOperationException("小游戏配表加载失败");
                var rows=table.OrderBy(r=>r.VisitFrom).ToArray();foreach(var row in rows)PathCleaningGame.Validate(row,LoadTypes(),LoadTools());
                long next=1;
                for(int i=0;i<rows.Length;i++){if(rows[i].VisitFrom!=next || (rows[i].VisitTo==0 && i!=rows.Length-1))throw new InvalidOperationException("难度区间必须从1开始连续且不重叠");next=rows[i].VisitTo==0?0:(long)rows[i].VisitTo+1;}
                if(rows.Length==0 || next!=0)throw new InvalidOperationException("最后难度行visitTo必须填0以覆盖后续次数");
                return rows;
            }
        }
        public void OnInit(){EmberServiceLocator.Register<IPathCleaningService>(this);}
        public void Invoke(string requestKey,string executionId,string payload,Action<string> complete,Action<string> fail)
        {
            if(requestKey!="cmh.a002.path_cleaning"){fail?.Invoke("未知小游戏请求："+requestKey);return;}
            if(_id!=null){fail?.Invoke("小游戏已有执行中的请求");return;}
            try
            {
                var request=JsonUtility.FromJson<PathCleaningRequest>(payload);
                var matches=LoadConfig().Where(c=>request.Visit>=c.VisitFrom&&(c.VisitTo==0||request.Visit<=c.VisitTo)).ToArray();
                if(matches.Length!=1)throw new InvalidOperationException("当前次数必须匹配唯一难度行："+request.Visit);
                _view=EmberServiceLocator.TryResolve<IPathCleaningPresenter>();if(_view==null)throw new InvalidOperationException("小游戏UI服务不可用");
                _id=executionId;_complete=complete;_fail=fail;_ready=false;
                _game=new PathCleaningGame(matches[0],LoadTypes(),LoadTools(),Guid.NewGuid().GetHashCode());
                _view.Open(_game,t=>{if(_id==executionId)Select(t);},i=>{if(_id==executionId)Clean(i);},(i,target)=>{if(_id==executionId)Clean(i,target);},(i,from,to)=>{if(_id==executionId)Wipe(i,from,to);},(from,to,begin)=>{if(_id==executionId)Sweep(from,to,begin);},()=>{if(_id!=executionId||_ready)return;_ready=true;_lastTime=Time.realtimeSinceStartupAsDouble;},e=>Fail(executionId,e));
            }
            catch(Exception e){if(_id==executionId)Fail(executionId,e.Message);else fail?.Invoke(e.Message);}
        }
        private void Select(string tool){if(!_ready||_game==null)return;try{AdvanceClock();if(!_game.Settled)_game.Select(tool);Publish();}catch(Exception e){Fail(_id,e.Message);}}
        private void Clean(int index,string destination=null){if(!_ready||_game==null)return;try{AdvanceClock();if(!_game.Settled)_game.Clean(index,destination);Publish();}catch(Exception e){Fail(_id,e.Message);}}
        private void Wipe(int index,Vector2 from,Vector2 to){if(!_ready||_game==null)return;try{AdvanceClock();if(!_game.Settled)_game.Wipe(index,from,to);Publish();}catch(Exception e){Fail(_id,e.Message);}}
        private void Sweep(Vector2 from,Vector2 to,bool begin){if(!_ready||_game==null)return;try{AdvanceClock();if(!_game.Settled)_game.Sweep(from,to,begin);Publish();}catch(Exception e){Fail(_id,e.Message);}}
        public static PathCleaningTypeRow[] LoadTypes()=>LoadTable<PathCleaningTypeRow>("path_cleaning_types");
        public static PathCleaningToolRow[] LoadTools()=>LoadTable<PathCleaningToolRow>("path_cleaning_tools");
        private static T[] LoadTable<T>(string key) where T:class
        {
            using(var engine=new EmberTableEngine()){
                var catalog=GameTables.CreateCatalog();var bytes=new Dictionary<string,byte[]>();foreach(var e in catalog.Entries){var asset=Resources.Load<TextAsset>(e.ResourcePath);if(asset)bytes[e.TableId]=asset.bytes;}
                if(!engine.Load(catalog,bytes).Succeeded||!engine.Database.TryGetTable(key,out EmberTable<T> table))throw new InvalidOperationException("配表加载失败："+key);return table.ToArray();
            }
        }
        private void AdvanceClock(){double now=Time.realtimeSinceStartupAsDouble;_game.Tick(Math.Max(0,now-_lastTime));_lastTime=now;}
        private void Publish()
        {
            if(_game==null)return;
            if(!_game.Settled){_view.Refresh(_game);return;}
            if(_game.Result.Outcome=="success" || _game.Result.Outcome=="timeout")
            {
                if(!_completionShown){_completionShown=true;_completionShownAt=Time.realtimeSinceStartupAsDouble;}
                _view.Refresh(_game);
                double duration=_game.Result.Outcome=="timeout"?_game.Config.TimeoutResultSeconds:_game.Config.SuccessResultSeconds;
                if(Time.realtimeSinceStartupAsDouble-_completionShownAt<duration)return;
            }
            var result=JsonUtility.ToJson(_game.Result);var callback=_complete;Cleanup();callback?.Invoke(result);
        }
        public void Update(){if(_id==null||!_ready)return;try{AdvanceClock();Publish();}catch(Exception e){Fail(_id,e.Message);}}
        private void Fail(string id,string error){if(id!=_id)return;var callback=_fail;Cleanup();callback?.Invoke(error);}
        private void Cleanup(){var view=_view;_id=null;_game=null;_view=null;_complete=null;_fail=null;_ready=false;_completionShown=false;try{view?.Close();}catch(Exception e){EmberDebug.LogError("Game.PathCleaning","小游戏UI清理失败："+e.Message);}}
        public void Abort(string executionId){if(executionId==_id)Cleanup();}
        void IEmberModule.OnDestroy(){if(_id!=null)Fail(_id,"小游戏模块已退出");EmberServiceLocator.Unregister<IPathCleaningService>();}
        public void ResetModuleData(){((IEmberModule)this).OnDestroy();}
    }
}
