using System;
using Ember.Core;
using Game.Narrative;
using UnityEngine;

namespace Game.ShopSelection
{
    [CreateAssetMenu(menuName = "Call Me Heartless/饮品图片选择步骤")]
    public sealed class ShopSelectionStepSO : NovelCustomStepSO
    {
        [SerializeField,HideInInspector] private string _configFingerprint;
        public override string ScriptId => "Game.ShopSelection.DrinkSelection";
        public override string DisplayName => "选择饮品图片";
        public override string Summary() => "Image 占位 · 悬浮高亮 · 按余额选择饮品";
        private sealed class Run { public IShopSelectionService Service; public string Id; public bool Settled; }
        public override string Validate(NovelCustomStepValidation validation)
        {
            if(!validation.TryGetVariable(NovelVariableScope.Global,ShopSelectionSettings.VisitVariable,out var visits) || visits.Type!=NovelValueType.Int)
                return "购物进入次数未声明。";
            if (!validation.TryGetVariable(NovelVariableScope.Global, "player_money", out var money) || money.Type != NovelValueType.Int)
                return "购物余额变量缺失或类型错误";
            if (!validation.TryGetVariable(NovelVariableScope.Chapter, "h_drinkChoice", out var result) || result.Type != NovelValueType.String)
                return "购物结果变量缺失或类型错误";
            if(!validation.TryGetVariable(NovelVariableScope.Chapter,ShopSelectionSettings.PriceVariable,out var price) || price.Type!=NovelValueType.Int)
                return "饮品成交价变量未声明。";
            try { ShopSelectionSettings.Load(0); if(_configFingerprint!=ShopSelectionSettings.Fingerprint())return "饮品配表指纹未同步。"; }
            catch (Exception e) { return e.Message; }
            return null;
        }
        public override void OnBegin(NovelCustomStepContext context)
        {
            var service = EmberServiceLocator.TryResolve<IShopSelectionService>();
            if (service == null) { context.Fail("购物模块尚未初始化"); return; }
            if (!context.TryGetVariable(NovelVariableScope.Global, "player_money", out var money) || money.Type != NovelValueType.Int || money.Int < 0)
            { context.Fail("购物余额无效"); return; }
            var run = new Run { Service = service, Id = context.ExecutionId }; context.State = run;
            context.SetReadMode(NarrativeReadMode.Manual); context.SetInputLock(run.Id, true);
            try
            {
                if(_configFingerprint!=ShopSelectionSettings.Fingerprint())throw new InvalidOperationException("饮品配表已变化，请重新烘焙并同步。");
                if(!context.TryGetVariable(NovelVariableScope.Global,ShopSelectionSettings.VisitVariable,out var visits) || visits.Type!=NovelValueType.Int || visits.Int<0)
                    throw new InvalidOperationException("购物进入次数无效。");
                int visit=visits.Int==int.MaxValue?int.MaxValue:visits.Int+1;
                var request=ShopSelectionSettings.Load(money.Int,visit);
                if(!context.SetVariable(NovelVariableScope.Global,ShopSelectionSettings.VisitVariable,new NovelValue(visit),out var countError))throw new InvalidOperationException(countError);
                service.Invoke("cmh.h002.drink_selection", run.Id,
                    request, result =>
                    {
                        if (run.Settled || !context.IsAlive) return;
                        run.Settled = true;
                        var product=Array.Find(request.Products,p=>p.id==result);
                        bool valid = result == "cancel" || (product!=null &&
                            context.TryGetVariable(NovelVariableScope.Global,"player_money",out var currentMoney) && currentMoney.Int >= product.price);
                        if (!valid) { context.SetInputLock(run.Id, false); context.Fail("购物返回了无效商品"); return; }
                        if (!context.SetVariable(NovelVariableScope.Chapter,ShopSelectionSettings.PriceVariable,new NovelValue(product?.price ?? 0),out var error) ||
                            !context.SetVariable(NovelVariableScope.Chapter, "h_drinkChoice", new NovelValue(result), out error))
                        { context.SetInputLock(run.Id, false); context.Fail(error); return; }
                        context.SetInputLock(run.Id, false); context.Complete();
                    }, error => { if (!run.Settled && context.IsAlive) { run.Settled = true; context.SetInputLock(run.Id, false); context.Fail(error); } });
            }
            catch (Exception e) { service.Abort(run.Id); context.SetInputLock(run.Id, false); context.Fail(e.Message); }
        }
        private static void Release(NovelCustomStepContext context)
        {
            if (context.State is Run run) { run.Settled = true; run.Service.Abort(run.Id); context.SetInputLock(run.Id, false); }
        }
        public override void OnEnd(NovelCustomStepContext context) => Release(context);
        public override void OnCancel(NovelCustomStepContext context) => Release(context);
    }
}
