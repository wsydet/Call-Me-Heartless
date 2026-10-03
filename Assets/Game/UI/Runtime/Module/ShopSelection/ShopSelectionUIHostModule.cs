using System;
using Ember.Basic;
using Ember.Core;
using Ember.Input;
using Ember.UI;
using Ember.UIExtension;
using Game.ShopSelection;
using Game.Narrative;
using UnityEngine;

namespace Game.UI
{
    [EmberModule(ModulePhase.Gameplay, Enabled = true)]
    public sealed class ShopSelectionUIHostModule : EmberSingleton<ShopSelectionUIHostModule>, IEmberModule, IShopSelectionPresenter
    {
        private EUIItem _item;
        private string _previousMap;
        private bool _ownsInput, _previousCursorVisible;
        private CursorLockMode _previousCursorLock;
        public void OnInit() { EmberServiceLocator.Register<IShopSelectionPresenter>(this); }
        public void Open(ShopSelectionModel game, Action<int> select, Action cancel, Action<string> failure)
        {
            try
            {
                if (_item != null) throw new InvalidOperationException("购物 UI 已打开");
                if (!EmberModuleCollector.Instance.TryGetModule(out NarrativeModule module) || module.Session?.View is not EUINovelReaderPage reader)
                    throw new InvalidOperationException("阅读页面尚未准备就绪");
                var prefab = Resources.Load<GameObject>("UI/Module/ShopSelection/Prefabs/EUIShopSelectionItem");
                if (!prefab) throw new InvalidOperationException("购物 UI 资源缺失");
                var root = UnityEngine.Object.Instantiate(prefab, reader.Page.GameObject.transform, false);
                if (!EUIItemFactory.TryCreate(root, out var item, out var error))
                {
                    if (Application.isPlaying) UnityEngine.Object.Destroy(root); else UnityEngine.Object.DestroyImmediate(root);
                    throw new InvalidOperationException(error);
                }
                _item = item; item.Show();
                _previousMap = EmberInputManager.Instance.CurrentMap;
                _previousCursorVisible = Cursor.visible; _previousCursorLock = Cursor.lockState; _ownsInput = true;
                EmberInputManager.Instance.SwitchMap("UI"); Cursor.lockState = CursorLockMode.None; Cursor.visible = true;
                ((EUIShopSelectionItem)item.Logic).Configure(game, select, cancel);
            }
            catch (Exception e) { Close(); failure?.Invoke(e.Message); }
        }
        public void Refresh(ShopSelectionModel game, bool paused)
        {
            if (_item == null) throw new InvalidOperationException("购物 UI 已丢失");
            ((EUIShopSelectionItem)_item.Logic).Refresh(game, paused);
        }
        public void Close()
        {
            var item = _item; _item = null;
            try
            {
                if (item != null)
                {
                    var go = item.GameObject;
                    try { item.Dispose(); }
                    finally { if (go) { if (Application.isPlaying) UnityEngine.Object.Destroy(go); else UnityEngine.Object.DestroyImmediate(go); } }
                }
            }
            finally
            {
                if (_ownsInput)
                {
                    if (EmberInputManager.TryGetInstance(out var input) && input.CurrentMap == "UI") input.SwitchMap(_previousMap ?? "Novel");
                    Cursor.lockState = _previousCursorLock; Cursor.visible = _previousCursorVisible;
                }
                _ownsInput = false; _previousMap = null;
            }
        }
        void IEmberModule.OnDestroy() { Close(); EmberServiceLocator.Unregister<IShopSelectionPresenter>(); }
        public void ResetModuleData() { ((IEmberModule)this).OnDestroy(); }
    }
}

