using Game.Narrative;
using UnityEngine;
using System;
using Ember.Core;

namespace Game.GardenCare
{
    /// <summary>
    /// Preserves the registered bridge ID and selects the tutorial per story save.
    /// </summary>
    [CreateAssetMenu(menuName = "Call Me Heartless/菜地模块桥接", fileName = "GardenCareBridge")]
    public sealed class GardenCareBridgeSO : NovelStepServiceBridgeSO
    {
        public override string ScriptId => typeof(NovelStepServiceBridgeSO).FullName;
        public const string TutorialVariable = "ch2_gardenLearned";
        [SerializeField,HideInInspector] private string _configFingerprint;
        private sealed class Run { public INovelStepService Service; public string Id; public bool Tutorial, Settled; public float Elapsed, Timeout; }
        public override string Validate(NovelCustomStepValidation validation)
        {
            string error=base.Validate(validation); if(error!=null)return error;
            for(int plot=0;plot<2;plot++)
                if(validation==null || !validation.TryGetVariable(NovelVariableScope.Global,GardenCareSettings.VisitVariable(plot),out var count) || count.Type!=NovelValueType.Int)
                    return "护菜进入次数未声明："+GardenCareSettings.VisitVariable(plot);
            if(validation==null || !validation.TryGetVariable(NovelVariableScope.Global,TutorialVariable,out var value) || value.Type!=NovelValueType.Bool)
                return "护菜教学标记未声明。";
            try { GardenCareSettings.Load(); if(_configFingerprint!=GardenCareSettings.Fingerprint())return "护菜配表指纹未同步。"; } catch(Exception e){return e.Message;}
            return null;
        }
        public override void OnBegin(NovelCustomStepContext context)
        {
            var service=EmberServiceLocator.TryResolve<INovelStepService>();
            if(service==null || !context.TryGetVariable(NovelVariableScope.Global,TutorialVariable,out var learned))
            {context.Fail("护菜服务或教学标记缺失。");return;}
            var run=new Run{Service=service,Id=context.ExecutionId,Tutorial=!learned.Bool}; context.State=run;
            context.SetReadMode(NarrativeReadMode.Manual);context.SetInputLock(run.Id,true);
            try
            {
                if(_configFingerprint!=GardenCareSettings.Fingerprint())throw new InvalidOperationException("护菜配表已变化，请重新烘焙并同步。");
                int plot=context.Command.IntegerOperand;
                if(plot<0 || plot>1)throw new InvalidOperationException("护菜地块无效。");
                string variable=GardenCareSettings.VisitVariable(plot);
                if(!context.TryGetVariable(NovelVariableScope.Global,variable,out var count) || count.Type!=NovelValueType.Int || count.Int<0)
                    throw new InvalidOperationException("护菜进入次数无效。");
                int visit=count.Int==int.MaxValue?int.MaxValue:count.Int+1;
                var config=GardenCareSettings.Load(visit);run.Tutorial=config.tutorial;config.Validate();run.Timeout=config.requestSeconds;
                if(!context.SetVariable(NovelVariableScope.Global,variable,new NovelValue(visit),out var countError))throw new InvalidOperationException(countError);
                service.Invoke(RequestKey,run.Id,JsonUtility.ToJson(config),result=>
                {
                    if(run.Settled || !context.IsAlive)return;run.Settled=true;
                    try
                    {
                        if(result!="completed" && result!="timeout")throw new InvalidOperationException("护菜结果无效。");
                        if(run.Tutorial && result!="completed")throw new InvalidOperationException("不限时教学不能超时。");
                        if(!context.SetVariable(ResultScope,ResultVariableId,new NovelValue(result),out var error))throw new InvalidOperationException(error);
                        if(run.Tutorial && !context.SetVariable(NovelVariableScope.Global,TutorialVariable,new NovelValue(true),out error))throw new InvalidOperationException(error);
                        context.SetInputLock(run.Id,false);context.Complete();
                    }
                    catch(Exception e){context.SetInputLock(run.Id,false);context.Fail(e.Message);}
                },error=>{if(run.Settled || !context.IsAlive)return;run.Settled=true;context.SetInputLock(run.Id,false);context.Fail(error);});
            }
            catch(Exception e){Release(context);context.Fail(e.Message);}
        }
        public override void OnTick(NovelCustomStepContext context,float delta)
        {
            if(context.State is not Run run || run.Settled || run.Tutorial)return;
            run.Elapsed+=delta;if(run.Elapsed<run.Timeout)return;
            Release(context);context.Fail("护菜服务等待超时。");
        }
        private static void Release(NovelCustomStepContext context)
        {
            if(context.State is not Run run)return;run.Settled=true;
            try{run.Service.Abort(run.Id);}finally{context.SetInputLock(run.Id,false);}
        }
        public override void OnEnd(NovelCustomStepContext context)=>Release(context);
        public override void OnCancel(NovelCustomStepContext context)=>Release(context);
    }
}
