using System;
using Ember.Core;
using Game.Narrative;
using Game.CMH.Rewards;
using UnityEngine;
namespace Game.OfferingSort
{
    [CreateAssetMenu(menuName="Call Me Heartless/祭品排序步骤")]
    public sealed class OfferingSortStepSO : NovelCustomStepSO
    {
        [SerializeField,HideInInspector] private string _configFingerprint;
        [SerializeField] private bool _previewOnly;
        public override string ScriptId => _previewOnly ? "Game.OfferingSort.OfferingSortPreview" : "Game.OfferingSort.OfferingSortStep";
        public override string DisplayName => _previewOnly ? "展示祭品（尚未开始）" : "盲盒祭品排序";
        public override string Summary()=>"按小游戏次数选择难度配表 · 0秒不限时";
        private sealed class Run {public IOfferingSortService Service;public IDisposable Pause;public string Id;public bool Settled;}
        public override string Validate(NovelCustomStepValidation validation)
        {
            foreach(var pair in new[]{("ch2_aBoxVisits",NovelValueType.Int),("ch2_aBoxElapsedMs",NovelValueType.Int),("ch2_aBoxOutcome",NovelValueType.String),("ch2_aBoxRewardId",NovelValueType.String),("ch2_aBoxMonthlyBonus",NovelValueType.Int)})
                if(!validation.TryGetVariable(NovelVariableScope.Global,pair.Item1,out var v)||v.Type!=pair.Item2)return "小游戏全局变量缺失或类型错误："+pair.Item1;
            if(string.IsNullOrEmpty(_configFingerprint))return "小游戏配置指纹未同步";
            try
            {
                if(_configFingerprint!=OfferingSortModule.BakedFingerprint())return "小游戏配置已变化，请同步步骤指纹";
                foreach(var config in OfferingSortModule.LoadConfig())foreach(var id in new[]{config.FastRewardId,config.MediumRewardId,config.SlowRewardId,config.TimeoutRewardId})
                {string error=NovelRewardService.ValidateReward(id,validation);if(error!=null)return error;}
            }catch(Exception e){return e.Message;}
            return null;
        }
        public override void OnBegin(NovelCustomStepContext context)
        {
            if(_configFingerprint!=OfferingSortModule.BakedFingerprint()){context.Fail("小游戏配置已变化，请同步步骤指纹");return;}
            var service=EmberServiceLocator.TryResolve<IOfferingSortService>();if(service==null){context.Fail("小游戏模块未初始化");return;}
            if(!context.TryGetVariable(NovelVariableScope.Global,"ch2_aBoxVisits",out var count)||count.Int<0||count.Int==int.MaxValue){context.Fail("小游戏次数无效");return;}
            if(_previewOnly)
            {
                context.State=new Run {Service=service,Id=context.ExecutionId};
                service.Invoke("cmh.a001.offering_sort.preview",context.ExecutionId,JsonUtility.ToJson(new OfferingSortRequest {Visit=count.Int+1}),
                    _=>{if(context.IsAlive)context.Complete();},e=>{if(context.IsAlive)context.Fail(e);});
                return;
            }
            var run=new Run {Service=service,Id=context.ExecutionId};context.State=run;
            context.SetReadMode(NarrativeReadMode.Manual);context.SetInputLock(run.Id,true);run.Pause=context.AcquirePause();
            int visit=count.Int+1;
            if(!context.SetVariable(NovelVariableScope.Global,"ch2_aBoxVisits",new NovelValue(visit),out var error)){Release(context,run);context.Fail(error);return;}
            try{service.Invoke("cmh.a001.offering_sort",run.Id,JsonUtility.ToJson(new OfferingSortRequest {Visit=visit}),result=>{
                if(run.Settled||!context.IsAlive)return;run.Settled=true;
                try{
                    var value=JsonUtility.FromJson<OfferingSortResult>(result);
                    if(value==null||value.ElapsedMs<0||(value.Outcome!="success"&&value.Outcome!="timeout"))throw new InvalidOperationException("小游戏返回结果无效");
                    if(!NovelRewardService.TryGet(value.RewardId,out _))throw new InvalidOperationException("小游戏奖励ID不存在");
                    if(!context.SetVariable(NovelVariableScope.Global,"ch2_aBoxElapsedMs",new NovelValue(value.ElapsedMs),out error)||!context.SetVariable(NovelVariableScope.Global,"ch2_aBoxOutcome",new NovelValue(value.Outcome),out error)||!context.SetVariable(NovelVariableScope.Global,"ch2_aBoxRewardId",new NovelValue(value.RewardId),out error))throw new InvalidOperationException(error);
                    Release(context,run);NovelRewardService.Apply(context,value.RewardId);
                }catch(Exception e){Release(context,run);context.Fail(e.Message);}
            },e=>{if(run.Settled||!context.IsAlive)return;run.Settled=true;Release(context,run);context.Fail(e);});}
            catch(Exception e){run.Service.Abort(run.Id);Release(context,run);context.Fail(e.Message);}
        }
        private static void Release(NovelCustomStepContext context,Run run){context.SetInputLock(run.Id,false);run.Pause?.Dispose();run.Pause=null;}
        public override void OnEnd(NovelCustomStepContext context){if(!_previewOnly && context.State is Run run){Release(context,run);run.Settled=true;run.Service.Abort(run.Id);}}
        public override void OnCancel(NovelCustomStepContext context){if(context.State is Run run){run.Settled=true;run.Service.Abort(run.Id);Release(context,run);}}
    }
}

