using System;
using Ember.Basic;
using Ember.Core;
using Ember.Input;
using Ember.UI;
using Ember.UIExtension;
using Game.Narrative;
using Game.OfferingSort;
using UnityEngine;
namespace Game.UI
{
    [EmberModule(ModulePhase.Gameplay,Enabled=true)]
    public sealed class OfferingSortUIHostModule : EmberSingleton<OfferingSortUIHostModule>,IEmberModule,IOfferingSortPresenter
    {
        private EUIItem _item;
        private string _previousMap;
        private bool _ownsMap;
        public void OnInit(){EmberServiceLocator.Register<IOfferingSortPresenter>(this);}
        public void Open(OfferingSortGame game,Action<int,int> swap,Action ready,Action<string> failure)
        {
            try
            {
                if(_item!=null)throw new InvalidOperationException("排序UI已打开");
                if(!EmberModuleCollector.Instance.TryGetModule(out NarrativeModule module)||module.Session?.View is not EUINovelReaderPage reader)throw new InvalidOperationException("阅读页面尚未准备就绪");
                var prefab=Resources.Load<GameObject>("UI/Module/OfferingSort/Prefabs/EUIOfferingSortItem");if(!prefab)throw new InvalidOperationException("排序UI资源缺失");
                var root=UnityEngine.Object.Instantiate(prefab,reader.Page.GameObject.transform,false);
                if(!EUIItemFactory.TryCreate(root,out var item,out var error)){UnityEngine.Object.Destroy(root);throw new InvalidOperationException(error);}
                _item=item;_item.Show();
                ApplyInteraction(game);
                ((EUIOfferingSortItem)_item.Logic).Configure(game,swap);ready?.Invoke();
            }
            catch(Exception e){Close();failure?.Invoke(e.Message);}
        }
        public void Refresh(OfferingSortGame game){if(_item==null)throw new InvalidOperationException("排序UI已丢失");ApplyInteraction(game);((EUIOfferingSortItem)_item.Logic).Refresh(game);}
        private void ApplyInteraction(OfferingSortGame game)
        {
            _item.GameObject.GetComponent<CanvasGroup>().blocksRaycasts=game.IsInteractive;
            if(game.IsInteractive && !_ownsMap){_previousMap=EmberInputManager.Instance.CurrentMap;EmberInputManager.Instance.SwitchMap("UI");_ownsMap=true;}
        }
        public void Close()
        {
            var item=_item;_item=null;
            try{if(item!=null){var go=item.GameObject;try{item.Dispose();}finally{if(go){if(Application.isPlaying)UnityEngine.Object.Destroy(go);else UnityEngine.Object.DestroyImmediate(go);}}}}
            finally{if(_ownsMap&&EmberInputManager.TryGetInstance(out var input)&&input.CurrentMap=="UI")input.SwitchMap(_previousMap??"Novel");_ownsMap=false;_previousMap=null;}
        }
        void IEmberModule.OnDestroy(){Close();EmberServiceLocator.Unregister<IOfferingSortPresenter>();}
        public void ResetModuleData(){((IEmberModule)this).OnDestroy();}
    }
}

