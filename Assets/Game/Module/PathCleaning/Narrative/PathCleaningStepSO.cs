using System;
using Ember.Core;
using Game.Narrative;
using Game.CMH.Rewards;
using UnityEngine;
namespace Game.PathCleaning
{
    [CreateAssetMenu(menuName="Call Me Heartless/山路打扫步骤")]
    public sealed class PathCleaningStepSO : NovelCustomStepSO
    {
        [SerializeField,HideInInspector] private string _configFingerprint;
        public override string ScriptId => "Game.PathCleaning.PathCleaningStep";
        public override string DisplayName => "山路打扫";
        public override string Summary()=>"按小游戏次数选择难度配表 · 0秒不限时";
        private sealed class Run {public IPathCleaningService Service;public IDisposable Pause;public string Id;public bool Settled;}
        public override string Validate(NovelCustomStepValidation validation)
        {
            foreach(var pair in new[]{("ch2_aCleaningVisits",NovelValueType.Int),("ch2_aCleaningElapsedMs",NovelValueType.Int),("ch2_aCleaningOutcome",NovelValueType.String),("ch2_aCleaningRewardId",NovelValueType.String),("ch2_aCleaningMonthlyBonus",NovelValueType.Int)})
                if(!validation.TryGetVariable(NovelVariableScope.Global,pair.Item1,out var v)||v.Type!=pair.Item2)return "小游戏全局变量缺失或类型错误："+pair.Item1;
            if(string.IsNullOrEmpty(_configFingerprint))return "小游戏配置指纹未同步";
            try
            {
                if(_configFingerprint!=PathCleaningModule.BakedFingerprint())return "小游戏配置已变化，请同步步骤指纹";
                foreach(var config in PathCleaningModule.LoadConfig())foreach(var id in new[]{config.FastRewardId,config.MediumRewardId,config.SlowRewardId,config.TimeoutRewardId})
                {string error=NovelRewardService.ValidateReward(id,validation);if(error!=null)return error;}
            }catch(Exception e){return e.Message;}
            return null;
        }
        public override void OnBegin(NovelCustomStepContext context)
        {
            if(_configFingerprint!=PathCleaningModule.BakedFingerprint()){context.Fail("小游戏配置已变化，请同步步骤指纹");return;}
            var service=EmberServiceLocator.TryResolve<IPathCleaningService>();if(service==null){context.Fail("小游戏模块未初始化");return;}
            if(!context.TryGetVariable(NovelVariableScope.Global,"ch2_aCleaningVisits",out var count)||count.Int<0||count.Int==int.MaxValue){context.Fail("小游戏次数无效");return;}
            var run=new Run {Service=service,Id=context.ExecutionId};context.State=run;
            context.SetReadMode(NarrativeReadMode.Manual);context.SetInputLock(run.Id,true);run.Pause=context.AcquirePause();
            int visit=count.Int+1;
            if(!context.SetVariable(NovelVariableScope.Global,"ch2_aCleaningVisits",new NovelValue(visit),out var error)){Release(context,run);context.Fail(error);return;}
            try{service.Invoke("cmh.a002.path_cleaning",run.Id,JsonUtility.ToJson(new PathCleaningRequest {Visit=visit}),result=>{
                if(run.Settled||!context.IsAlive)return;run.Settled=true;
                try{
                    var value=JsonUtility.FromJson<PathCleaningResult>(result);
                    if(value==null||value.ElapsedMs<0||(value.Outcome!="success"&&value.Outcome!="timeout"))throw new InvalidOperationException("小游戏返回结果无效");
                    if(!NovelRewardService.TryGet(value.RewardId,out _))throw new InvalidOperationException("小游戏奖励ID不存在");
                    if(!context.SetVariable(NovelVariableScope.Global,"ch2_aCleaningElapsedMs",new NovelValue(value.ElapsedMs),out error)||!context.SetVariable(NovelVariableScope.Global,"ch2_aCleaningOutcome",new NovelValue(value.Outcome),out error)||!context.SetVariable(NovelVariableScope.Global,"ch2_aCleaningRewardId",new NovelValue(value.RewardId),out error))throw new InvalidOperationException(error);
                    Release(context,run);NovelRewardService.Apply(context,value.RewardId);
                }catch(Exception e){Release(context,run);context.Fail(e.Message);}
            },e=>{if(run.Settled||!context.IsAlive)return;run.Settled=true;Release(context,run);context.Fail(e);});}
            catch(Exception e){run.Service.Abort(run.Id);Release(context,run);context.Fail(e.Message);}
        }
        private static void Release(NovelCustomStepContext context,Run run){context.SetInputLock(run.Id,false);run.Pause?.Dispose();run.Pause=null;}
        public override void OnEnd(NovelCustomStepContext context){if(context.State is Run run){Release(context,run);run.Settled=true;run.Service.Abort(run.Id);}}
        public override void OnCancel(NovelCustomStepContext context){if(context.State is Run run){run.Settled=true;run.Service.Abort(run.Id);Release(context,run);}}
    }
}

