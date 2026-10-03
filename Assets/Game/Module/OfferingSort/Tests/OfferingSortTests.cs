using System;
using System.Linq;
using System.Reflection;
using Ember.Core;
using Game.Table;
using Game.Narrative;
using Game.CMH.Rewards;
using NUnit.Framework;
using UnityEngine;
namespace Game.OfferingSort.Tests
{
    public sealed class OfferingSortTests
    {
        private static OfferingSortConfigRow Config(int count=6,int limit=0,bool first=true,bool last=true,bool hint=true)
            =>new("test",1,0,count,48,112,limit,first,last,hint,4,15,30,"fast","medium","slow","timeout","hint");
        private static void Solve(OfferingSortGame game)
        {for(int i=0;i<game.Order.Count&&!game.Settled;i++){int at=game.Order.ToList().IndexOf(i);if(at!=i)game.Swap(i,at);}}
        [Test] public void FirstRoundAnchorsAndShuffleAreConfigurationDriven()
        {for(int seed=0;seed<100;seed++){var game=new OfferingSortGame(Config(),seed);Assert.AreEqual(0,game.Order[0]);Assert.AreEqual(5,game.Order[5]);Assert.Greater(game.Inversions(),0);CollectionAssert.AreEquivalent(Enumerable.Range(0,6),game.Order);}}
        [Test] public void OpeningCannotBeSolvedByOneExchange()
        {
            for(int seed=0;seed<1000;seed++)
                for(int from=0;from<6;from++)for(int to=from+1;to<6;to++)
                {
                    var game=new OfferingSortGame(Config(),seed);game.Swap(from,to);
                    Assert.IsFalse(game.Settled,"seed="+seed+" exchange="+from+","+to);
                    Assert.Greater(game.Inversions(),0);
                }
        }
        [Test] public void PartialSortNeverSettlesBeforeAllItemsAreOrdered()
        {
            for(int seed=0;seed<100;seed++)
            {
                var game=new OfferingSortGame(Config(),seed);
                for(int i=0;i<game.Order.Count;i++)
                {
                    int at=game.Order.ToList().IndexOf(i);if(at!=i)game.Swap(i,at);
                    Assert.AreEqual(game.Order.SequenceEqual(Enumerable.Range(0,6)),game.Settled);
                }
                Assert.AreEqual("success",game.Result.Outcome);
            }
        }
        [Test] public void ZeroTimeLimitNeverTimesOut()
        {var game=new OfferingSortGame(Config(),7);game.Tick(3600);Assert.IsFalse(game.Settled);Solve(game);Assert.AreEqual("success",game.Result.Outcome);Assert.AreEqual("slow",game.Result.RewardId);}
        [TestCase(15,"fast")][TestCase(15.001,"medium")][TestCase(30,"medium")][TestCase(30.001,"slow")]
        public void CompletionTimeChoosesConfiguredReward(double time,string reward)
        {var game=new OfferingSortGame(Config(),3);game.Tick(time);Solve(game);Assert.AreEqual(reward,game.Result.RewardId);}
        [Test] public void DeadlineSettlesOnceWithConfiguredFloorReward()
        {var game=new OfferingSortGame(Config(limit:45),3);game.Tick(44.999);Assert.IsFalse(game.Settled);game.Tick(.0011);Assert.AreEqual("timeout",game.Result.Outcome);Assert.AreEqual("timeout",game.Result.RewardId);var result=game.Result;Assert.IsFalse(game.Swap(1,2));game.Tick(10);Assert.AreSame(result,game.Result);}
        [Test] public void CountAndBothEndpointRulesCanChangeWithoutCode()
        {var game=new OfferingSortGame(Config(count:8,first:false,last:false),3);Assert.AreEqual(8,game.Order.Count);Solve(game);Assert.AreEqual("success",game.Result.Outcome);Assert.Throws<ArgumentException>(()=>new OfferingSortGame(Config(count:3),0));}
        [Test] public void InvalidDropsDoNotCountAndImprovingPlayDoesNotHint()
        {var game=new OfferingSortGame(Config(),3);Assert.IsFalse(game.Swap(-1,2));Assert.IsFalse(game.Swap(1,1));Assert.AreEqual(0,game.Moves);Solve(game);Assert.IsFalse(game.HintVisible);}
        [Test] public void FourNonImprovingExchangesTriggerTutorial()
        {
            OfferingSortGame game=null;
            for(int seed=0;seed<100;seed++){var candidate=new OfferingSortGame(Config(count:8,first:false,last:false),seed);if(candidate.Inversions()<=20){game=candidate;break;}}
            Assert.IsNotNull(game);
            for(int move=0;move<4;move++)
            {bool swapped=false;for(int i=0;i<game.Order.Count&&!swapped;i++)for(int j=i+1;j<game.Order.Count&&!swapped;j++)if(game.Order[i]<game.Order[j]){game.Swap(i,j);swapped=true;}Assert.IsTrue(swapped);}
            Assert.IsTrue(game.HintVisible);
        }
        [Test] public void RepeatedAimlessSwapsEventuallyHint()
        {
            var game=new OfferingSortGame(Config(count:8,first:false,last:false),3);
            for(int i=0;i<8;i++)game.Swap(0,1);
            Assert.IsFalse(game.Settled);Assert.IsTrue(game.HintVisible);
        }
        private sealed class Presenter : IOfferingSortPresenter
        {
            public Action<int,int> Swap;public Action Ready;public int Closes;public int Opens;public bool DelayReady;public bool ThrowClose;
            public void Open(OfferingSortGame game,Action<int,int> swap,Action ready,Action<string> failure){Opens++;Swap=swap;Ready=ready;if(!DelayReady)ready();}
            public string ShownOutcome;
            public void Refresh(OfferingSortGame game){ShownOutcome=game.Result?.Outcome;}
            public void Close(){Closes++;if(ThrowClose)throw new InvalidOperationException("injected close failure");}
        }
        [Test] public void AbortDuringTimeoutNoticeDoesNotAdvanceStory()
        {
            var module=new OfferingSortModule();var view=new Presenter();EmberServiceLocator.Register<IOfferingSortPresenter>(view);int completes=0;
            try
            {
                module.OnInit();module.Invoke("cmh.a001.offering_sort","test","{\"Visit\":2}",_=>completes++,Assert.Fail);
                typeof(OfferingSortModule).GetField("_lastTime",BindingFlags.Instance|BindingFlags.NonPublic).SetValue(module,Time.realtimeSinceStartupAsDouble-module.CurrentGame.Config.TimeLimitSeconds-1);
                module.Update();Assert.AreEqual("timeout",view.ShownOutcome);
                module.Abort("test");module.Update();view.Swap(1,2);
                Assert.AreEqual(0,completes);Assert.AreEqual(1,view.Closes);Assert.IsNull(module.CurrentGame);
            }
            finally{((IEmberModule)module).OnDestroy();EmberServiceLocator.Unregister<IOfferingSortPresenter>();}
        }
        [Test] public void ModuleAbortIgnoresLateReadyAndNeverPays()
        {
            var module=new OfferingSortModule();var view=new Presenter{DelayReady=true};EmberServiceLocator.Register<IOfferingSortPresenter>(view);int completes=0,failures=0;
            try{module.OnInit();module.Invoke("cmh.a001.offering_sort","test","{\"Visit\":1}",_=>completes++,_=>failures++);Assert.IsNotNull(module.CurrentGame);module.Abort("test");view.Ready();module.Update();view.Swap(1,2);Assert.IsNull(module.CurrentGame);Assert.AreEqual(0,completes);Assert.AreEqual(0,failures);Assert.AreEqual(1,view.Closes);}
            finally{((IEmberModule)module).OnDestroy();EmberServiceLocator.Unregister<IOfferingSortPresenter>();}
        }
        [TestCase(false)][TestCase(true)] public void ModuleClockTimesOutAndReturnsExactlyOnce(bool throwClose)
        {
            var module=new OfferingSortModule();var view=new Presenter{ThrowClose=throwClose};EmberServiceLocator.Register<IOfferingSortPresenter>(view);int completes=0;string result=null;
            if(throwClose)UnityEngine.TestTools.LogAssert.Expect(LogType.Error,new System.Text.RegularExpressions.Regex(".*小游戏UI清理失败.*"));
            try
            {
                module.OnInit();module.Invoke("cmh.a001.offering_sort","test","{\"Visit\":2}",r=>{completes++;result=r;},e=>Assert.Fail(e));
                typeof(OfferingSortModule).GetField("_lastTime",BindingFlags.Instance|BindingFlags.NonPublic).SetValue(module,Time.realtimeSinceStartupAsDouble-module.CurrentGame.Config.TimeLimitSeconds-1);
                module.Update();module.Update();view.Swap(1,2);
                Assert.AreEqual("timeout",view.ShownOutcome);Assert.IsNotNull(module.CurrentGame);Assert.AreEqual(0,completes);Assert.AreEqual(0,view.Closes);
                typeof(OfferingSortModule).GetField("_completionShownAt",BindingFlags.Instance|BindingFlags.NonPublic).SetValue(module,Time.realtimeSinceStartupAsDouble-module.CurrentGame.Config.TimeoutResultSeconds*.5);
                module.Update();Assert.AreEqual(0,completes,"Keep timeout notice for the full two seconds");
                typeof(OfferingSortModule).GetField("_completionShownAt",BindingFlags.Instance|BindingFlags.NonPublic).SetValue(module,Time.realtimeSinceStartupAsDouble-module.CurrentGame.Config.TimeoutResultSeconds-1);
                module.Update();module.Update();view.Swap(1,2);
                Assert.AreEqual(1,completes);Assert.AreEqual("cmh_box_timeout",JsonUtility.FromJson<OfferingSortResult>(result).RewardId);Assert.AreEqual(1,view.Closes);
            }
            finally{((IEmberModule)module).OnDestroy();EmberServiceLocator.Unregister<IOfferingSortPresenter>();}
        }
        [Test] public void PreviewDoesNotCountTimeOrAcceptMovesAndStartKeepsSameBoard()
        {
            var module=new OfferingSortModule();var view=new Presenter();EmberServiceLocator.Register<IOfferingSortPresenter>(view);int previews=0;
            try
            {
                module.OnInit();module.Invoke("cmh.a001.offering_sort.preview","preview","{\"Visit\":2}",_=>previews++,e=>Assert.Fail(e));
                var game=module.CurrentGame;Assert.IsNotNull(game);Assert.AreEqual(1,previews);Assert.IsFalse(game.IsInteractive);
                var order=game.Order.ToArray();view.Swap(0,1);module.Update();game.Tick(100);Assert.AreEqual(0,game.Elapsed);Assert.AreEqual(0,game.Moves);
                module.Invoke("cmh.a001.offering_sort","start","{\"Visit\":2}",_=>{},e=>Assert.Fail(e));
                Assert.AreSame(game,module.CurrentGame);Assert.IsTrue(game.IsInteractive);CollectionAssert.AreEqual(order,game.Order);Assert.AreEqual(1,view.Opens);
                module.Abort("preview");Assert.AreSame(game,module.CurrentGame);module.Abort("start");Assert.IsNull(module.CurrentGame);Assert.AreEqual(1,view.Closes);
            }
            finally{((IEmberModule)module).OnDestroy();EmberServiceLocator.Unregister<IOfferingSortPresenter>();}
        }
        [TestCase("Morning","上午",1)]
        [TestCase("Afternoon","下午",1)]
        [TestCase("Evening","傍晚",1)]
        [TestCase("Town","第5天 · 进镇",1)]
        [TestCase("NextMonth","上午",2)]
        public void ScheduleEntryShowsTimeCardBeforeChoices(string entry,string label,int month)
        {
            NovelRewardService.LoadBakedForEditor();
            using var engine=new Ember.Table.EmberTableEngine();var catalog=Game.Table.Generated.GameTables.CreateCatalog();var bytes=new System.Collections.Generic.Dictionary<string,byte[]>();foreach(var e in catalog.Entries)bytes[e.TableId]=Resources.Load<TextAsset>(e.ResourcePath).bytes;Assert.IsTrue(engine.Load(catalog,bytes).Succeeded);var tables=new NarrativeTableCatalog(engine.Database);
            var story=Resources.Load<NarrativeStorySO>("Config/Narrative/CallMeHeartless/Story_CallMeHeartless");var chapter=story.Chapters.Single(c=>c.ChapterId=="a9dbb0cd472a4c99a5a1d1ece01bcaa7");
            using var session=new NovelSession(new NovelNewGameRequest(chapterId:chapter.ChapterId,nodeId:chapter.Nodes.Single(n=>n.name=="CH03_Chapter02_"+entry+"_Receive").NodeId),()=>tables,new StoryResources{Story=story});var view=new View();session.AttachView(view);bool saw=false;long frame=0;
            for(int i=0;i<50&&session.Snapshot.State!=NarrativeState.AwaitingChoice;i++)
            {
                session.Tick(10,++frame);
                if(view.LastCommand?.TextMode==NovelTextMode.Title)
                {
                    string text=view.LastCommand.Text;
                    Assert.AreEqual(month+"月 · "+label,text);saw=true;
                }
                session.Advance(++frame);
            }
            Assert.IsTrue(saw,"Missing time card at "+entry);Assert.AreEqual(NarrativeState.AwaitingChoice,session.Snapshot.State,session.Snapshot.Error?.ToString());
        }
        private sealed class Lease<T> : INovelAssetLease<T> where T:UnityEngine.Object
        {public bool IsDone=>true;public T Asset{get;set;}public string Error=>null;public void Dispose(){}}
        private sealed class StoryResources : INovelResources
        {public NarrativeStorySO Story;public INovelAssetLease<T> Load<T>(string path)where T:UnityEngine.Object=>new Lease<T>{Asset=typeof(T)==typeof(NarrativeStorySO)?Story as T:Resources.Load<T>(path)};}
        private sealed class View : INovelView
        {public NovelCommand LastCommand;public string Status;public int TextLength=>10;public void Render(NarrativeSnapshot s,NovelCommand c,string speaker,int visible,string status){Status=status;LastCommand=c;}public void Visual(NovelCommand c,Sprite sprite,float progress){}public void ClearVisuals(){}}
        [Test] public void RealA001LocksSessionCompletesAndAccumulatesMonthlyReward()
        {
            var module=new OfferingSortModule();var view=new Presenter();EmberServiceLocator.Register<IOfferingSortPresenter>(view);NovelRewardService.LoadBakedForEditor();
            using var engine=new Ember.Table.EmberTableEngine();var catalog=Game.Table.Generated.GameTables.CreateCatalog();var bytes=new System.Collections.Generic.Dictionary<string,byte[]>();foreach(var e in catalog.Entries)bytes[e.TableId]=Resources.Load<TextAsset>(e.ResourcePath).bytes;Assert.IsTrue(engine.Load(catalog,bytes).Succeeded);var tables=new NarrativeTableCatalog(engine.Database);
            var original=Resources.Load<NarrativeStorySO>("Config/Narrative/CallMeHeartless/Story_CallMeHeartless");var story=UnityEngine.Object.Instantiate(original);
            try
            {
                module.OnInit();var chapter=story.Chapters.Single(c=>c.ChapterId=="a9dbb0cd472a4c99a5a1d1ece01bcaa7");
                // Enter through the real parent call; seed first-event data so visit two selects A001.
                var data=new UnityEditor.SerializedObject(story);var globals=data.FindProperty("_globals");for(int i=0;i<globals.arraySize;i++){var r=globals.GetArrayElementAtIndex(i);string id=r.FindPropertyRelative("_id").stringValue;if(id=="ch2_aVisits"||id=="ch2_aFirstEvent")r.FindPropertyRelative("_value").FindPropertyRelative("_int").intValue=id=="ch2_aVisits"?1:2;}data.ApplyModifiedPropertiesWithoutUndo();
                using var session=new NovelSession(new NovelNewGameRequest(chapterId:chapter.ChapterId,nodeId:chapter.Nodes.Single(n=>n.name.EndsWith("_A_Call")).NodeId),()=>tables,new StoryResources{Story=story});var reader=new View();session.AttachView(reader);long frame=0;
                bool sawPreview=false;
                for(int i=0;i<40&&(module.CurrentGame==null||!module.CurrentGame.IsInteractive);i++)
                {
                    session.Tick(10,++frame);
                    if(module.CurrentGame!=null && !module.CurrentGame.IsInteractive && session.Snapshot.CommandId=="46f2ce58622845a197d828606bd6a9ab")
                    {
                        sawPreview=true;Assert.IsFalse(session.IsInputLocked);Assert.IsEmpty(session.Snapshot.PauseReasons);
                        Assert.AreEqual(0,session.Snapshot.GlobalVariables["ch2_aBoxVisits"].Int);Assert.AreEqual(0,module.CurrentGame.Elapsed);
                    }
                    session.Advance(++frame);
                }
                Assert.IsTrue(sawPreview,"The cute-offerings line must show the board before gameplay starts");
                Assert.IsNotNull(module.CurrentGame,session.Snapshot.Error?.ToString());Assert.AreEqual(0,module.CurrentGame.Config.TimeLimitSeconds);Assert.IsTrue(session.IsInputLocked);Assert.IsNotEmpty(session.Snapshot.PauseReasons);Assert.IsFalse(session.TryCapture(out _,out _));Assert.AreEqual(1,session.Snapshot.GlobalVariables["ch2_aBoxVisits"].Int);
                session.Tick(0,++frame);Assert.AreEqual("",reader.Status,"An active minigame is not a user pause");
                using(session.AcquirePause("Settings")){session.Tick(0,++frame);Assert.IsNotEmpty(reader.Status,"A real popup pause remains visible");}
                var game=module.CurrentGame;for(int i=0;i<game.Order.Count&&!game.Settled;i++){int at=game.Order.ToList().IndexOf(i);if(i!=at)view.Swap(i,at);}
                Assert.IsNotNull(module.CurrentGame,"Show the fully sorted board before closing");
                typeof(OfferingSortModule).GetField("_completionShownAt",BindingFlags.Instance|BindingFlags.NonPublic).SetValue(module,Time.realtimeSinceStartupAsDouble-2);module.Update();
                Assert.IsNull(module.CurrentGame);Assert.IsFalse(session.IsInputLocked);Assert.IsEmpty(session.Snapshot.PauseReasons);Assert.AreEqual(30,session.Snapshot.GlobalVariables["ch2_aBoxMonthlyBonus"].Int);Assert.AreEqual(25,session.Snapshot.GlobalVariables["player_money"].Int);
                for(int i=0;i<20&&session.Snapshot.State!=NarrativeState.AwaitingAdvance;i++)session.Tick(10,++frame);
                Assert.IsTrue(session.TryCapture(out var save,out var error),error);Assert.AreEqual(1,save.Globals.Single(v=>v.Id=="ch2_aBoxVisits").Value.Int);
                bool sawAfternoonCard=false;
                for(int i=0;i<40&&session.Snapshot.State!=NarrativeState.AwaitingChoice;i++)
                {
                    session.Tick(10,++frame);
                    if(reader.LastCommand?.Text=="1月 · 下午" && reader.LastCommand.TextMode==NovelTextMode.Title)sawAfternoonCard=true;
                    session.Advance(++frame);
                }
                Assert.IsTrue(sawAfternoonCard,"Show the afternoon chapter card before its choices");
                Assert.AreEqual("9ae06dbb25974227b5415b385ace6293",session.Snapshot.NodeId,"Morning A1 returns to the afternoon schedule");
                Assert.AreEqual("下午，主控能做的事：",session.ChoicePrompt);
                Assert.AreEqual(1,session.Snapshot.Options.Count,"Default variables do not unlock Yan's visit");
                Assert.AreEqual("1b849bfd2c75429da798ee173facdbc5",session.Snapshot.Options[0].Id);

            }
            finally{UnityEngine.Object.DestroyImmediate(story);((IEmberModule)module).OnDestroy();EmberServiceLocator.Unregister<IOfferingSortPresenter>();}
        }
    }
}

