using System;
using Ember.Core;
using Game.Narrative;
using UnityEngine;
namespace Game.TalismanPractice
{
    [CreateAssetMenu(menuName="Call Me Heartless/D 学习项目",fileName="TalismanPracticeStep")]
    public sealed class TalismanPracticeStepSO : NovelCustomStepSO
    {
        [SerializeField,HideInInspector] private string _configFingerprint;
        public TalismanPracticeConfig Config => StudySettings.Load();
        public override string ScriptId => "Game.TalismanPractice.Training";
        public override string DisplayName => "学习所选项目";
        public override string Summary() => "三种符样共用 · 占位素材 · 不额外发放奖励";
        public static string TutorialVariable(PracticeStage stage) => "ch2_studyLearned_"+(int)stage;
        private sealed class Run { public ITalismanPracticeService Service; public string Id; public bool Settled; }
        public override string Validate(NovelCustomStepValidation validation)
        {
            try { Config.Validate(); FeatherDifficulty.Load(); if(_configFingerprint != StudySettings.Fingerprint()) return "学习配表指纹未同步。"; } catch(Exception e) { return e.Message; }
            foreach(PracticeStage stage in Enum.GetValues(typeof(PracticeStage)))
                if(validation == null || !validation.TryGetVariable(NovelVariableScope.Global,StudySettings.VisitVariable(stage),out var progress) || progress.Type != NovelValueType.Int)
                    return "学习进入次数未声明："+StudySettings.VisitVariable(stage);
            foreach(PracticeStage stage in Enum.GetValues(typeof(PracticeStage)))
                if(validation==null || !validation.TryGetVariable(NovelVariableScope.Global,TutorialVariable(stage),out var learned) || learned.Type!=NovelValueType.Bool)
                    return "学习教学标记未声明："+TutorialVariable(stage);
            if(validation == null || !validation.TryGetVariable(NovelVariableScope.Chapter,"d_talismanOutcome",out var value) || value.Type != NovelValueType.String)
                return "D 练习结果变量 d_talismanOutcome 未声明为章节 String。";
            return null;
        }
        public override void OnBegin(NovelCustomStepContext context)
        {
            var service=EmberServiceLocator.TryResolve<ITalismanPracticeService>();
            if(service == null) { context.Fail("D 练习服务尚未初始化。"); return; }
            var run=new Run {Service=service,Id=context.ExecutionId}; context.State=run;
            context.SetReadMode(NarrativeReadMode.Manual); context.SetInputLock(run.Id,true);
            try
            {
                var stage = (PracticeStage)context.Command.IntegerOperand;
                if(!Enum.IsDefined(typeof(PracticeStage),stage))throw new InvalidOperationException("学习项目无效。");
                if(_configFingerprint != StudySettings.Fingerprint()) throw new InvalidOperationException("学习配表已变化，请重新烘焙并同步。");
                string visitVariable=StudySettings.VisitVariable(stage);
                if(!context.TryGetVariable(NovelVariableScope.Global,visitVariable,out var progress) || progress.Type!=NovelValueType.Int || progress.Int<0)
                    throw new InvalidOperationException("学习进入次数无效。");
                int visit=progress.Int==int.MaxValue?int.MaxValue:progress.Int+1;
                var config=stage==PracticeStage.Feather?FeatherDifficulty.Resolve(visit):StudySettings.Load(visit,stage);
                config.Validate();
                if(!context.SetVariable(NovelVariableScope.Global,visitVariable,new NovelValue(visit),out var countError))
                    throw new InvalidOperationException(countError);
                service.Invoke("cmh.d.talisman_practice",run.Id,config,json=>
                {
                    if(run.Settled || !context.IsAlive) return;
                    run.Settled=true;
                    try
                    {
                        var result=JsonUtility.FromJson<TalismanPracticeResult>(json);
                        if(result == null ||
                            (config.stage == PracticeStage.Differences ? result.differences != "completed" && result.differences != "timeout" : result.differences != "not_selected") ||
                            (config.stage == PracticeStage.Orders ? result.orders != "completed" && result.orders != "partial" && result.orders != "timeout" : result.orders != "not_selected") ||
                            (config.stage == PracticeStage.Feather ? result.feather != "completed" && result.feather != "timeout" : result.feather != "not_selected") ||
                            (config.stage != PracticeStage.Differences && result.differencesFound != 0) ||
                            (config.stage != PracticeStage.Orders && result.ordersCompleted != 0) ||
                            (config.stage != PracticeStage.Feather && result.holdMilliseconds != 0) ||
                            result.differencesFound < 0 || result.differencesFound > 6 ||
                            result.ordersCompleted < 0 || result.ordersCompleted > config.customerCount ||
                            result.holdMilliseconds < 0 || result.holdMilliseconds > Mathf.CeilToInt(config.holdSeconds*1000) ||
                            (config.stage==PracticeStage.Feather && result.feather=="completed" && result.holdMilliseconds < Mathf.RoundToInt(config.holdSeconds*1000)) ||
                            (config.stage==PracticeStage.Feather && config.tutorial && result.feather=="timeout"))
                            throw new InvalidOperationException("D 练习返回结果无效。");
                        if(!context.SetVariable(NovelVariableScope.Chapter,"d_talismanOutcome",new NovelValue(json),out var error))
                            throw new InvalidOperationException(error);
                        if(config.tutorial && !context.SetVariable(NovelVariableScope.Global,TutorialVariable(config.stage),new NovelValue(true),out error))
                            throw new InvalidOperationException(error);
                        context.SetInputLock(run.Id,false); context.Complete();
                    }
                    catch(Exception e) { context.SetInputLock(run.Id,false); context.Fail(e.Message); }
                },error=>
                {
                    if(run.Settled || !context.IsAlive) return;
                    run.Settled=true; context.SetInputLock(run.Id,false); context.Fail(error);
                });
            }
            catch(Exception e) { Release(context); context.Fail(e.Message); }
        }
        private static void Release(NovelCustomStepContext context)
        {
            if(context.State is Run run)
            {
                run.Settled=true;
                try { run.Service.Abort(run.Id); }
                finally { context.SetInputLock(run.Id,false); }
            }
        }
        public override void OnEnd(NovelCustomStepContext context) => Release(context);
        public override void OnCancel(NovelCustomStepContext context) => Release(context);
    }
}
