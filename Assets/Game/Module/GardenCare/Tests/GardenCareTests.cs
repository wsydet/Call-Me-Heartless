using System;
using System.Linq;
using Ember.Core;
using NUnit.Framework;
using UnityEngine;

namespace Game.GardenCare.Tests
{
    public sealed class GardenCareTests
    {
        private static GardenCareConfig Config() => new GardenCareConfig { cropCount = 12, weedCount = 6, insectCount = 4, timeLimitSeconds = 90, columns = 4, rows = 3 };

        [Test]
        public void BoardsAlwaysContainDistinctInsectsOnCrops()
        {
            for (int seed = 0; seed < 32; ++seed)
            {
                var game = new GardenCareGame(Config(), seed);
                Assert.AreEqual(6, Enumerable.Range(0, game.PlantCount).Count(game.IsWeed));
                Assert.AreEqual(18, game.PlantCount);
                var companions = Enumerable.Range(12, 6).Select(game.PlantingCell).ToArray();
                Assert.AreEqual(6, companions.Distinct().Count());
                Assert.IsTrue(companions.All(i => i >= 0 && i < 12));
                var occupied = Enumerable.Range(0, 4).Select(game.InsectCell).ToArray();
                Assert.AreEqual(4, occupied.Distinct().Count());
                Assert.IsTrue(occupied.All(i => !game.IsWeed(i)));
            }
        }

        [Test]
        public void WrongTargetsAndRepeatedInputsCannotCompleteEarly()
        {
            var game = new GardenCareGame(Config(), 3);
            int weed = Enumerable.Range(0, game.PlantCount).First(game.IsWeed);
            Assert.AreEqual(GardenCarePhase.Playing,game.Phase);
            game.Start(); game.Pull(game.InsectCell(0)); game.Deliver(0, false);
            Assert.AreEqual(0, game.PulledCount + game.DeliveredCount);
            Assert.AreEqual(1, game.LostCropCount);
            Assert.IsTrue(game.IsPulled(game.InsectCell(0)), "误点菜苗必须实际拔掉");
            game.Pull(game.InsectCell(0)); Assert.AreEqual(1, game.LostCropCount);
            foreach (int i in Enumerable.Range(0, game.PlantCount).Where(game.IsWeed)) { game.Pull(i); game.Pull(i); }
            Assert.AreEqual(GardenCarePhase.Playing, game.Phase);
            for (int i = 0; i < 4; ++i) { game.Deliver(i, true); game.Deliver(i, true); }
            Assert.AreEqual(6, game.PulledCount); Assert.AreEqual(4, game.DeliveredCount);
            Assert.AreEqual(GardenCarePhase.Result, game.Phase); Assert.AreEqual("completed", game.Outcome);
            game.Continue(); game.Continue(); game.Tick(1000, false);
            Assert.AreEqual(GardenCarePhase.Closed, game.Phase); Assert.AreEqual("completed", game.Outcome);
        }

        [Test]
        public void PausesFreezeEveryPhaseAndUnattendedGameEventuallyReturns()
        {
            var game = new GardenCareGame(Config(), 1);
            game.Tick(1000, true); Assert.AreEqual(GardenCarePhase.Playing, game.Phase);
            game.Tick(1000, true); Assert.AreEqual(0, game.Elapsed);
            game.Tick(90, false); Assert.AreEqual(GardenCarePhase.Result, game.Phase); Assert.AreEqual("timeout", game.Outcome);
            game.Tick(1000, true); Assert.AreEqual(GardenCarePhase.Result, game.Phase);
            game.Tick(15, false); Assert.AreEqual(GardenCarePhase.Closed, game.Phase);
            var slow = new GardenCareGame(Config(), 1); slow.Tick(166, false);
            Assert.AreEqual(GardenCarePhase.Closed, slow.Phase); Assert.AreEqual("timeout", slow.Outcome);
        }

        [Test]
        public void InvalidConfigurationAndClockAreRejected()
        {
            var c = Config(); c.insectCount = 7; Assert.Throws<ArgumentException>(() => new GardenCareGame(c, 0));
            c = Config(); c.timeLimitSeconds = float.NaN; Assert.Throws<ArgumentException>(() => new GardenCareGame(c, 0));
            c = Config(); c.columns = 5; Assert.Throws<ArgumentException>(() => new GardenCareGame(c, 0));
            var game = new GardenCareGame(Config(), 0); Assert.Throws<ArgumentOutOfRangeException>(() => game.Tick(double.NaN, false));
        }

        [Test] public void TutorialStartsImmediatelyAndNeverTimesOut()
        {
            var config=Config();config.tutorial=true;var game=new GardenCareGame(config,7);
            Assert.AreEqual(GardenCarePhase.Playing,game.Phase);game.Tick(3600,false);
            Assert.AreEqual(GardenCarePhase.Playing,game.Phase);Assert.AreEqual("",game.Outcome);
            foreach(int i in Enumerable.Range(0,game.PlantCount).Where(game.IsWeed))game.Pull(i);
            for(int i=0;i<4;i++)game.Deliver(i,true);
            game.Tick(2,false);Assert.AreEqual(GardenCarePhase.Closed,game.Phase);Assert.AreEqual("completed",game.Outcome);
        }

        private sealed class View : IGardenCarePresenter
        {
            public Action Start, Next;
            public Action<int> Pull;
            public Action<int, bool> Deliver;
            public Action<string> Failure;
            public int Closed;
            public void Open(GardenCareGame game, Action start, Action<int> pull, Action<int, bool> deliver, Action next, Action<string> failure)
            { Start = start; Pull = pull; Deliver = deliver; Next = next; Failure = failure; }
            public void Refresh(GardenCareGame game, bool paused) { }
            public void Close() { Closed++; }
        }

        [Test]
        public void ModuleCompletesOnceAndRejectsStaleCallbacksAfterAbortOrRestart()
        {
            var previous = EmberServiceLocator.TryResolve<IGardenCarePresenter>();
            var view = new View(); var module = new GardenCareModule();
            int completed = 0, failed = 0;
            try
            {
                EmberServiceLocator.Register<IGardenCarePresenter>(view);
                string payload = JsonUtility.ToJson(Config());
                module.Invoke("cmh.b.garden_care", "old", payload, _ => completed++, _ => failed++);
                var staleStart = view.Start; var staleNext = view.Next; var staleFailure = view.Failure;
                module.Abort("old"); module.Abort("old");
                Assert.AreEqual(1, failed); Assert.AreEqual(1, view.Closed); Assert.IsNull(module.CurrentGame);
                module.Invoke("cmh.b.garden_care", "new", payload, _ => completed++, _ => failed++);
                staleStart(); staleNext(); staleFailure("stale");
                Assert.AreEqual(GardenCarePhase.Playing, module.CurrentGame.Phase);
                module.Invoke("cmh.b.garden_care", "duplicate", payload, _ => completed++, _ => failed++);
                Assert.AreEqual(2, failed);
                view.Start(); var game = module.CurrentGame;
                foreach (int i in Enumerable.Range(0, game.PlantCount).Where(game.IsWeed)) view.Pull(i);
                for (int i = 0; i < 4; ++i) view.Deliver(i, true);
                view.Next(); view.Next(); view.Failure("late"); module.Update();
                Assert.AreEqual(1, completed); Assert.AreEqual(2, failed); Assert.AreEqual(2, view.Closed);
                Assert.IsNull(module.CurrentGame);
            }
            finally
            {
                module.ResetModuleData();
                if (previous != null) EmberServiceLocator.Register<IGardenCarePresenter>(previous);
                else EmberServiceLocator.Unregister<IGardenCarePresenter>();
            }
        }
    }
}
