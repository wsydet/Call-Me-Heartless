using System;
using System.Collections.Generic;
using System.Linq;
using Ember.Core;
using Ember.Table;
using Ember.UI;
using Ember.UIExtension;
using Game.Narrative;
using Game.Table.Generated;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
namespace Game.TalismanPractice.Tests
{
    public sealed class TalismanPracticeTests
    {
        private sealed class GardenService : INovelStepService
        {
            public global::Game.GardenCare.GardenCareConfig Config;
            public Action<string> Complete;
            public void Invoke(string key,string id,string payload,Action<string> complete,Action<string> fail)
            { Config=JsonUtility.FromJson<global::Game.GardenCare.GardenCareConfig>(payload); Complete=complete; }
            public void Abort(string id) { }
        }
        private sealed class ShopService : global::Game.ShopSelection.IShopSelectionService
        {
            public global::Game.ShopSelection.ShopSelectionRequest Request;
            public Action<string> Complete;
            public void Invoke(string key,string id,global::Game.ShopSelection.ShopSelectionRequest request,Action<string> complete,Action<string> fail)
            { Request=request; Complete=complete; }
            public void Abort(string id) { }
        }
        [TestCase(0,0,false)][TestCase(0,4,false)][TestCase(1,0,false)][TestCase(1,2,true)]
        [TestCase(2,0,false)][TestCase(2,4,false)][TestCase(2,5,true)]
        public void ActualGardenAndShopCountOnEntryAndIgnoreLateCompletion(int kind,int previousVisits,bool cancel)
        {
            using var engine=new EmberTableEngine();var catalog=GameTables.CreateCatalog();
            var bytes=catalog.Entries.ToDictionary(e=>e.TableId,e=>Resources.Load<TextAsset>(e.ResourcePath).bytes);
            Assert.IsTrue(engine.Load(catalog,bytes).Succeeded);
            global::Game.CMH.Rewards.NovelRewardService.Install(engine.Database);
            var tables=new NarrativeTableCatalog(engine.Database);
            var story=UnityEngine.Object.Instantiate(Resources.Load<NarrativeStorySO>("Config/Narrative/CallMeHeartless/Story_CallMeHeartless"));
            string counter=kind==2?global::Game.ShopSelection.ShopSelectionSettings.VisitVariable:global::Game.GardenCare.GardenCareSettings.VisitVariable(kind);
            var so=new SerializedObject(story);var globals=so.FindProperty("_globals");
            for(int i=0;i<globals.arraySize;i++)
            {
                var v=globals.GetArrayElementAtIndex(i);string id=v.FindPropertyRelative("_id").stringValue;
                if(id==counter || id=="player_money")v.FindPropertyRelative("_value").FindPropertyRelative("_int").intValue=id==counter?previousVisits:1000;
            }
            so.ApplyModifiedPropertiesWithoutUndo();
            var garden=new GardenService();var shop=new ShopService();
            var oldGarden=EmberServiceLocator.TryResolve<INovelStepService>();
            var oldShop=EmberServiceLocator.TryResolve<global::Game.ShopSelection.IShopSelectionService>();
            EmberServiceLocator.Unregister<INovelStepService>();EmberServiceLocator.Unregister<global::Game.ShopSelection.IShopSelectionService>();
            try
            {
                EmberServiceLocator.Register<INovelStepService>(garden);EmberServiceLocator.Register<global::Game.ShopSelection.IShopSelectionService>(shop);
                var chapter=story.Chapters.Single(c=>c.ChapterId=="a9dbb0cd472a4c99a5a1d1ece01bcaa7");
                string name=kind==2?"CH03_Chapter02_H002_Intro":"CH03_Chapter02_B00"+(kind+1);
                var node=chapter.Nodes.Single(n=>n.name==name);
                Assert.IsTrue(story.TryReadDefinition(tables,out var definition,out var errors),string.Join("\n",errors));
                var target=global::Game.Narrative.Editor.NovelTestSaveBuilder.FindTargets(definition,chapter.ChapterId,node.NodeId).Single();
                var chain=global::Game.Narrative.Editor.NovelTestSaveBuilder.CallChains(target.Chapter,target.Node).Single();
                var settings=global::Game.Narrative.Editor.NovelTestSaveBuilder.Defaults(definition,target,"Config/Narrative/CallMeHeartless/Story_CallMeHeartless",chain);
                var checkpoint=global::Game.Narrative.Editor.NovelTestSaveBuilder.Build(definition,tables,settings);
                using var session=new NovelSession(checkpoint,()=>tables,new Assets{Story=story});
                long frame=0;for(int i=0;i<5 && !session.RestoreReady;i++)session.Tick(0,++frame);
                Assert.IsTrue(session.RestoreReady,session.Snapshot.Error?.ToString());session.CommitRestore(new View());
                for(int i=0;i<30 && garden.Config==null && shop.Request==null;i++){session.Tick(1,++frame);session.Advance(++frame);}
                Assert.AreNotEqual(NarrativeState.Faulted,session.Snapshot.State,session.Snapshot.Error?.ToString());
                Assert.AreEqual(previousVisits+1,session.Snapshot.GlobalVariables[counter].Int);
                foreach(string other in new[]{"ch2_gardenVisits_0","ch2_gardenVisits_1","ch2_shopVisits","ch2_featherVisits","ch2_studyVisits_0","ch2_studyVisits_1"})
                    if(other!=counter)Assert.AreEqual(0,session.Snapshot.GlobalVariables[other].Int,"Unrelated node counter: "+other);
                Action<string> complete=kind==2?shop.Complete:garden.Complete;
                Assert.IsNotNull(complete);
                if(kind<2)
                {
                    var expected=global::Game.GardenCare.GardenCareSettings.Load(previousVisits+1);
                    Assert.AreEqual(expected.weedCount,garden.Config.weedCount);Assert.AreEqual(previousVisits==0,garden.Config.tutorial);
                }
                else
                {
                    Assert.AreEqual(global::Game.ShopSelection.ShopSelectionSettings.Load(1000,previousVisits+1).TimeoutSeconds,shop.Request.TimeoutSeconds);
                    if(previousVisits==0){var model=new global::Game.ShopSelection.ShopSelectionModel(shop.Request);model.Tick(3600,false);Assert.IsFalse(model.Settled);}
                }
                if(cancel){session.Dispose();complete(kind==2?"water":"completed");return;}
                int balance=session.Snapshot.GlobalVariables["player_money"].Int;
                complete(kind==2?"water":"completed");
                session.Tick(0,++frame);
                Assert.AreEqual(previousVisits+1,session.Snapshot.GlobalVariables[counter].Int);
                if(kind==2)
                {
                    Assert.AreEqual(shop.Request.Products.Single(p=>p.id=="water").price,session.Snapshot.Variables["h_drinkPrice"].Int);
                    Assert.AreEqual(balance-shop.Request.Products.Single(p=>p.id=="water").price,session.Snapshot.GlobalVariables["player_money"].Int);
                }
                complete(kind==2?"tea":"completed");
                Assert.AreEqual(previousVisits+1,session.Snapshot.GlobalVariables[counter].Int);
                if(kind==2)Assert.AreEqual(balance-shop.Request.Products.Single(p=>p.id=="water").price,session.Snapshot.GlobalVariables["player_money"].Int);
            }
            finally
            {
                EmberServiceLocator.Unregister<INovelStepService>();EmberServiceLocator.Unregister<global::Game.ShopSelection.IShopSelectionService>();
                if(oldGarden!=null)EmberServiceLocator.Register<INovelStepService>(oldGarden);
                if(oldShop!=null)EmberServiceLocator.Register<global::Game.ShopSelection.IShopSelectionService>(oldShop);
                UnityEngine.Object.DestroyImmediate(story);
            }
        }
        private static TalismanPracticeGame Game(PracticeStage stage=PracticeStage.Differences, bool tutorial=false)
            => new TalismanPracticeGame(new TalismanPracticeConfig{stage=stage,tutorial=tutorial});
        private static void Assemble(TalismanPracticeGame g,int pattern)
        {for(int layer=0;layer<3;layer++)g.SelectPart(layer,TalismanPatterns.PartFor(pattern,layer));}
        [Test] public void DifferencesRequireTwoDistinctHitsInEachOfThreeRounds()
        {
            var g=Game();Assert.AreEqual(PracticePhase.Playing,g.Phase);
            g.Start();g.FindDifference(-1);g.FindDifference(0);g.FindDifference(0);Assert.AreEqual(1,g.Result.differencesFound);
            g.FindDifference(1);Assert.AreEqual(1,g.Round);
            for(int i=0;i<2;i++){g.FindDifference(0);g.FindDifference(1);}
            Assert.AreEqual(6,g.Result.differencesFound);Assert.AreEqual(PracticePhase.Review,g.Phase);
            g.FindDifference(1);Assert.AreEqual(6,g.Result.differencesFound);
        }
        [TestCase(0)][TestCase(1)][TestCase(2)]
        public void SharedCorrectPatternIsAccepted(int pattern)
        {
            var g=Game(PracticeStage.Orders);
            for(int i=0;i<pattern;i++){Assemble(g,g.Pattern);g.Submit();}
            Assert.AreEqual(pattern,g.Pattern);Assemble(g,pattern);Assert.IsTrue(g.Submit());
        }
        [Test] public void IncompleteWrongAndChangedPartsAreHandledWithoutDrawing()
        {
            var g=Game(PracticeStage.Orders);Assert.IsFalse(g.Submit());
            g.SelectPart(0,0);g.SelectPart(1,0);Assert.IsFalse(g.Submit());
            g.SelectPart(2,2);Assert.IsFalse(g.Submit());Assert.AreEqual(0,g.Customer);
            g.SelectPart(2,0);Assert.IsTrue(g.Submit());Assert.AreEqual(1,g.Result.ordersCompleted);
            Assert.IsTrue(g.SelectedParts.All(p=>p==-1),"New orders clear all three selections");
            Assemble(g,2);Assert.IsFalse(g.Submit());g.Clear();
            Assert.IsTrue(g.SelectedParts.All(p=>p==-1));
            Assemble(g,g.Pattern);Assert.IsTrue(g.Submit());
            g.SelectPart(-1,3);g.SelectPart(8,-1);Assert.IsTrue(g.SelectedParts.All(p=>p==-1));
        }
        [Test] public void PatienceExpiryAdvancesWithoutRewardAndPreservesSharedPatterns()
        {
            var g=Game(PracticeStage.Orders);g.Tick(25.1f,false);
            Assert.AreEqual(1,g.Customer);Assert.AreEqual(0,g.Result.ordersCompleted);
            Assert.AreEqual(1,g.Pattern);Assert.IsTrue(g.SelectedParts.All(p=>p==-1));
        }
        [Test] public void PauseFreezesReadyReviewOrdersAndFeather()
        {
            var g=Game(PracticeStage.Orders);g.Tick(300,true);Assert.AreEqual(PracticePhase.Playing,g.Phase);
            g.Tick(300,true);Assert.AreEqual(25,g.Patience);Assert.AreEqual(0,g.Customer);
            for(int i=0;i<5;i++){Assemble(g,g.Pattern);g.Submit();}Assert.AreEqual(PracticePhase.Review,g.Phase);
            g.Tick(300,true);Assert.AreEqual(PracticePhase.Review,g.Phase);
            g=Game(PracticeStage.Feather);g.SetBlowing(true);float y=g.FeatherY,v=g.FeatherVelocity;
            g.Tick(300,true);Assert.AreEqual(y,g.FeatherY);Assert.AreEqual(v,g.FeatherVelocity);
        }
        [TestCase(PracticeStage.Differences)][TestCase(PracticeStage.Orders)][TestCase(PracticeStage.Feather)]
        public void TimedGameFinishesOnlyTheChosenStage(PracticeStage stage)
        {
            var g=Game(stage);g.Tick(500,false);
            Assert.AreEqual(PracticePhase.Finished,g.Phase);
            Assert.AreEqual(stage,g.Stage);
            Assert.AreEqual(stage==PracticeStage.Differences?"timeout":"not_selected",g.Result.differences);
            Assert.AreEqual(stage==PracticeStage.Orders?"partial":"not_selected",g.Result.orders);
            Assert.AreEqual(stage==PracticeStage.Feather?"timeout":"not_selected",g.Result.feather);
        }
        [TestCase(PracticeStage.Differences)][TestCase(PracticeStage.Orders)][TestCase(PracticeStage.Feather)]
        public void FirstVisitHasNoGameOrCustomerDeadline(PracticeStage stage)
        {
            var g=Game(stage,true);g.Tick(1200,false);g.Tick(1200,false);
            Assert.AreEqual(PracticePhase.Playing,g.Phase);Assert.AreEqual(0,g.Customer);Assert.AreEqual(25,g.Patience);
            Assert.AreEqual(stage,g.Stage);
        }
        [Test] public void FeatherRequiresWholeShapeInsideAndCanRecoverFromFloor()
        {
            var g=Game(PracticeStage.Feather);g.Tick(4,false);
            Assert.AreEqual(g.Config.featherHalfHeight,g.FeatherY,.0001f);
            Assert.AreEqual(0,g.HoldTime);g.SetBlowing(true);g.Tick(1,false);
            Assert.Greater(g.FeatherY,g.Config.featherHalfHeight);
            for(int i=0;i<100;i++)g.SetBlowing(true);Assert.LessOrEqual(g.FeatherVelocity,g.Config.maxRiseSpeed);
        }
        [Test] public void FeatherCanBeKeptInsideWithGentleHeldBreaths()
        {
            var g=Game(PracticeStage.Feather);
            for(int i=0;i<6000 && g.Phase==PracticePhase.Playing;i++)
            {
                if(g.FeatherY < .4f)g.SetBlowing(true);
                if(g.FeatherY > .64f)g.SetBlowing(false);
                g.Tick(.01f,false);
            }
            Assert.AreEqual("completed",g.Result.feather);Assert.AreEqual(10000,g.Result.holdMilliseconds);
            g.Next();Assert.AreEqual(PracticePhase.Finished,g.Phase);
        }
        [Test] public void FeatherForceRampsAndReleaseRemovesLiftImmediately()
        {
            var g=Game(PracticeStage.Feather,true);
            g.SetBlowing(true);Assert.AreEqual(0,g.BlowStrength);Assert.AreEqual(0,g.FeatherVelocity);
            g.Tick(.1f,false);float early=g.BlowStrength;
            Assert.Greater(early,0);Assert.Less(early,.2f);
            g.Tick(1,false);Assert.AreEqual(1,g.BlowStrength,.001f);
            Assert.LessOrEqual(g.FeatherVelocity,g.Config.maxRiseSpeed);
            float v=g.FeatherVelocity,x=g.FeatherX;
            g.SetBlowing(false);Assert.AreEqual(0,g.BlowStrength);
            g.Tick(.1f,false);Assert.Less(g.FeatherVelocity,v);Assert.AreNotEqual(x,g.FeatherX);
            g.Tick(1,false);Assert.Less(g.FeatherVelocity,0);
            Assert.GreaterOrEqual(g.FeatherVelocity,-g.Config.maxFallSpeed);
            g.SetBlowing(true);Assert.AreEqual(0,g.BlowStrength,"每次重新按下都从零开始");
        }
        [Test] public void ConfigIsCopiedAndRejectsNonfiniteParameters()
        {
            var config=new TalismanPracticeConfig();var g=new TalismanPracticeGame(config);config.customerCount=20;Assert.AreEqual(5,g.Config.customerCount);
            config.gravity=float.NaN;Assert.Throws<ArgumentException>(()=>new TalismanPracticeGame(config));
            config=new TalismanPracticeConfig{frameTop=.4f,frameBottom=.39f};Assert.Throws<ArgumentException>(()=>new TalismanPracticeGame(config));
            g.Tick(float.NaN,false);Assert.AreEqual(PracticePhase.Playing,g.Phase);
        }
        [TestCase(1)][TestCase(2)][TestCase(3)][TestCase(4)][TestCase(5)][TestCase(30)]
        public void EveryBakedFeatherDifficultyIsReachableAndTracksItsMovingFrame(int visit)
        {
            var config=FeatherDifficulty.Resolve(visit);
            Assert.AreEqual(Math.Min(visit,5),config.difficultyLevel);
            Assert.AreEqual(visit==1,config.tutorial);
            Assert.AreEqual(visit>=4,config.frameMoveAmplitudeY>0);
            var g=new TalismanPracticeGame(config);
            float frame=g.FrameBottom;g.Tick(1,true);Assert.AreEqual(frame,g.FrameBottom);
            float minFrame=g.FrameBottom,maxFrame=g.FrameBottom;
            for(int i=0;i<20000 && g.Phase==PracticePhase.Playing;i++)
            {
                float center=(g.FrameBottom+g.FrameTop)/2;
                if(g.FeatherY<center-.07f)g.SetBlowing(true);
                if(g.FeatherY>center+.06f)g.SetBlowing(false);
                g.Tick(.01f,false);
                minFrame=Mathf.Min(minFrame,g.FrameBottom);maxFrame=Mathf.Max(maxFrame,g.FrameBottom);
                Assert.GreaterOrEqual(g.FrameBottom,0);Assert.LessOrEqual(g.FrameTop,1);
                if(!g.InFrame && g.HoldTime>0)Assert.Greater(g.Result.holdMilliseconds,0,"离框保留累计进度");
            }
            Assert.AreEqual("completed",g.Result.feather,"当前表难度应能通过按住/松开控制完成");
            Assert.AreEqual(Mathf.RoundToInt(config.holdSeconds*1000),g.Result.holdMilliseconds);
            if(visit>=4)Assert.Greater(maxFrame-minFrame,.03f);else Assert.AreEqual(minFrame,maxFrame);
        }

        [Test] public void InvalidMovingFrameAndNonfiniteTableValuesAreRejected()
        {
            var c=FeatherDifficulty.Resolve(4);c.frameMoveAmplitudeY=.5f;Assert.Throws<ArgumentException>(()=>c.Validate());
            c=FeatherDifficulty.Resolve(4);c.frameWidth=float.PositiveInfinity;Assert.Throws<ArgumentException>(()=>c.Validate());
            c=FeatherDifficulty.Resolve(4);c.blowAcceleration=c.gravity;Assert.Throws<ArgumentException>(()=>c.Validate());
            Assert.Throws<ArgumentOutOfRangeException>(()=>FeatherDifficulty.Resolve(0));
        }
        private sealed class Presenter : ITalismanPracticePresenter
        {
            public Action Start,Next;public Action<int> Hit;public Action<string> Fail;public int Closed;
            public void Open(TalismanPracticeGame g,Action start,Action next,Action<int> hit,Action clear,Action submit,Action<bool> blow,Action<int,int> select,Action<string> fail)
            {Start=start;Next=next;Hit=hit;Fail=fail;}
            public void Refresh(TalismanPracticeGame g,bool paused){}
            public void Close(){Closed++;}
        }
        [Test] public void ModuleAbortsOnceIgnoresLateCallbacksAndDoesNotReplaceGardenService()
        {
            var old=EmberServiceLocator.TryResolve<ITalismanPracticePresenter>();
            var garden=EmberServiceLocator.TryResolve<INovelStepService>();
            var view=new Presenter();var module=new TalismanPracticeModule();int fail=0,complete=0;
            try
            {
                EmberServiceLocator.Register<ITalismanPracticePresenter>(view);
                module.Invoke("cmh.d.talisman_practice","old",new TalismanPracticeConfig(),_=>complete++,_=>fail++);
                var lateStart=view.Start;var lateFail=view.Fail;module.Abort("old");module.Abort("old");
                Assert.AreEqual(1,fail);Assert.AreEqual(1,view.Closed);
                module.Invoke("cmh.d.talisman_practice","new",new TalismanPracticeConfig(),_=>complete++,_=>fail++);
                lateStart();lateFail("late");Assert.AreEqual(PracticePhase.Playing,module.CurrentGame.Phase);
                module.Advance(500,true);Assert.AreEqual(PracticePhase.Playing,module.CurrentGame.Phase);
                module.Advance(500,false);module.Update();module.Update();
                Assert.AreEqual(1,complete);Assert.AreEqual(2,view.Closed);Assert.AreSame(garden,EmberServiceLocator.TryResolve<INovelStepService>());
            }
            finally{module.Abort("new");if(old!=null)EmberServiceLocator.Register<ITalismanPracticePresenter>(old);else EmberServiceLocator.Unregister<ITalismanPracticePresenter>();}
        }
        [Test] public void ModuleActivityTimeoutFailsAndCloses()
        {
            var old=EmberServiceLocator.TryResolve<ITalismanPracticePresenter>();var view=new Presenter();var module=new TalismanPracticeModule();int failures=0;
            try
            {EmberServiceLocator.Register<ITalismanPracticePresenter>(view);module.Invoke("cmh.d.talisman_practice","timeout",new TalismanPracticeConfig(),_=>Assert.Fail("No fake completion"),_=>failures++);
             module.Advance(600,false);Assert.AreEqual(1,failures);Assert.AreEqual(1,view.Closed);Assert.IsNull(module.CurrentGame);}
            finally{if(old!=null)EmberServiceLocator.Register<ITalismanPracticePresenter>(old);else EmberServiceLocator.Unregister<ITalismanPracticePresenter>();}
        }
        private sealed class Lease<T> : INovelAssetLease<T> where T:UnityEngine.Object
        {public bool IsDone=>true;public T Asset{get;set;}public string Error=>null;public void Dispose(){}}
        private sealed class Assets : INovelResources
        {public NarrativeStorySO Story; public INovelAssetLease<T> Load<T>(string path) where T:UnityEngine.Object=>new Lease<T>{Asset=typeof(T)==typeof(NarrativeStorySO)?Story as T:Resources.Load<T>(path)};}
        private sealed class View : INovelView
        {
            public int TextLength=>1;
            public void Render(NarrativeSnapshot snapshot,NovelCommand command,string speaker,int visibleCharacters,string status){}
            public void Visual(NovelCommand command,Sprite sprite,float progress){}
            public void ClearVisuals(){}
        }
        private sealed class Service : ITalismanPracticeService
        {
            public Action<string> Complete,Fail; public int Calls,Aborts; public TalismanPracticeConfig Config;
            public void Invoke(string key,string id,TalismanPracticeConfig config,Action<string> complete,Action<string> fail){Calls++;Config=config;Complete=complete;Fail=fail;}
            public void Abort(string id){Aborts++;}
        }
        [TestCase(0,false,0,false)][TestCase(1,false,0,false)][TestCase(2,false,0,false)][TestCase(0,true,0,false)]
        [TestCase(2,false,1,false)][TestCase(2,false,3,false)][TestCase(2,false,5,false)]
        [TestCase(2,false,3,true)][TestCase(2,true,2,false)]
        [TestCase(0,false,2,false)][TestCase(1,false,4,false)][TestCase(0,true,3,false)][TestCase(1,true,2,false)]
        public void ActualDStoryValidatesAndReturnsOrCancelsWithoutLateWrites(int selected,bool cancel,int previousVisits,bool timeout)
        {
            using var engine=new EmberTableEngine();var catalog=GameTables.CreateCatalog();var bytes=new Dictionary<string,byte[]>();
            foreach(var entry in catalog.Entries)bytes.Add(entry.TableId,Resources.Load<TextAsset>(entry.ResourcePath).bytes);
            Assert.IsTrue(engine.Load(catalog,bytes).Succeeded);
            global::Game.CMH.Rewards.NovelRewardService.Install(engine.Database);
            var tables=new NarrativeTableCatalog(engine.Database);
            var story=UnityEngine.Object.Instantiate(Resources.Load<NarrativeStorySO>("Config/Narrative/CallMeHeartless/Story_CallMeHeartless"));
            var storyData=new SerializedObject(story);var globals=storyData.FindProperty("_globals");
            for(int i=0;i<globals.arraySize;i++)
            {
                var v=globals.GetArrayElementAtIndex(i);
                if(v.FindPropertyRelative("_id").stringValue==StudySettings.VisitVariable((PracticeStage)selected))v.FindPropertyRelative("_value").FindPropertyRelative("_int").intValue=previousVisits;
            }
            storyData.ApplyModifiedPropertiesWithoutUndo();
            Assert.IsTrue(story.TryReadDefinition(tables,out _,out var errors),string.Join("\n",errors));
            var chapter=story.Chapters.Single(c=>c.ChapterId=="a9dbb0cd472a4c99a5a1d1ece01bcaa7");
            var node=chapter.Nodes.Single(n=>n.name=="CH03_Chapter02_D_Call");
            var service=new Service();var old=EmberServiceLocator.TryResolve<ITalismanPracticeService>();
            try
            {
                EmberServiceLocator.Register<ITalismanPracticeService>(service);
                using var session=new NovelSession(new NovelNewGameRequest(chapterId:chapter.ChapterId,nodeId:node.NodeId),()=>tables,new Assets{Story=story});
                session.AttachView(new View());long frame=0;session.Tick(0,frame);
                for(int i=0;i<10 && session.Snapshot.State!=NarrativeState.AwaitingChoice;i++){session.Tick(1,++frame);session.Advance(++frame);}
                Assert.AreEqual(0,service.Calls,"No game starts before choosing a study option");
                var choice=chapter.Nodes.OfType<NarrativeChoiceSO>().Single(n=>n.name=="CH03_Chapter02_D_StudyChoice");
                Assert.AreEqual(choice.NodeId,session.Snapshot.NodeId);Assert.AreEqual(3,choice.Options.Count);
                session.Choose(session.Snapshot.SessionGeneration,session.Snapshot.PositionVersion,choice.Options[selected].OptionId,++frame);
                session.Tick(0,++frame);
                Assert.AreEqual(1,service.Calls,session.Snapshot.State+" / "+session.Snapshot.NodeId+" / "+session.Snapshot.Error);
                Assert.AreEqual(NarrativeWait.CustomStep,session.Snapshot.Wait & NarrativeWait.CustomStep);
                Assert.AreEqual((PracticeStage)selected,service.Config.stage);Assert.AreEqual(previousVisits==0,service.Config.tutorial);
                Assert.AreEqual(Math.Min(previousVisits+1,5),service.Config.difficultyLevel);
                string result=JsonUtility.ToJson(new TalismanPracticeResult{differences=selected==0?"completed":"not_selected",orders=selected==1?"completed":"not_selected",feather=selected==2?(timeout?"timeout":"completed"):"not_selected",differencesFound=selected==0?6:0,ordersCompleted=selected==1?service.Config.customerCount:0,holdMilliseconds=selected==2?(timeout?0:Mathf.RoundToInt(service.Config.holdSeconds*1000)):0});
                Assert.AreEqual(previousVisits+1,session.Snapshot.GlobalVariables[StudySettings.VisitVariable((PracticeStage)selected)].Int);
                if(cancel){session.Dispose();service.Complete(result);Assert.GreaterOrEqual(service.Aborts,1);return;}
                service.Complete(result);
                Assert.AreEqual(result,session.Snapshot.Variables["d_talismanOutcome"].String);
                if(service.Config.tutorial)Assert.IsTrue(session.Snapshot.GlobalVariables[TalismanPracticeStepSO.TutorialVariable((PracticeStage)selected)].Bool);
                int expectedVisits=previousVisits+1;
                Assert.AreEqual(expectedVisits,session.Snapshot.GlobalVariables[StudySettings.VisitVariable((PracticeStage)selected)].Int);
                for(int i=0;i<30 && session.Snapshot.State!=NarrativeState.AwaitingChoice;i++){session.Tick(1,++frame);session.Advance(++frame);}
                Assert.AreNotEqual(NarrativeState.Faulted,session.Snapshot.State,session.Snapshot.Error?.ToString());
                Assert.AreEqual(NarrativeState.AwaitingChoice,session.Snapshot.State);
                int step=session.Snapshot.GlobalVariables["ch2_monthStep"].Int;
                service.Complete(result);service.Fail("late");
                session.Tick(1,++frame);Assert.AreEqual(step,session.Snapshot.GlobalVariables["ch2_monthStep"].Int);
                Assert.AreEqual(1,service.Calls);
                Assert.AreEqual(expectedVisits,session.Snapshot.GlobalVariables[StudySettings.VisitVariable((PracticeStage)selected)].Int,"迟到回调不可增加进入次数");
            }
            finally{if(old!=null)EmberServiceLocator.Register<ITalismanPracticeService>(old);else EmberServiceLocator.Unregister<ITalismanPracticeService>();UnityEngine.Object.DestroyImmediate(story);}
        }
        [TestCase(0, false)][TestCase(4, false)]
        [TestCase(0, true)][TestCase(4, true)]
        public void TimedOrderPrefabShowsSubmissionErrorsBeforeAndAfterHintDelay(float elapsed, bool wrongPart)
        {
            var scene=UnityEditor.SceneManagement.EditorSceneManager.NewPreviewScene();
            var root=new GameObject("Order feedback test",typeof(RectTransform),typeof(Canvas));
            UnityEngine.SceneManagement.SceneManager.MoveGameObjectToScene(root,scene);
            EUIItem item=null;
            try
            {
                var prefab=Resources.Load<GameObject>("UI/Module/TalismanPractice/Prefabs/EUITalismanPracticeItem");
                var instance=UnityEngine.Object.Instantiate(prefab,root.transform,false);
                Assert.IsTrue(EUIItemFactory.TryCreate(instance,out item,out var error),error);
                item.Show();
                var game=new TalismanPracticeGame(new TalismanPracticeConfig{stage=PracticeStage.Orders,tutorial=false});
                item.Logic.GetType().GetMethod("Configure").Invoke(item.Logic,new object[]{game,
                    (Action)(()=>game.Start()),(Action)(()=>game.Next()),(Action<int>)(i=>game.FindDifference(i)),
                    (Action)(()=>game.Clear()),(Action)(()=>game.Submit()),(Action<bool>)(held=>game.SetBlowing(held)),
                    (Action<int,int>)((layer,variant)=>game.SelectPart(layer,variant))});
                game.Tick(elapsed,false);
                if(wrongPart)game.SelectPart(0,(TalismanPatterns.PartFor(game.Pattern,0)+1)%3);
                instance.transform.Find("PracticePanel/ShopRoot/SubmitButton").GetComponent<Button>().onClick.Invoke();
                item.Logic.GetType().GetMethod("Refresh").Invoke(item.Logic,new object[]{game,false});
                var hint=(TMPro.TMP_Text)item.Logic.ControlMap["HintText"];
                Assert.AreEqual(wrongPart ? "底纹与顾客需求不符，可以直接换一个。" : "还没有选择底纹。",hint.text);
                Assert.IsTrue(hint.gameObject.activeInHierarchy);
                Assert.AreEqual(0,game.Result.ordersCompleted);
            }
            finally
            {
                item?.Dispose();UnityEngine.Object.DestroyImmediate(root);
                UnityEditor.SceneManagement.EditorSceneManager.ClosePreviewScene(scene);
            }
        }
        [TestCase(1280,800,0)][TestCase(1920,1080,0)][TestCase(800,600,0)]
        [TestCase(1280,800,1)][TestCase(1920,1080,1)][TestCase(800,600,1)]
        [TestCase(1280,800,2)][TestCase(1920,1080,2)][TestCase(800,600,2)]
        public void FormalPrefabStartsChosenGameDirectlyAndSupportsPausedInput(int width,int height,int stage)
        {
            var root=new GameObject("D UI Test",typeof(RectTransform),typeof(Canvas),typeof(CanvasScaler),typeof(GraphicRaycaster));
            var eventGo=new GameObject("D Events",typeof(EventSystem));
            var cameraGo=new GameObject("D Camera",typeof(Camera));
            var camera=cameraGo.GetComponent<Camera>();camera.orthographic=true;camera.transform.position=new Vector3(0,0,-10);
            var texture=new RenderTexture(width,height,24);camera.targetTexture=texture;
            GameObject instance=null;EUIItem item=null;
            try
            {
                root.GetComponent<Canvas>().renderMode=RenderMode.ScreenSpaceCamera;root.GetComponent<Canvas>().worldCamera=camera;
                root.GetComponent<Canvas>().planeDistance=1;
                var prefab=Resources.Load<GameObject>("UI/Module/TalismanPractice/Prefabs/EUITalismanPracticeItem");
                instance=UnityEngine.Object.Instantiate(prefab,root.transform,false);
                foreach(var graphic in instance.GetComponentsInChildren<Graphic>(true))
                    Assert.IsNotNull(graphic.GetComponent<CanvasRenderer>(),"Graphics must render: "+graphic.name);
                Assert.IsTrue(EUIItemFactory.TryCreate(instance,out item,out var error),error);item.Show();
                var g=Game((PracticeStage)stage,true);
                var configure=item.Logic.GetType().GetMethod("Configure");
                Action<int,int> select=(layer,variant)=>g.SelectPart(layer,variant);
                configure.Invoke(item.Logic,new object[]{g,(Action)(()=>g.Start()),(Action)(()=>g.Next()),(Action<int>)(i=>g.FindDifference(i)),
                    (Action)(()=>g.Clear()),(Action)(()=>g.Submit()),(Action<bool>)(held=>g.SetBlowing(held)),select});
                Canvas.ForceUpdateCanvases();camera.Render();
                void Click(string path,bool expectHit=true)
                {
                    var button=instance.transform.Find("PracticePanel/"+path).gameObject;
                    var eventData=new PointerEventData(eventGo.GetComponent<EventSystem>()){button=PointerEventData.InputButton.Left,pointerId=-1,
                        position=RectTransformUtility.WorldToScreenPoint(camera,button.transform.position)};
                    var hits=new List<RaycastResult>();root.GetComponent<GraphicRaycaster>().Raycast(eventData,hits);
                    if(expectHit){Assert.IsNotEmpty(hits,"No raycast hit: "+path);Assert.AreEqual(button,hits[0].gameObject,"Top raycast target: "+path);}
                    ExecuteEvents.Execute(button,eventData,ExecuteEvents.pointerClickHandler);
                }
                void Refresh(){item.Logic.GetType().GetMethod("Refresh").Invoke(item.Logic,new object[]{g,false});Canvas.ForceUpdateCanvases();camera.Render();}
                Refresh();
                Assert.IsFalse(instance.transform.Find("PracticePanel/StartButton").gameObject.activeSelf);
                Assert.IsFalse(instance.transform.Find("PracticePanel/NextButton").gameObject.activeSelf);
                Assert.IsFalse(instance.transform.Find("PracticePanel/TimerText").gameObject.activeSelf);
                if(stage==0)
                {
                    for(int round=0;round<3;round++)
                    {
                        Click("DifferenceRoot/LeftPaper/Difference0");Refresh();
                        Click("DifferenceRoot/RightPaper/Difference1");Refresh();
                    }
                    Assert.AreEqual(6,g.Result.differencesFound);
                    g.Tick(2,false);Assert.AreEqual(PracticePhase.Finished,g.Phase);
                }
                else if(stage==1)
                {
                    for(int layer=0;layer<3;layer++)Click("ShopRoot/PartsRoot/Part"+layer+"_"+TalismanPatterns.PartFor(g.Pattern,layer));
                    Click("ShopRoot/SubmitButton");Assert.AreEqual(1,g.Result.ordersCompleted);
                    item.Logic.GetType().GetMethod("Refresh").Invoke(item.Logic,new object[]{g,true});
                    Click("ShopRoot/PartsRoot/Part0_0");Assert.IsTrue(g.SelectedParts.All(p=>p==-1),"Paused UI cannot select a part");
                }
                else
                {
                    var button=instance.transform.Find("PracticePanel/FeatherRoot/BlowButton").gameObject;
                    var data=new PointerEventData(eventGo.GetComponent<EventSystem>()){button=PointerEventData.InputButton.Left,pointerId=-1};
                    Click("FeatherRoot/BlowButton");Assert.IsFalse(g.Blowing,"点击事件不再施加脉冲");
                    ExecuteEvents.Execute(button,data,ExecuteEvents.pointerDownHandler);Assert.IsTrue(g.Blowing);
                    g.Tick(.1f,false);Assert.Less(g.BlowStrength,.2f);
                    ExecuteEvents.Execute(button,data,ExecuteEvents.pointerUpHandler);Assert.IsFalse(g.Blowing);Assert.AreEqual(0,g.BlowStrength);
                    var input=button.GetComponents<MonoBehaviour>().Single(c=>c.GetType().Name=="TalismanBreathInput");
                    void Space(bool held)=>input.GetType().GetMethod("SampleKeyboard").Invoke(input,new object[]{held});
                    Space(true);Assert.IsTrue(g.Blowing);
                    ExecuteEvents.Execute(button,data,ExecuteEvents.pointerDownHandler);
                    Space(false);Assert.IsTrue(g.Blowing,"鼠标与空格可交替接续");
                    ExecuteEvents.Execute(button,data,ExecuteEvents.pointerExitHandler);Assert.IsFalse(g.Blowing);
                    Space(true);
                    item.Logic.GetType().GetMethod("Refresh").Invoke(item.Logic,new object[]{g,true});
                    Assert.IsFalse(g.Blowing);Space(true);Assert.IsFalse(g.Blowing);
                    Refresh();Space(true);Assert.IsFalse(g.Blowing,"暂停前按住的空格必须松开再按");
                    Space(false);Space(true);Assert.IsTrue(g.Blowing);
                    Space(false);Assert.IsFalse(g.Blowing);
                }
            }
            finally
            {item?.Dispose();if(instance)UnityEngine.Object.DestroyImmediate(instance);UnityEngine.Object.DestroyImmediate(root);UnityEngine.Object.DestroyImmediate(eventGo);
                camera.targetTexture=null;UnityEngine.Object.DestroyImmediate(cameraGo);texture.Release();UnityEngine.Object.DestroyImmediate(texture);}
        }
    }
}
