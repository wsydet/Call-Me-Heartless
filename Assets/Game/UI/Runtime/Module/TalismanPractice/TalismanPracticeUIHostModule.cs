using System;
using Ember.Basic;
using Ember.Core;
using Ember.Input;
using Ember.UI;
using Ember.UIExtension;
using Game.Narrative;
using Game.TalismanPractice;
using UnityEngine;
namespace Game.UI
{
    [EmberModule(ModulePhase.Gameplay, Enabled=true)]
    public sealed class TalismanPracticeUIHostModule : EmberSingleton<TalismanPracticeUIHostModule>, IEmberModule, ITalismanPracticePresenter
    {
        private EUIItem _item;
        private string _previousMap;
        private bool _ownsInput, _cursorVisible;
        private CursorLockMode _cursorLock;
        public void OnInit() => EmberServiceLocator.Register<ITalismanPracticePresenter>(this);
        public void Open(TalismanPracticeGame game,Action start,Action next,Action<int> difference,Action clear,
            Action submit,Action<bool> blow,Action<int,int> select,Action<string> fail)
        {
            try
            {
                if(_item != null) throw new InvalidOperationException("D 练习界面已打开。");
                if(!EmberModuleCollector.Instance.TryGetModule(out NarrativeModule module) || module.Session?.View is not EUINovelReaderPage reader)
                    throw new InvalidOperationException("小说阅读页未准备好。");
                var prefab=Resources.Load<GameObject>("UI/Module/TalismanPractice/Prefabs/EUITalismanPracticeItem");
                if(!prefab) throw new InvalidOperationException("D 练习 Prefab 缺失。");
                var root=UnityEngine.Object.Instantiate(prefab,reader.Page.GameObject.transform,false);
                if(!EUIItemFactory.TryCreate(root,out var item,out var error))
                { if(Application.isPlaying) UnityEngine.Object.Destroy(root); else UnityEngine.Object.DestroyImmediate(root); throw new InvalidOperationException(error); }
                _item=item; item.Show();
                _previousMap=EmberInputManager.Instance.CurrentMap; _cursorVisible=Cursor.visible; _cursorLock=Cursor.lockState; _ownsInput=true;
                EmberInputManager.Instance.SwitchMap("UI"); Cursor.visible=true; Cursor.lockState=CursorLockMode.None;
                Canvas.ForceUpdateCanvases();
                ((EUITalismanPracticeItem)item.Logic).Configure(game,start,next,difference,clear,submit,blow,select);
            }
            catch(Exception e) { Close(); fail?.Invoke(e.Message); }
        }
        public void Refresh(TalismanPracticeGame game,bool paused)
        {
            if(_item == null) throw new InvalidOperationException("D 练习界面已丢失。");
            ((EUITalismanPracticeItem)_item.Logic).Refresh(game,paused);
        }
        public void Close()
        {
            var item=_item; _item=null;
            try
            {
                if(item != null)
                {
                    var go=item.GameObject;
                    try { item.Dispose(); }
                    finally { if(go) { if(Application.isPlaying) UnityEngine.Object.Destroy(go); else UnityEngine.Object.DestroyImmediate(go); } }
                }
            }
            finally
            {
                if(_ownsInput)
                {
                    if(EmberInputManager.TryGetInstance(out var input) && input.CurrentMap=="UI") input.SwitchMap(_previousMap??"Novel");
                    Cursor.visible=_cursorVisible; Cursor.lockState=_cursorLock;
                }
                _ownsInput=false; _previousMap=null;
            }
        }
        void IEmberModule.OnDestroy()
        {
            Close();
            if(ReferenceEquals(EmberServiceLocator.TryResolve<ITalismanPracticePresenter>(),this)) EmberServiceLocator.Unregister<ITalismanPracticePresenter>();
        }
        public void ResetModuleData() { ((IEmberModule)this).OnDestroy(); }
    }
}
