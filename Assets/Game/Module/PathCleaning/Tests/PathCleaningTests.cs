using System;
using System.Linq;
using System.Reflection;
using Ember.Core;
using Game.Table;
using Game.Narrative;
using Game.CMH.Rewards;
using NUnit.Framework;
using UnityEngine;
namespace Game.PathCleaning.Tests
{
    public sealed class PathCleaningTests
    {
        private static PathCleaningConfigRow Config(int count=6,int limit=0,bool hint=true)=>new("test",1,0,count,limit,"standard",hint,3,12,15,30,"fast","medium","slow","timeout");
        private static PathCleaningGame NewGame(int count=6,int limit=0,bool hint=true)=>new(Config(count,limit,hint),PathCleaningModule.LoadTypes(),PathCleaningModule.LoadTools(),3);
        private static PathCleaningConfigRow SweepConfig(float leaf=.25f,float mud=.22f,float length=1,float opacity=.9f)=>new("sweep",1,0,1,0,"standard",false,3,12,15,30,"fast","medium","slow","timeout",leaf,mud,length,opacity);
        private static PathCleaningGame ConfiguredMud(PathCleaningConfigRow config)
        {
            var types=PathCleaningModule.LoadTypes();var tools=PathCleaningModule.LoadTools();
            for(int seed=0;seed<20;seed++){var g=new PathCleaningGame(config,types,tools,seed);if(g.Spots[0].Type.Id=="mud")return g;}
            throw new InvalidOperationException("No mud test seed");
        }
        [TestCase(.1f)][TestCase(.25f)][TestCase(.45f)]
        public void ConfiguredLeafDistanceControlsThePerPressTravel(float distance)
        {
            var row=PathCleaningModule.LoadTypes().Single(t=>t.Id=="leaves");
            var g=new PathCleaningGame(SweepConfig(leaf:distance),new[]{row},PathCleaningModule.LoadTools(),1);
            var spot=g.Spots[0];var start=spot.Position;var direction=start.x>.5f?Vector2.left:Vector2.right;
            g.Select("broom");g.Sweep(start,start+direction*.7f,true);
            Assert.AreEqual(distance,Vector2.Distance(start,spot.Position),.0001f);
            var stopped=spot.Position;g.Sweep(stopped,stopped+direction*.7f);Assert.AreEqual(stopped,spot.Position);
            Assert.Greater(PathCleaningModule.LoadConfig()[0].LeafSweepDistance,.1f,"The shipped table increases the old travel limit");
        }
        [Test] public void MudLengthOpacityAndTravelAllComeFromConfig()
        {
            var games=new[]{ConfiguredMud(SweepConfig(length:.15f)),ConfiguredMud(SweepConfig()),ConfiguredMud(SweepConfig(opacity:.25f)),ConfiguredMud(SweepConfig(mud:.01f))};
            int n=PathCleaningGame.MudResolution;var original=Enumerable.Range(0,n*n).Select(games[0].Spots[0].IsDirtyPixel).ToArray();
            var coverage=new int[games.Length];var opacity=new float[games.Length];
            for(int g=0;g<games.Length;g++)
            {
                var game=games[g];var p=game.Spots[0].Position;game.Select("broom");game.Sweep(p-Vector2.right*.2f,p+Vector2.right*.2f,true);
                for(int i=0;i<original.Length;i++)if(!original[i]&&game.Spots[0].IsDirtyPixel(i)){coverage[g]++;opacity[g]+=game.Spots[0].MudOpacity(i);}
            }
            Assert.Greater(coverage[1],coverage[0]*1.5f,"Configured length changes the visible area");
            Assert.Greater(opacity[1],opacity[2]*1.5f,"Configured opacity changes the visible deposit");
            Assert.Greater(coverage[1],coverage[3]*1.5f,"Configured travel budget limits spreading");
        }
        [TestCase(0f)][TestCase(-.1f)][TestCase(float.NaN)][TestCase(float.PositiveInfinity)]
        public void InvalidSweepConfigIsRejected(float value)
        {
            var types=PathCleaningModule.LoadTypes();var tools=PathCleaningModule.LoadTools();
            foreach(var config in new[]{SweepConfig(leaf:value),SweepConfig(mud:value),SweepConfig(length:value),SweepConfig(opacity:value)})
                Assert.Throws<ArgumentException>(()=>PathCleaningGame.Validate(config,types,tools));
            Assert.Throws<ArgumentException>(()=>PathCleaningGame.Validate(SweepConfig(leaf:1.1f),types,tools));
            Assert.Throws<ArgumentException>(()=>PathCleaningGame.Validate(SweepConfig(mud:1.1f),types,tools));
            Assert.Throws<ArgumentException>(()=>PathCleaningGame.Validate(SweepConfig(length:1.6f),types,tools));
            Assert.Throws<ArgumentException>(()=>PathCleaningGame.Validate(SweepConfig(opacity:1.1f),types,tools));
        }
        private static void ClearSpot(PathCleaningGame g,int i)
        {
            g.Select(g.Spots[i].Type.ToolId);
            if(g.Spots[i].Type.Id=="mud")for(float y=.05f;y<=1&&!g.Spots[i].Cleared;y+=.08f)g.Wipe(i,new Vector2(0,y),new Vector2(1,y));
            else if(g.Spots[i].Type.Id=="leaves")for(int step=0;step<100&&!g.Spots[i].Cleared;step++)g.Sweep(g.Spots[i].Position,g.LeafCollection.center,true);
            else g.Clean(i,PathCleaningGame.CollectionTarget(g.Spots[i]));
        }
        private static void Solve(PathCleaningGame g){for(int pass=0;pass<20&&!g.Settled;pass++)for(int i=0;i<g.Spots.Count;i++)if(!g.Spots[i].Cleared)ClearSpot(g,i);}
        [TestCase(6,2,2,2)]
        [TestCase(10,4,3,3)]
        public void TableWeightsAllocateEveryTypeAcrossSeeds(int count,int leaves,int mud,int trash)
        {
            for(int seed=0;seed<100;seed++)
            {
                var g=new PathCleaningGame(Config(count),PathCleaningModule.LoadTypes(),PathCleaningModule.LoadTools(),seed);
                Assert.AreEqual(leaves,g.Spots.Count(s=>s.Type.Id=="leaves"));
                Assert.AreEqual(mud,g.Spots.Count(s=>s.Type.Id=="mud"));
                Assert.AreEqual(trash,g.Spots.Count(s=>s.Type.Id=="trash"));
                Assert.AreEqual(count,g.Spots.Select(s=>s.Cell).Distinct().Count());
            }
        }
        [Test] public void ChangedTableWeightsChangeCountsWithoutDroppingRareTypes()
        {
            var types=PathCleaningModule.LoadTypes().Select(t=>new PathCleaningTypeRow(t.Id,t.Pool,t.ToolId,t.Id=="leaves"?4:1,t.Color,t.Size,t.SpritePath,t.HintKey)).ToArray();
            var g=new PathCleaningGame(Config(9),types,PathCleaningModule.LoadTools(),7);
            Assert.AreEqual(5,g.Spots.Count(s=>s.Type.Id=="leaves"));
            Assert.AreEqual(2,g.Spots.Count(s=>s.Type.Id=="mud"));
            Assert.AreEqual(2,g.Spots.Count(s=>s.Type.Id=="trash"));
        }
        [Test] public void MatchingAndUnknownToolsCannotClearWrongStain(){var g=NewGame();Assert.IsFalse(g.Clean(0));Assert.IsFalse(g.Select("unknown"));g.Select(g.Tools.First(t=>t.Id!=g.Spots[0].Type.ToolId).Id);Assert.IsFalse(g.Clean(0));Assert.AreEqual(0,g.ClearedCount);ClearSpot(g,0);Assert.IsTrue(g.Spots[0].Cleared);Assert.IsFalse(g.Clean(0));Assert.AreEqual(1,g.ClearedCount);}
        [Test] public void ThreeWrongClicksHintAndProgressResets(){var g=NewGame();g.Clean(0);g.Clean(0);Assert.IsNull(g.HintKey);g.Clean(0);Assert.AreEqual(g.Spots[0].Type.HintKey,g.HintKey);ClearSpot(g,0);Assert.IsNull(g.HintKey);g.Tick(11.99);Assert.IsNull(g.HintKey);g.Tick(.02);Assert.IsNotNull(g.HintKey);}
        [TestCase("trash","trashBin","leafPile")]
        public void CollectedItemsRequireCorrectToolAndDestination(string type,string target,string wrong)
        {
            var row=PathCleaningModule.LoadTypes().Single(t=>t.Id==type);
            var g=new PathCleaningGame(Config(1),new[]{row},PathCleaningModule.LoadTools(),1);
            Assert.IsFalse(g.Clean(0,target));g.Select(row.ToolId);
            Assert.IsFalse(g.Clean(0));Assert.IsFalse(g.Clean(0,wrong));Assert.AreEqual(0,g.ClearedCount);
            Assert.IsTrue(g.Clean(0,target));Assert.AreEqual("success",g.Result.Outcome);Assert.IsFalse(g.Clean(0,target));
        }
        [Test] public void LeavesMoveOnlyALittlePerStrokeAndNeedRepeatedSweeps()
        {
            var row=PathCleaningModule.LoadTypes().Single(t=>t.Id=="leaves");var g=new PathCleaningGame(Config(1),new[]{row},PathCleaningModule.LoadTools(),1);var spot=g.Spots[0];
            var original=spot.Position;Assert.IsFalse(g.Sweep(original,original+Vector2.right*.3f,true));Assert.AreEqual(original,spot.Position);
            g.Select("broom");Assert.IsFalse(g.Clean(0,"leafPile"));Assert.IsFalse(g.Sweep(Vector2.zero,new Vector2(.01f,.01f),true));
            g.Sweep(original,original+Vector2.right*.3f,true);var first=spot.Position;
            Assert.Greater(Vector2.Distance(original,first),0);Assert.LessOrEqual(Vector2.Distance(original,first),g.Config.LeafSweepDistance+.0001f);Assert.IsFalse(spot.Cleared);
            Assert.IsFalse(g.Sweep(first,first+Vector2.right*.3f));Assert.AreEqual(first,spot.Position,"Holding one stroke cannot bypass its push budget");
            ClearSpot(g,0);Assert.IsTrue(spot.Cleared);Assert.AreEqual(1,g.ClearedCount);Assert.IsFalse(g.Sweep(original,Vector2.zero,true));
        }
        private static int MudPixel(float u,float v)
        {
            int n=PathCleaningGame.MudResolution;
            return Mathf.FloorToInt((v+.5f)*n/2)*n+Mathf.FloorToInt((u+.5f)*n/2);
        }
        [Test] public void BroomSmearsMudInStrokeDirectionWhileBrushStillCleansIt()
        {
            var g=MudGame();var spot=g.Spots[0];var from=spot.Position-Vector2.right*.2f;var to=spot.Position+Vector2.right*.2f;
            int n=PathCleaningGame.MudResolution;
            var original=Enumerable.Range(0,n*n).Select(spot.MudOpacity).ToArray();
            var sources=Enumerable.Range(0,n*n).Select(spot.MudSourcePixel).ToArray();
            var bounds=spot.MudBounds;
            g.Select("broom");Assert.IsTrue(g.Sweep(from,to,true));
            var added=Enumerable.Range(0,n*n).Where(i=>spot.MudOpacity(i)>original[i]).ToArray();
            Assert.IsNotEmpty(added);Assert.IsTrue(added.Any(i=>i%n>n/2));
            Assert.IsTrue(added.All(i=>spot.MudOpacity(i)<=g.Config.MudSpreadOpacity),"The tail follows the configured opacity");
            foreach(int i in Enumerable.Range(0,n*n).Where(i=>original[i]>.9f))
            {Assert.AreEqual(original[i],spot.MudOpacity(i),"Do not resample the original stain");Assert.AreEqual(sources[i],spot.MudSourcePixel(i));}
            int revision=spot.MudRevision;g.Sweep(to,from);Assert.AreEqual(revision,spot.MudRevision,"One held stroke has a fixed budget");
            Assert.IsFalse(spot.Cleared);Assert.AreEqual(0,g.ClearedCount);
            for(int i=0;i<20;i++)g.Sweep(from,to,true);
            Assert.AreEqual(bounds,spot.MudBounds);Assert.AreEqual(0,g.SmearSpot);
            Assert.IsTrue(Enumerable.Range(0,n).All(i=>!spot.IsDirtyPixel(i*n+n-1)),"Fade out before the canvas edge");
            ClearSpot(g,0);Assert.IsTrue(spot.Cleared);
        }
        [TestCase(3f)][TestCase(.3333333f)]
        public void ReplacementMaskKeepsItsShapeAndValidSourceColorsThroughDiagonalSmearing(float aspect)
        {
            var g=MudGame();var spot=g.Spots[0];int n=PathCleaningGame.MudSourceResolution;
            var mask=new bool[n*n];
            for(int y=n/4;y<3*n/4;y++)for(int x=n/4;x<3*n/4;x++)
                mask[y*n+x]=(x-n/2)*(x-n/2)+(y-n/2)*(y-n/2)>n*n/100;
            g.ConfigureMudMask(0,mask,aspect);
            g.ConfigureMudFootprint(0,aspect>=1?new Vector2(.15f,.15f/aspect):new Vector2(.15f*aspect,.15f));
            Assert.IsFalse(spot.IsDirtyPixel(MudPixel(.5f,.5f)),"Transparent hole remains empty before interaction");
            g.Select("broom");var direction=new Vector2(.10f,.10f);
            Assert.IsTrue(g.Sweep(spot.Position-direction,spot.Position+direction,true));
            for(int i=0;i<PathCleaningGame.MudResolution*PathCleaningGame.MudResolution;i++)
                if(spot.IsDirtyPixel(i)){Assert.IsTrue(mask[spot.MudSourcePixel(i)],"Only visible source texels may spread");Assert.Greater(spot.MudOpacity(i),0);}
            g.Select("brush");
            for(float y=.02f;y<1&&!spot.Cleared;y+=.025f)g.Wipe(0,new Vector2(0,y),new Vector2(1,y));
            Assert.IsTrue(spot.Cleared,"A replacement with a different aspect remains cleanable");
        }
        [Test] public void MudStopsAtTheDragLimitUntilANewPress()
        {
            var g=MudGame();var spot=g.Spots[0];var p=spot.Position;g.Select("broom");
            Assert.Greater(g.Config.MudSweepDistance,0);
            Assert.IsTrue(g.Sweep(p-Vector2.right*.2f,p+Vector2.right*.2f,true));
            int revision=spot.MudRevision;
            for(int i=0;i<30;i++)g.Sweep(p-Vector2.up*.2f,p+Vector2.up*.2f);
            Assert.AreEqual(revision,spot.MudRevision,"Changing direction while held cannot bypass the limit");
            Assert.IsTrue(g.Sweep(p-Vector2.up*.2f,p+Vector2.up*.2f,true));
            Assert.Greater(spot.MudRevision,revision,"A new press replenishes the drag budget");
        }
        [Test] public void DistantAndStationarySweepsCannotSpreadOrResurrectWipedMud()
        {
            var g=MudGame();var spot=g.Spots[0];g.Select("brush");g.Wipe(0,new Vector2(0,.5f),new Vector2(1,.5f));
            Assert.IsFalse(spot.IsDirtyPixel(MudPixel(.5f,.5f)));int revision=spot.MudRevision;
            g.Select("broom");Assert.IsFalse(g.Sweep(spot.Position,spot.Position,true));
            Assert.IsFalse(g.Sweep(new Vector2(-1,-1),new Vector2(-.9f,-1),true));
            Assert.AreEqual(revision,spot.MudRevision);Assert.IsFalse(spot.IsDirtyPixel(MudPixel(.5f,.5f)));
        }
        [Test] public void SmallPointerMovementsAccumulateIntoVisibleSmearing()
        {
            var g=MudGame();var spot=g.Spots[0];g.Select("broom");
            int revision=spot.MudRevision;var start=spot.Position;
            for(int i=0;i<40;i++)g.Sweep(start+Vector2.right*(i*.00015f),start+Vector2.right*((i+1)*.00015f),i==0);
            Assert.Greater(spot.MudRevision,revision);Assert.IsFalse(spot.Cleared);
        }
        [TestCase(50)][TestCase(150)]
        public void RealisticSegmentedSweepSpreadsAsFarAsOneLongSweep(int segments)
        {
            var single=MudGame();var segmented=MudGame();
            foreach(var game in new[]{single,segmented})
            {
                game.ConfigureSweepArea(game.LeafCollection,2.8f);
                game.ConfigureMudFootprint(0,new Vector2(.052f,.144f));
                game.Select("broom");
            }
            int n=PathCleaningGame.MudResolution;
            var original=Enumerable.Range(0,n*n).Select(single.Spots[0].MudOpacity).ToArray();
            var start=single.Spots[0].Position;var end=start+Vector2.right*.16f;
            single.Sweep(start,end,true);
            for(int i=0;i<segments;i++)segmented.Sweep(Vector2.Lerp(start,end,(float)i/segments),Vector2.Lerp(start,end,(float)(i+1)/segments),i==0);
            int expected=Enumerable.Range(0,n*n).Count(i=>original[i]==0&&single.Spots[0].IsDirtyPixel(i));
            int actual=Enumerable.Range(0,n*n).Count(i=>original[i]==0&&segmented.Spots[0].IsDirtyPixel(i));
            Assert.Greater(expected,500,"The reference stroke has a visible tail");
            Assert.GreaterOrEqual(actual,expected*.75f,"Splitting a pointer path into frames must not erase the spread");
            // Small events may leave the contact radius before using the larger configured
            // budget. Returning across the mud consumes the remaining travel of this press.
            segmented.Sweep(end,start);
            int revision=segmented.Spots[0].MudRevision;
            segmented.Sweep(end,start);Assert.AreEqual(revision,segmented.Spots[0].MudRevision,"The per-press limit still applies");
        }
        [Test] public void CollectedLeavesCanBeSweptOutAndMustAllBeInsideAtVictory()
        {
            PathCleaningGame g=null;int leaf=-1;
            for(int seed=0;seed<50;seed++){var candidate=new PathCleaningGame(Config(6),PathCleaningModule.LoadTypes(),PathCleaningModule.LoadTools(),seed);leaf=-1;for(int i=0;i<6;i++)if(candidate.Spots[i].Type.Id=="leaves"){leaf=i;break;}if(leaf>=0&&candidate.Spots.Any(s=>s.Type.Id=="mud")){g=candidate;break;}}
            Assert.IsNotNull(g);ClearSpot(g,leaf);var spot=g.Spots[leaf];Assert.IsTrue(spot.Cleared);Assert.IsFalse(g.Settled);
            int count=g.ClearedCount;g.Select("broom");g.Sweep(spot.Position,spot.Position+Vector2.right*.3f,true);
            Assert.IsFalse(spot.Cleared);Assert.Less(g.ClearedCount,count);Assert.IsFalse(g.Settled);
            Solve(g);Assert.IsTrue(g.Settled);Assert.IsTrue(g.Spots.Where(s=>s.Type.Id=="leaves").All(s=>s.Cleared&&g.LeafCollection.Contains(s.Position)));
        }
        private static PathCleaningGame MudGame()
        {
            for(int seed=0;seed<20;seed++){var g=NewGameWithSeed(seed);if(g.Spots[0].Type.Id=="mud")return g;}
            throw new InvalidOperationException("No mud test seed");
        }
        private static PathCleaningGame NewGameWithSeed(int seed)=>new(Config(1),PathCleaningModule.LoadTypes(),PathCleaningModule.LoadTools(),seed);
        [Test] public void MudMustBeWipedAndWrongToolCannotErase()
        {
            var g=MudGame();g.Select("broom");Assert.IsFalse(g.Wipe(0,Vector2.zero,Vector2.one));Assert.AreEqual(0,g.Spots[0].WipedFraction);
            g.Select("brush");Assert.IsFalse(g.Clean(0));Assert.IsFalse(g.Clean(0,"trashBin"));Assert.AreEqual(0,g.ClearedCount);
            Assert.IsTrue(g.Wipe(0,new Vector2(.5f,.5f),new Vector2(.5f,.5f)));Assert.IsFalse(g.Spots[0].Cleared);
            float fraction=g.Spots[0].WipedFraction;Assert.IsFalse(g.Wipe(0,new Vector2(.5f,.5f),new Vector2(.5f,.5f)));Assert.AreEqual(fraction,g.Spots[0].WipedFraction);
            Assert.IsFalse(g.Wipe(0,new Vector2(float.NaN,0),Vector2.one));Assert.IsFalse(g.Wipe(0,new Vector2(-1,-1),new Vector2(-.5f,-.5f)));Assert.AreEqual(fraction,g.Spots[0].WipedFraction);
            g.Select("tongs");Assert.IsFalse(g.Wipe(0,Vector2.zero,Vector2.one));Assert.AreEqual(fraction,g.Spots[0].WipedFraction);
            ClearSpot(g,0);Assert.IsTrue(g.Settled);Assert.GreaterOrEqual(g.Spots[0].WipedFraction,g.MudClearThreshold);Assert.AreEqual(1,g.ClearedCount);
            Assert.IsFalse(g.Wipe(0,Vector2.zero,Vector2.one));Assert.AreEqual(1,g.ClearedCount);
        }
        [Test] public void CustomShapeCountsOnlyOpaquePixelsAndRejectsEmptyMask()
        {
            var g=MudGame();int n=PathCleaningGame.MudSourceResolution;var mask=new bool[n*n];for(int y=52;y<76;y++)for(int x=52;x<76;x++)mask[y*n+x]=true;
            Assert.Throws<ArgumentException>(()=>g.ConfigureMudMask(0,new bool[n*n]));g.ConfigureMudMask(0,mask,2);mask[64*n+64]=false;
            Assert.IsTrue(g.Spots[0].IsDirtyPixel(MudPixel(.5f,.5f)),"Copy caller mask");Assert.AreEqual(2,g.Spots[0].MudAspectRatio);
            g.Select("brush");Assert.IsFalse(g.Wipe(0,new Vector2(.1f,.1f),new Vector2(.1f,.1f)));Assert.AreEqual(0,g.Spots[0].WipedFraction);
            g.Wipe(0,new Vector2(.4f,.5f),new Vector2(.6f,.5f));Assert.IsTrue(g.Settled,"Transparent border must not need wiping");
            Assert.Throws<InvalidOperationException>(()=>g.ConfigureMudMask(0,mask));
        }
        [Test] public void FastWipeErasesContinuousTrackWithoutClearingUntouchedArea()
        {
            var g=MudGame();g.Select("brush");g.Wipe(0,new Vector2(0,.5f),new Vector2(1,.5f));
            Assert.IsFalse(g.Spots[0].IsDirtyPixel(MudPixel(.5f,.5f)));Assert.IsTrue(g.Spots[0].IsDirtyPixel(MudPixel(.5f,.76f)));
            Assert.Greater(g.Spots[0].WipedFraction,.15f);Assert.Less(g.Spots[0].WipedFraction,.7f);Assert.IsFalse(g.Settled);
            var fraction=g.Spots[0].WipedFraction;g.Tick(1);Assert.AreEqual(fraction,g.Spots[0].WipedFraction);
        }
        [Test] public void DisabledTutorialNeverHints(){var g=NewGame(hint:false);for(int i=0;i<10;i++)g.Clean(0);g.Tick(20);Assert.IsNull(g.HintKey);}
        [Test] public void CountAndNonOverlappingCellsFollowConfig(){var g=NewGame(20);Assert.AreEqual(20,g.Spots.Count);Assert.AreEqual(20,g.Spots.Select(s=>s.Cell).Distinct().Count());Assert.Throws<ArgumentException>(()=>NewGame(21));}
        [Test] public void ZeroTimeLimitNeverTimesOut(){var g=NewGame();g.Tick(3600);Assert.IsFalse(g.Settled);Solve(g);Assert.AreEqual("slow",g.Result.RewardId);}
        [TestCase(15,"fast")][TestCase(15.001,"medium")][TestCase(30,"medium")][TestCase(30.001,"slow")]
        public void TimeThresholdsChooseConfiguredReward(double seconds,string reward){var g=NewGame();g.Tick(seconds);Solve(g);Assert.AreEqual(reward,g.Result.RewardId);}
        [Test] public void DeadlineWinsAndOnlySettlesOnce(){var g=NewGame(limit:45);g.Tick(45);var result=g.Result;Solve(g);g.Tick(9);Assert.AreSame(result,g.Result);Assert.AreEqual("timeout",g.Result.RewardId);Assert.AreEqual(0,g.ClearedCount);}
        [Test] public void ToolMappingIsEntirelyTableDriven(){var tools=new[]{new PathCleaningToolRow("custom","name","#FFFFFF","")};var types=new[]{new PathCleaningTypeRow("customStain","standard","custom",1,"#AABBCC",40,"","hint")};var g=new PathCleaningGame(Config(),types,tools,1);g.Select("custom");for(int i=0;i<g.Spots.Count;i++)g.Clean(i,PathCleaningGame.CollectionTarget(g.Spots[i]));Assert.AreEqual("success",g.Result.Outcome);}
        [Test] public void BothServicesCoexistAndShutdownIndependently(){var a=new global::Game.OfferingSort.OfferingSortModule();var b=new PathCleaningModule();try{a.OnInit();b.OnInit();Assert.AreSame(a,EmberServiceLocator.TryResolve<global::Game.OfferingSort.IOfferingSortService>());Assert.AreSame(b,EmberServiceLocator.TryResolve<IPathCleaningService>());((IEmberModule)b).OnDestroy();Assert.AreSame(a,EmberServiceLocator.TryResolve<global::Game.OfferingSort.IOfferingSortService>());}finally{((IEmberModule)a).OnDestroy();((IEmberModule)b).OnDestroy();}}
        private sealed class Presenter : IPathCleaningPresenter
        {
            public Action<string> Select;public Action<int> Clean;public Action<int,string> Deliver;public Action<int,Vector2,Vector2> Wipe;public Action<Vector2,Vector2,bool> Sweep;public Action Ready;public int Closes;public bool DelayReady;public bool ThrowClose;
            public void Open(PathCleaningGame game,Action<string> select,Action<int> clean,Action<int,string> deliver,Action<int,Vector2,Vector2> wipe,Action<Vector2,Vector2,bool> sweep,Action ready,Action<string> failure){Select=select;Clean=clean;Deliver=deliver;Wipe=wipe;Sweep=sweep;Ready=ready;if(!DelayReady)ready();}
            public string ShownOutcome;
            public void Refresh(PathCleaningGame game){ShownOutcome=game.Result?.Outcome;}
            public void Close(){Closes++;if(ThrowClose)throw new InvalidOperationException("injected close failure");}
        }
        [Test] public void AbortDuringTimeoutNoticeDoesNotAdvanceStory()
        {
            var module=new PathCleaningModule();var view=new Presenter();EmberServiceLocator.Register<IPathCleaningPresenter>(view);int completes=0;
            try
            {
                module.OnInit();module.Invoke("cmh.a002.path_cleaning","test","{\"Visit\":2}",_=>completes++,Assert.Fail);
                typeof(PathCleaningModule).GetField("_lastTime",BindingFlags.Instance|BindingFlags.NonPublic).SetValue(module,Time.realtimeSinceStartupAsDouble-module.CurrentGame.Config.TimeLimitSeconds-1);
                module.Update();Assert.AreEqual("timeout",view.ShownOutcome);
                module.Abort("test");module.Update();view.Clean(1);
                Assert.AreEqual(0,completes);Assert.AreEqual(1,view.Closes);Assert.IsNull(module.CurrentGame);
            }
            finally{((IEmberModule)module).OnDestroy();EmberServiceLocator.Unregister<IPathCleaningPresenter>();}
        }
        [Test] public void ModuleAbortIgnoresLateReadyAndNeverPays()
        {
            var module=new PathCleaningModule();var view=new Presenter{DelayReady=true};EmberServiceLocator.Register<IPathCleaningPresenter>(view);int completes=0,failures=0;
            try{module.OnInit();module.Invoke("cmh.a002.path_cleaning","test","{\"Visit\":1}",_=>completes++,_=>failures++);Assert.IsNotNull(module.CurrentGame);module.Abort("test");view.Ready();module.Update();view.Clean(1);Assert.IsNull(module.CurrentGame);Assert.AreEqual(0,completes);Assert.AreEqual(0,failures);Assert.AreEqual(1,view.Closes);}
            finally{((IEmberModule)module).OnDestroy();EmberServiceLocator.Unregister<IPathCleaningPresenter>();}
        }
        [TestCase(false)][TestCase(true)] public void ModuleClockTimesOutAndReturnsExactlyOnce(bool throwClose)
        {
            var module=new PathCleaningModule();var view=new Presenter{ThrowClose=throwClose};EmberServiceLocator.Register<IPathCleaningPresenter>(view);int completes=0;string result=null;
            if(throwClose)UnityEngine.TestTools.LogAssert.Expect(LogType.Error,new System.Text.RegularExpressions.Regex(".*小游戏UI清理失败.*"));
            try
            {
                module.OnInit();module.Invoke("cmh.a002.path_cleaning","test","{\"Visit\":2}",r=>{completes++;result=r;},e=>Assert.Fail(e));
                typeof(PathCleaningModule).GetField("_lastTime",BindingFlags.Instance|BindingFlags.NonPublic).SetValue(module,Time.realtimeSinceStartupAsDouble-module.CurrentGame.Config.TimeLimitSeconds-1);
                module.Update();module.Update();view.Clean(1);
                Assert.AreEqual("timeout",view.ShownOutcome);Assert.IsNotNull(module.CurrentGame);Assert.AreEqual(0,completes);Assert.AreEqual(0,view.Closes);
                typeof(PathCleaningModule).GetField("_completionShownAt",BindingFlags.Instance|BindingFlags.NonPublic).SetValue(module,Time.realtimeSinceStartupAsDouble-module.CurrentGame.Config.TimeoutResultSeconds*.5);
                module.Update();Assert.AreEqual(0,completes,"Keep timeout notice for the full two seconds");
                typeof(PathCleaningModule).GetField("_completionShownAt",BindingFlags.Instance|BindingFlags.NonPublic).SetValue(module,Time.realtimeSinceStartupAsDouble-module.CurrentGame.Config.TimeoutResultSeconds-1);
                module.Update();module.Update();view.Clean(1);
                Assert.AreEqual(1,completes);Assert.AreEqual("cmh_clean_timeout",JsonUtility.FromJson<PathCleaningResult>(result).RewardId);Assert.AreEqual(1,view.Closes);
            }
            finally{((IEmberModule)module).OnDestroy();EmberServiceLocator.Unregister<IPathCleaningPresenter>();}
        }
        [Test] public void PreviousRunCallbacksCannotTouchNextRun()
        {
            var module=new PathCleaningModule();var view=new Presenter();EmberServiceLocator.Register<IPathCleaningPresenter>(view);
            try{module.OnInit();module.Invoke("cmh.a002.path_cleaning","old","{\"Visit\":1}",_=>{},Assert.Fail);var oldSelect=view.Select;var oldClean=view.Clean;var oldWipe=view.Wipe;var oldReady=view.Ready;module.Abort("old");module.Invoke("cmh.a002.path_cleaning","new","{\"Visit\":1}",_=>{},Assert.Fail);var game=module.CurrentGame;oldSelect(game.Spots[0].Type.ToolId);oldClean(0);oldWipe(0,Vector2.zero,Vector2.one);oldReady();Assert.IsNull(game.SelectedTool);Assert.AreEqual(0,game.ClearedCount);module.Abort("new");}
            finally{((IEmberModule)module).OnDestroy();EmberServiceLocator.Unregister<IPathCleaningPresenter>();}
        }
        private sealed class Lease<T> : INovelAssetLease<T> where T:UnityEngine.Object
        {public bool IsDone=>true;public T Asset{get;set;}public string Error=>null;public void Dispose(){}}
        private sealed class StoryResources : INovelResources
        {public NarrativeStorySO Story;public INovelAssetLease<T> Load<T>(string path)where T:UnityEngine.Object=>new Lease<T>{Asset=typeof(T)==typeof(NarrativeStorySO)?Story as T:Resources.Load<T>(path)};}
        private sealed class View : INovelView
        {public int TextLength=>10;public void Render(NarrativeSnapshot s,NovelCommand c,string speaker,int visible,string status){}public void Visual(NovelCommand c,Sprite sprite,float progress){}public void ClearVisuals(){}}
        [Test] public void RealA002LocksSessionCompletesAndAccumulatesMonthlyReward()
        {
            var module=new PathCleaningModule();var view=new Presenter();EmberServiceLocator.Register<IPathCleaningPresenter>(view);NovelRewardService.LoadBakedForEditor();
            using var engine=new Ember.Table.EmberTableEngine();var catalog=Game.Table.Generated.GameTables.CreateCatalog();var bytes=new System.Collections.Generic.Dictionary<string,byte[]>();foreach(var e in catalog.Entries)bytes[e.TableId]=Resources.Load<TextAsset>(e.ResourcePath).bytes;Assert.IsTrue(engine.Load(catalog,bytes).Succeeded);var tables=new NarrativeTableCatalog(engine.Database);
            var original=Resources.Load<NarrativeStorySO>("Config/Narrative/CallMeHeartless/Story_CallMeHeartless");var story=UnityEngine.Object.Instantiate(original);
            try
            {
                module.OnInit();var chapter=story.Chapters.Single(c=>c.ChapterId=="a9dbb0cd472a4c99a5a1d1ece01bcaa7");
                // Enter through the real parent call; seed first-event data so visit two selects A002.
                var data=new UnityEditor.SerializedObject(story);var globals=data.FindProperty("_globals");for(int i=0;i<globals.arraySize;i++){var r=globals.GetArrayElementAtIndex(i);string id=r.FindPropertyRelative("_id").stringValue;if(id=="ch2_aVisits"||id=="ch2_aFirstEvent")r.FindPropertyRelative("_value").FindPropertyRelative("_int").intValue=id=="ch2_aVisits"?1:1;}data.ApplyModifiedPropertiesWithoutUndo();
                using var session=new NovelSession(new NovelNewGameRequest(chapterId:chapter.ChapterId,nodeId:chapter.Nodes.Single(n=>n.name.EndsWith("_A_Call")).NodeId),()=>tables,new StoryResources{Story=story});session.AttachView(new View());long frame=0;
                for(int i=0;i<40&&module.CurrentGame==null;i++){session.Tick(10,++frame);session.Advance(++frame);}
                Assert.IsNotNull(module.CurrentGame,session.Snapshot.Error?.ToString());Assert.AreEqual(0,module.CurrentGame.Config.TimeLimitSeconds);Assert.IsTrue(session.IsInputLocked);Assert.IsNotEmpty(session.Snapshot.PauseReasons);Assert.IsFalse(session.TryCapture(out _,out _));Assert.AreEqual(1,session.Snapshot.GlobalVariables["ch2_aCleaningVisits"].Int);
                var game=module.CurrentGame;for(int i=0;i<game.Spots.Count&&!game.Settled;i++)
                {
                    view.Select(game.Spots[i].Type.ToolId);
                    if(game.Spots[i].Type.Id=="mud")for(float y=.05f;y<=1&&!game.Spots[i].Cleared;y+=.08f)view.Wipe(i,new Vector2(0,y),new Vector2(1,y));
                    else if(game.Spots[i].Type.Id=="leaves")for(int step=0;step<100&&!game.Spots[i].Cleared;step++)view.Sweep(game.Spots[i].Position,game.LeafCollection.center,true);
                    else view.Deliver(i,PathCleaningGame.CollectionTarget(game.Spots[i]));
                }
                Assert.IsNotNull(module.CurrentGame,"Keep the final completion effect visible");Assert.IsTrue(session.IsInputLocked);
                typeof(PathCleaningModule).GetField("_completionShownAt",BindingFlags.Instance|BindingFlags.NonPublic).SetValue(module,Time.realtimeSinceStartupAsDouble-2);module.Update();
                Assert.IsNull(module.CurrentGame);Assert.IsFalse(session.IsInputLocked);Assert.IsEmpty(session.Snapshot.PauseReasons);Assert.AreEqual(30,session.Snapshot.GlobalVariables["ch2_aCleaningMonthlyBonus"].Int);Assert.AreEqual(25,session.Snapshot.GlobalVariables["player_money"].Int);
                for(int i=0;i<20&&session.Snapshot.State!=NarrativeState.AwaitingAdvance;i++)session.Tick(10,++frame);
                Assert.IsTrue(session.TryCapture(out var save,out var error),error);Assert.AreEqual(1,save.Globals.Single(v=>v.Id=="ch2_aCleaningVisits").Value.Int);
            }
            finally{UnityEngine.Object.DestroyImmediate(story);((IEmberModule)module).OnDestroy();EmberServiceLocator.Unregister<IPathCleaningPresenter>();}
        }
    }
}



