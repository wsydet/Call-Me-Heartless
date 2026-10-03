using System;
using Ember.Basic;
using Ember.Core;
using Game.Narrative;
using UnityEngine;

namespace Game.GardenCare
{
    [EmberModule(ModulePhase.Gameplay, Enabled = true)]
    public sealed class GardenCareModule : EmberSingleton<GardenCareModule>, IEmberModule, IEmberUpdate, INovelStepService
    {
        private string _id;
        private GardenCareGame _game;
        private IGardenCarePresenter _view;
        private NovelSession _session;
        private Action<string> _complete, _fail;
        private double _lastTime;
        public GardenCareGame CurrentGame => _game;
        private bool Paused => _session != null && _session.Snapshot.PauseReasons.Count > 0;

        public void OnInit()
        {
            var previous = EmberServiceLocator.TryResolve<INovelStepService>();
            if (previous != null && !ReferenceEquals(previous, this))
                throw new InvalidOperationException("已有其它通用剧情桥接服务，护菜模块未覆盖它。");
            EmberServiceLocator.Register<INovelStepService>(this);
        }

        public void Invoke(string requestKey, string executionId, string payload, Action<string> complete, Action<string> fail)
        {
            if (requestKey != "cmh.b.garden_care") { fail?.Invoke("未知护菜请求：" + requestKey); return; }
            if (string.IsNullOrEmpty(executionId) || _id != null) { fail?.Invoke("护菜请求无效或已有一局正在执行。"); return; }
            try
            {
                var config = JsonUtility.FromJson<GardenCareConfig>(payload);
                var game = new GardenCareGame(config, Guid.NewGuid().GetHashCode());
                var view = EmberServiceLocator.TryResolve<IGardenCarePresenter>();
                if (view == null) throw new InvalidOperationException("护菜 UI 服务未初始化。");
                _id = executionId; _game = game; _view = view; _complete = complete; _fail = fail;
                if (EmberModuleCollector.Instance.TryGetModule(out NarrativeModule narrative)) _session = narrative.Session;
                _lastTime = Time.realtimeSinceStartupAsDouble;
                view.Open(game, () => Act(executionId, g => g.Start()), i => Act(executionId, g => g.Pull(i)),
                    (i, hit) => Act(executionId, g => g.Deliver(i, hit)), () => Act(executionId, g => g.Continue()),
                    e => Fail(executionId, e));
                if (_id == executionId) Publish();
            }
            catch (Exception e)
            {
                if (_id == executionId) Fail(executionId, e.Message); else fail?.Invoke(e.Message);
            }
        }

        private void AdvanceClock()
        {
            double now = Time.realtimeSinceStartupAsDouble;
            _game.Tick(Math.Max(0, now - _lastTime), Paused); _lastTime = now;
        }

        private void Act(string id, Action<GardenCareGame> action)
        {
            if (_id != id || _game == null) return;
            try { AdvanceClock(); if (!Paused) action(_game); Publish(); }
            catch (Exception e) { Fail(id, e.Message); }
        }

        private void Publish()
        {
            if (_game == null) return;
            if (_game.Phase != GardenCarePhase.Closed) { _view.Refresh(_game, Paused); return; }
            string result = _game.Outcome; var callback = _complete;
            Cleanup(); callback?.Invoke(result);
        }

        public void Update()
        {
            if (_id == null) return;
            string id = _id;
            try { AdvanceClock(); Publish(); } catch (Exception e) { Fail(id, e.Message); }
        }

        private void Fail(string id, string error)
        {
            if (_id != id) return;
            var callback = _fail; Cleanup(); callback?.Invoke(error);
        }

        private void Cleanup()
        {
            var view = _view; _id = null; _game = null; _view = null; _session = null; _complete = null; _fail = null;
            try { view?.Close(); } catch (Exception e) { EmberDebug.LogError("Game.GardenCare", "护菜 UI 清理失败：" + e.Message); }
        }

        public void Abort(string executionId) { if (_id == executionId) Fail(executionId, "护菜请求已中止。"); }
        void IEmberModule.OnDestroy()
        {
            if (_id != null) Abort(_id);
            if (ReferenceEquals(EmberServiceLocator.TryResolve<INovelStepService>(), this))
                EmberServiceLocator.Unregister<INovelStepService>();
        }
        public void ResetModuleData() { ((IEmberModule)this).OnDestroy(); }
    }
}
