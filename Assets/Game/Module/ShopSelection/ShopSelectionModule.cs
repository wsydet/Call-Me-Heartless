using System;
using Ember.Basic;
using Ember.Core;
using Game.Narrative;
using UnityEngine;

namespace Game.ShopSelection
{
    [EmberModule(ModulePhase.Gameplay, Enabled = true)]
    public sealed class ShopSelectionModule : EmberSingleton<ShopSelectionModule>, IEmberModule, IEmberUpdate, IShopSelectionService
    {
        private string _id;
        private ShopSelectionModel _model;
        private IShopSelectionPresenter _view;
        private NovelSession _session;
        private Action<string> _complete, _fail;
        private double _lastTime;
        private bool Paused => _session != null && _session.Snapshot.PauseReasons.Count > 0;

        public void OnInit() { EmberServiceLocator.Register<IShopSelectionService>(this); }
        public void Invoke(string requestKey, string executionId, ShopSelectionRequest request, Action<string> complete, Action<string> fail)
        {
            if (requestKey != "cmh.h002.drink_selection" || string.IsNullOrEmpty(executionId) || _id != null)
            { fail?.Invoke("购物请求无效或已有请求正在执行"); return; }
            try
            {
                var model = new ShopSelectionModel(request);
                var view = EmberServiceLocator.TryResolve<IShopSelectionPresenter>();
                if (view == null) throw new InvalidOperationException("购物 UI 服务未初始化");
                _id = executionId; _model = model; _view = view; _complete = complete; _fail = fail;
                if (EmberModuleCollector.Instance.TryGetModule(out NarrativeModule narrative)) _session = narrative.Session;
                _lastTime = Time.realtimeSinceStartupAsDouble;
                view.Open(model, index => Act(executionId, m => m.Select(index, Paused)),
                    () => Act(executionId, m => m.Cancel(Paused)), error => Fail(executionId, error));
                if (_id == executionId) Publish();
            }
            catch (Exception e) { if (_id == executionId) Fail(executionId, e.Message); else fail?.Invoke(e.Message); }
        }
        private void Clock()
        {
            double now = Time.realtimeSinceStartupAsDouble;
            _model.Tick(Math.Max(0, now - _lastTime), Paused); _lastTime = now;
        }
        private void Act(string id, Action<ShopSelectionModel> action)
        {
            if (_id != id || _model == null) return;
            try { Clock(); action(_model); Publish(); } catch (Exception e) { Fail(id, e.Message); }
        }
        private void Publish()
        {
            if (_model == null) return;
            if (!_model.Settled) { _view.Refresh(_model, Paused); return; }
            string result = _model.Result; var callback = _complete;
            Cleanup(); callback?.Invoke(result);
        }
        public void Update()
        {
            if (_id == null) return;
            string id = _id;
            try { Clock(); Publish(); } catch (Exception e) { Fail(id, e.Message); }
        }
        private void Fail(string id, string error)
        {
            if (_id != id) return;
            var callback = _fail; Cleanup(); callback?.Invoke(error);
        }
        private void Cleanup()
        {
            var view = _view; _id = null; _view = null; _model = null; _session = null; _complete = null; _fail = null;
            try { view?.Close(); } catch (Exception e) { EmberDebug.LogError("Game.ShopSelection", "购物 UI 清理失败：" + e.Message); }
        }
        public void Abort(string executionId) { if (_id == executionId) Cleanup(); }
        void IEmberModule.OnDestroy() { Cleanup(); EmberServiceLocator.Unregister<IShopSelectionService>(); }
        public void ResetModuleData() { ((IEmberModule)this).OnDestroy(); }
    }
}
