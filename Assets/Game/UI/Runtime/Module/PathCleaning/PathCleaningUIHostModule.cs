using System;
using Ember.Basic;
using Ember.Core;
using Ember.Input;
using Ember.UI;
using Ember.UIExtension;
using Game.Narrative;
using Game.PathCleaning;
using UnityEngine;
namespace Game.UI
{
    [EmberModule(ModulePhase.Gameplay,Enabled=true)]
    public sealed class PathCleaningUIHostModule : EmberSingleton<PathCleaningUIHostModule>,IEmberModule,IPathCleaningPresenter
    {
        private EUIItem _item;
        private string _previousMap;
        private bool _ownsMap;
        public void OnInit(){EmberServiceLocator.Register<IPathCleaningPresenter>(this);}
        public void Open(PathCleaningGame game,Action<string> select,Action<int> clean,Action<int,string> deliver,Action<int,Vector2,Vector2> wipe,Action<Vector2,Vector2,bool> sweep,Action ready,Action<string> failure)
        {
            try
            {
                if(_item!=null)throw new InvalidOperationException("打扫UI已打开");
                if(!EmberModuleCollector.Instance.TryGetModule(out NarrativeModule module)||module.Session?.View is not EUINovelReaderPage reader)throw new InvalidOperationException("阅读页面尚未准备就绪");
                var prefab=Resources.Load<GameObject>("UI/Module/PathCleaning/Prefabs/EUIPathCleaningItem");if(!prefab)throw new InvalidOperationException("打扫UI资源缺失");
                var root=UnityEngine.Object.Instantiate(prefab,reader.Page.GameObject.transform,false);
                if(!EUIItemFactory.TryCreate(root,out var item,out var error)){UnityEngine.Object.Destroy(root);throw new InvalidOperationException(error);}
                _item=item;_item.Show();
                _previousMap=EmberInputManager.Instance.CurrentMap;EmberInputManager.Instance.SwitchMap("UI");_ownsMap=true;
                ((EUIPathCleaningItem)_item.Logic).Configure(game,select,clean,deliver,wipe,sweep);ready?.Invoke();
            }
            catch(Exception e){Close();failure?.Invoke(e.Message);}
        }
        public void Refresh(PathCleaningGame game){if(_item==null)throw new InvalidOperationException("打扫UI已丢失");((EUIPathCleaningItem)_item.Logic).Refresh(game);}
        public void Close()
        {
            var item=_item;_item=null;
            try{if(item!=null){var go=item.GameObject;try{item.Dispose();}finally{if(go){if(Application.isPlaying)UnityEngine.Object.Destroy(go);else UnityEngine.Object.DestroyImmediate(go);}}}}
            finally{if(_ownsMap&&EmberInputManager.TryGetInstance(out var input)&&input.CurrentMap=="UI")input.SwitchMap(_previousMap??"Novel");_ownsMap=false;_previousMap=null;}
        }
        void IEmberModule.OnDestroy(){Close();EmberServiceLocator.Unregister<IPathCleaningPresenter>();}
        public void ResetModuleData(){((IEmberModule)this).OnDestroy();}
    }
}

