using System;
using Ember.Basic;
using Ember.Core;
using Game.Narrative;
using UnityEngine;
namespace Game.TalismanPractice
{
    [EmberModule(ModulePhase.Gameplay, Enabled = true)]
    public sealed class TalismanPracticeModule : EmberSingleton<TalismanPracticeModule>, IEmberModule, IEmberUpdate, ITalismanPracticeService
    {
        private string _id;
        private TalismanPracticeGame _game;
        private ITalismanPracticePresenter _view;
        private NovelSession _session;
        private Action<string> _complete, _fail;
        private double _lastTime;
        private float _elapsed;
        public TalismanPracticeGame CurrentGame => _game;
        private bool Paused => _session != null && _session.Snapshot.PauseReasons.Count > 0;
        public void OnInit()
        {
            var existing=EmberServiceLocator.TryResolve<ITalismanPracticeService>();
            if(existing != null && !ReferenceEquals(existing,this)) throw new InvalidOperationException("D 练习服务已注册。");
            EmberServiceLocator.Register<ITalismanPracticeService>(this);
        }
        public void Invoke(string requestKey, string id, TalismanPracticeConfig config, Action<string> complete, Action<string> fail)
        {
            if(requestKey != "cmh.d.talisman_practice" || string.IsNullOrEmpty(id) || _id != null)
            { fail?.Invoke("D 练习请求无效或已有练习正在执行。"); return; }
            try
            {
                var game=new TalismanPracticeGame(config);
                var view=EmberServiceLocator.TryResolve<ITalismanPracticePresenter>();
                if(view == null) throw new InvalidOperationException("D 练习 UI 服务未初始化。");
                _id=id; _game=game; _view=view; _complete=complete; _fail=fail; _elapsed=0;
                if(EmberModuleCollector.Instance.TryGetModule(out NarrativeModule narrative)) _session=narrative.Session;
                _lastTime=Time.realtimeSinceStartupAsDouble;
                view.Open(game,()=>Act(id,g=>g.Start()),()=>Act(id,g=>g.Next()),
                    n=>Act(id,g=>g.FindDifference(n)),()=>Act(id,g=>g.Clear()),()=>Act(id,g=>g.Submit()),
                    held=>{ if(held) Act(id,g=>g.SetBlowing(true)); else if(_id==id) _game?.SetBlowing(false); },
                    (layer,variant)=>Act(id,g=>g.SelectPart(layer,variant)),error=>Fail(id,error));
                if(_id == id) Publish();
            }
            catch(Exception e) { if(_id == id) Fail(id,e.Message); else fail?.Invoke(e.Message); }
        }
        private void Clock()
        {
            double now=Time.realtimeSinceStartupAsDouble;
            float delta=(float)Math.Max(0,now-_lastTime); _lastTime=now;
            Advance(delta,Paused);
        }
        public void Advance(float delta,bool paused)
        {
            if(_game == null || delta < 0 || float.IsNaN(delta) || float.IsInfinity(delta)) return;
            if(paused) { _game.SetBlowing(false); return; }
            _elapsed+=delta;
            if(!_game.Config.tutorial && _elapsed >= _game.Config.requestSeconds) { Fail(_id,"D 练习等待超过活动时间上限。"); return; }
            _game.Tick(delta,false);
        }
        private void Act(string id,Action<TalismanPracticeGame> action)
        {
            if(_id != id || _game == null) return;
            try { Clock(); if(_id != id) return; if(!Paused) action(_game); Publish(); }
            catch(Exception e) { Fail(id,e.Message); }
        }
        private void Publish()
        {
            if(_game == null) return;
            if(_game.Phase != PracticePhase.Finished) { _view.Refresh(_game,Paused); return; }
            string result=JsonUtility.ToJson(_game.Result);
            var callback=_complete; Cleanup(); callback?.Invoke(result);
        }
        public void Update()
        {
            if(_id == null) return;
            string id=_id;
            try { Clock(); Publish(); } catch(Exception e) { Fail(id,e.Message); }
        }
        private void Fail(string id,string error)
        {
            if(_id == null || _id != id) return;
            var callback=_fail; Cleanup(); callback?.Invoke(error);
        }
        private void Cleanup()
        {
            var view=_view; _id=null; _game=null; _view=null; _session=null; _complete=null; _fail=null;
            try { view?.Close(); } catch(Exception e) { EmberDebug.LogError("Game.TalismanPractice","练习 UI 清理失败："+e.Message); }
        }
        public void Abort(string id) { Fail(id,"D 练习已中止。"); }
        void IEmberModule.OnDestroy()
        {
            if(_id != null) Abort(_id);
            if(ReferenceEquals(EmberServiceLocator.TryResolve<ITalismanPracticeService>(),this))
                EmberServiceLocator.Unregister<ITalismanPracticeService>();
        }
        public void ResetModuleData() { ((IEmberModule)this).OnDestroy(); }
    }
}
